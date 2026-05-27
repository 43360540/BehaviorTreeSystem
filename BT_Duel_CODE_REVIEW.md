# BT Duel Demo — Code Review

> 1v1 tactical duel (Duelist vs Marksman) — built to showcase the Class-First
> BT framework on a non-trivial scenario.
>
> **Scope V1** (Sections 1-12 below): Intermediate tactics (HP-tier state /
> cover / circle) + 2 Advanced tactics (Flank, Predicted shooting). Decoy /
> fake retreat skipped — pure BT can't express it cleanly.
>
> **Scope V2** (Section 13, "Human-like overhaul"): Cone FOV + LOS perception,
> LastKnownPos memory, cover-to-cover advance / investigate, careful vs snap
> shot fire discipline. Built after V1 play exposed two real findings:
> (a) `Sense` has no LOS so NPCs see through walls,
> (b) Marksman 12 m "attack range" + 6 m retreat made the safe-fire window
> too narrow for the demo's arena.

---

## 1. Overview

Two NPCs in a 40×40 m arena with 2 roofless buildings, walls (high/low) and
pillars (each tagged with `CoverPoint`). The BT trees pick tactical mode
based on HP, ammo state, and the opponent's vulnerability windows.

What this is meant to demonstrate:

- BT framework can express **layered tactical AI** in pure code (no
  visual editor, no external planner)
- New BT primitives (Cover lookup, LOS check, ammo gating) compose with
  existing primitives via the fluent builder without modifying core
- The "Advanced tactics" branches (Flank, Predicted shooting) exercise BT's
  expressiveness limits — failure modes here will be documented as evaluation
  evidence rather than worked around

---

## 2. File inventory (18 new + 0 modified)

```
Assets/Scripts/BT/ClassFirst/Duel/
├── IRangedAmmo.cs              ─ read-only: Ammo / MaxAmmo / IsReloading
├── IRangedAmmoController.cs    ─ owner-side mutator (StartReload / TickReload / ConsumeShot)
├── ICoverHolder.cs             ─ slot for "currently selected cover"
├── CoverPoint.cs               ─ MonoBehaviour, scene-placed, with SafeDirection + arc
├── CoverRegistry.cs            ─ process-wide list, FindBestCover query
├── Conditions/
│   ├── HasLineOfSight.cs       ─ raycast self→target, blocked → false
│   ├── IsTargetReloading.cs    ─ asks target.GetComponent<IRangedAmmo>()
│   ├── IsAmmoLow.cs            ─ Check(ratio) + IsEmpty(source)
│   └── IsBehindCover.cs        ─ scans CoverRegistry for cover within 1.2m
├── Actions/
│   ├── FindCover.cs            ─ writes result into ICoverHolder.CurrentCover
│   ├── MoveToCover.cs          ─ NavMesh to cover.StandPosition
│   ├── Reload.cs               ─ locks agent, drives IRangedAmmoController
│   ├── StrafeAroundTarget.cs   ─ orbit target at current distance
│   ├── MoveToFlankPosition.cs  ─ Quaternion(±120°) * target.forward, NavMesh-sample
│   └── PredictedShoot.cs       ─ lead by velocity·t, raycast resolves at predicted point
└── Editor/
    └── DuelSceneSetup.cs       ─ 3 menu items: Create / Setup / Clear

Assets/Scripts/BT/ClassFirst/Runners/
├── DuelistRunner.cs            ─ melee, ICoverHolder
└── MarksmanRunner.cs           ─ ranged, ICoverHolder + IRangedAmmoController
```

`BTContext` is **NOT** modified (it's `sealed` — see Section 3).

---

## 3. Architectural decisions

### 3.1 Sealed BTContext → state on Runners + interfaces

`BTContext` is `sealed` (verified). Three options were considered:

| Option | Verdict |
|---|---|
| Unseal BTContext + inherit (DuelistContext) | Touches user's framework. Rejected. |
| Add nullable duel fields to BTContext | Pollutes shared blackboard. Rejected. |
| **Store duel state on the Runner; Actions take Runner-typed interface via ctor** | Chosen — zero framework surface area change |

Pattern: `MarksmanRunner` implements `IRangedAmmoController` and
`ICoverHolder`. Actions take these interfaces in their ctor; runners pass
`this` from `CreateTree()`.

```csharp
// inside MarksmanRunner.CreateTree()
.Do(new PredictedShoot(this, _bulletSpeed))
.Do(new Reload(this))
.Do(new FindCover(this, _coverSearchRadius))
```

Pros: zero coupling pollution, ammo state has one owner.
Cons: each Action has Runner-knowledge baked into its ctor — can't be reused
verbatim by an unrelated class.

### 3.2 CoverPoint: SafeDirection + arc (not full 3D cover map)

A `CoverPoint` declares one stand position + a `SafeDirection` (outward normal
of the protecting wall) + a `ProtectionArcDeg`. For a flat wall: 120° arc.
For a pillar: **four** cover points around it, each 180° arc (the original
single-point omnidirectional design was a bug — `ProtectsFrom` would return
true regardless of which side the NPC stood on; fixed in `SpawnPillar`).

Cover scoring (`CoverRegistry.FindBestCover`):

```
score = -distance(seeker, stand)
      - 4 if seeker is currently on the threat-side of the cover
        (would have to run through the line of fire)
```

Single duel = no contention, so no occupancy/reservation logic.

### 3.3 Ammo as `IRangedAmmo` (read) + `IRangedAmmoController` (mutate)

Split read interface from controller so opposing NPCs can introspect the
Marksman ("are they reloading?") without getting permission to mutate.

`StartReload` immediately drains ammo to 0 — commits the Marksman to the
window, can't be quick-cancelled. This is what enables the Duelist's
"Opportunity" rush branch to actually matter.

---

## 4. New BT primitives

### Conditions (static methods, called from `.When` predicates)

| Predicate | Signature | Edge cases |
|---|---|---|
| `HasLineOfSight.Check(ctx)` | bool, eyeY=1.2, chestY=1.2, maxRange=60 | No target → false. No raycast hit at all → **true** (matches ArcherShoot convention) |
| `IsTargetReloading.Check(ctx)` | bool | Target without IRangedAmmo → false silently |
| `IsAmmoLow.Check(source, ratio=0.3)` | bool | null source → false |
| `IsAmmoLow.IsEmpty(source)` | bool | null source → false |
| `IsBehindCover.Check(ctx, max=1.2m)` | bool | Scans ALL registered cover; O(N) per call (N is tiny for duel) |

### Actions

#### `FindCover(ICoverHolder, searchRadius=25)`

Pure side-effect: writes best cover into holder.CurrentCover.
Returns Success when found, Failure when no cover or no target.
**Does not move** — pair with MoveToCover in a Sequence.

#### `MoveToCover(ICoverHolder, arriveTolerance=0.8)`

Reads holder.CurrentCover, NavMesh to that position. Re-tries
SetDestination on first Tick if Start's call failed (NavMesh
sometimes rejects until agent settled).

Failure if no cover, agent off-NavMesh, or path invalid.

#### `Reload(IRangedAmmoController)`

`Start` calls `StartReload()` + locks agent (`isStopped = true`).
`Tick` calls `TickReload(dt)`; returns Running while `IsReloading`.
`Stop` un-locks agent regardless of completion.

Note: If a higher-priority BT branch preempts Reload mid-way,
the controller is **still in IsReloading state**. Branch 2b
("ongoing reload — just pump it") handles re-entry so the timer
keeps ticking even from another decision path.

#### `StrafeAroundTarget(duration=1.2, clockwise=false, desiredDistance=0)`

Tangent-step at current radial distance (or `desiredDistance` if
non-zero). Faces the target. NavMesh.SamplePosition each tick —
short-hop, no path planning.

Reset zeros elapsed so re-entry restarts. Failure if no target or
on top of target.

#### `MoveToFlankPosition(flankAngle=120, flankRadius=4, arriveTolerance=1.2)`

Computes a candidate position: `target.pos + Rotate(±flankAngle) * target.forward * flankRadius`.
Tries the side closer to the NPC's current position first; falls
back to the opposite side; both fail → action Failure.

`target.forward` falls back to (self - target) when target.forward
is near-zero (early-frame agents).

#### `PredictedShoot(IRangedAmmoController, bulletSpeed=25)`

Iterates the lead calculation twice (refines time-to-hit). Resolves
the hit by raycasting toward the **predicted** point, not the
current position — so a moving target eats the shot and a
last-second sidestep misses naturally.

`ConsumeShot()` is called on damage resolution, NOT on Start —
so a Stop-with-Failure doesn't consume ammo.

---

## 5. DuelistRunner BT tree

```
Root
└── Parallel
    ├── Sensor:  Repeater · Force(Success) · Throttle(0.3s) · When(!HasTarget) · Sense
    └── Decision: Repeater · Selector
        │
        ├── 1) OPPORTUNITY  ── target reloading
        │   └── Sequence: MoveToTarget(chase) → FaceTarget → WarriorCharge → Wait(0.2)
        │
        ├── 2) DEFENSIVE    ── HpRatio < 0.30
        │   └── Selector
        │       ├── Sequence: FindCover → MoveToCover → Wait(1.5)
        │       └── MoveAway(_attackRange * 2.5)        ← fallback if no cover
        │
        ├── 3) TACTICAL     ── HpRatio < 0.60
        │   └── Sequence: MoveToFlank → MoveToTarget(chase) → FaceTarget → WarriorCharge → Wait(0.2)
        │       (if MoveToFlank fails, sequence fails → fall through to branch 4/5)
        │
        ├── 4) AGGRESSIVE   ── in attack range
        │   └── Sequence: FaceTarget → WarriorCharge → Wait(0.2)
        │
        ├── 5) AGGRESSIVE chase ── any target
        │   └── MoveToTarget(stop=attackRange - 0.3)
        │
        └── 6) Idle ── Wait(0.1)
```

### Branch annotations

- **Branch 1 priority over everything**, including own low HP. Rationale: a
  reloading Marksman is a 3 s window — if Duelist gets in melee range during
  it, Duelist wins.
- **Branch 2** commits Duelist to cover for 1.5 s before re-evaluating.
  Prevents oscillation between "I'm low → flee" and "I see target → charge".
- **Branch 3** is where Flank shows up. If flank fails (no NavMesh path either
  side — common when target's against a wall), sequence fails and selector
  falls into branch 4 or 5 = standard aggressive engagement. Failure mode
  degrades to baseline, no edge case crash.
- **Branches 4 and 5** are identical to WarriorRunner's design — verified
  pattern, no surprise.

---

## 6. MarksmanRunner BT tree

```
Root
└── Parallel
    ├── Sensor (identical to Duelist)
    └── Decision: Repeater · Selector
        │
        ├── 1) OPPORTUNITY  ── target reloading + I have ammo + LOS
        │   └── Sequence: FaceTarget → PredictedShoot → Wait(_shootCooldown)
        │
        ├── 2) DANGER ZONE   ── target inside _retreatRange (6 m)
        │   └── MoveAway(desired = _retreatRange + 1.5)
        │   (sits above reload branches so closing Duelist always triggers
        │    kite, even mid-reload; Reload state is preserved on re-entry.)
        │
        ├── 3) FORCED RELOAD ── ammo == 0 + !IsReloading
        │   └── Selector
        │       ├── Sequence: FindCover → MoveToCover → Reload
        │       └── Reload                              ← fallback: reload exposed
        │
        ├── 3b) Ongoing reload ── IsReloading
        │   └── Reload                                  ← keep pumping timer
        │
        ├── 4) TACTICAL RELOAD ── ammo low + target outside (retreat + 2 m)
        │   └── Sequence: FindCover → MoveToCover → Reload
        │
        ├── 5) ENGAGE        ── in range + LOS + has ammo + !reloading
        │   └── Sequence: FaceTarget → PredictedShoot → Wait(_shootCooldown)
        │
        ├── 6) CLOSE         ── any target (too far or no LOS)
        │   └── MoveToTarget(stop = _engagementRange - 1)
        │
        └── 7) Idle ── Wait(0.1)
```

### Branch annotations

- **Branch 1** mirrors Duelist branch 1 — same priority idea, opposite role.
- **Branch 2 vs 2b** split is intentional: 2 is "I just ran out, decide where
  to reload" (fresh decision); 2b is "I'm in the middle of a reload, just
  keep the timer running" (re-entry from preemption). Without 2b, a higher
  priority preemption (e.g. target gets close → branch 3 retreats) would
  Stop the Reload action and the timer would stall forever.
- **Branch 3 (DANGER ZONE) sits above tactical reload (4)** — if a Duelist
  closes during a tactical reload window, the Marksman bails out of branch 4
  and goes to branch 3 (kite). The reload state is preserved (handled by 2b
  next tick when distance grows back).
- **Branch 5 PredictedShoot** uses target velocity. Against a stationary
  target, degrades to hitscan. Against a kiting target, leads the shot.
- **Branch 6 ("CLOSE")** is the catch-all for "have target but rules 1-5
  didn't match". Either out of range or no LOS — closing in fixes both.

### Tunable defaults (MarksmanRunner Inspector)

| Field | Default | Purpose |
|---|---|---|
| `_maxAmmo` | 5 | Shots per reload |
| `_reloadDuration` | 3 s | Vulnerability window for opponent |
| `_shootCooldown` | 1.0 s | Between-shot timing |
| `_retreatRange` | 6 m | Danger zone radius |
| `_engagementRange` | 12 m | Preferred firing range |
| `_lowAmmoRatio` | 0.4 | Tactical reload trigger |
| `_bulletSpeed` | 25 m/s | Lead calc input |

---

## 7. Tactical walkthrough — concrete scenarios

### Scenario A: full-HP Duelist vs Marksman with 5 ammo, no LOS yet

1. Both NPCs spawn ~42 m apart, sensor radius 45 → each sees the other on
   first sensor tick (0.3 s in)
2. Duelist: HP 1.0 + target found + not in range → branch 5 (chase)
3. Marksman: ammo 5 + not reloading + target found + likely no LOS (buildings
   in middle) → branch 6 (close to engagement range)
4. As gap closes:
   - Duelist enters Marksman's engagement range → Marksman branch 5 fires
   - Duelist eats a few shots, HP drops past 0.6 → Duelist branch 3 (Flank)
   - Marksman's PredictedShoot leads Duelist's flank arc — some hit, some miss
5. Duelist completes flank, in melee range → branch 4 (WarriorCharge)
6. Marksman backs into branch 3 (DANGER ZONE) → MoveAway → opens distance →
   back to branch 5 fire

### Scenario B: Duelist HP critical (0.2) during a reload window

1. Multiple shots have dropped Duelist HP to 0.20
2. Selector evaluates branches 1 → 2 → … in order
3. Branch 1 first: is target reloading? **Yes** → branch 1 wins, skip
   defensive branch 2
4. Duelist rushes Marksman regardless of HP — interrupts the reload by
   killing the Marksman before timer completes
5. **Key tactical moment** — opportunity overrides survival. Designed in.

### Scenario C: Marksman pinned against wall while reloading

1. Marksman branch 2 fires, FindCover picks a wall cover, MoveToCover commits
2. Mid-traversal, Duelist closes inside `_retreatRange` (6 m)
3. Selector preempts branch 2 (Sequence still running) for branch 3
   (DANGER ZONE)
4. Reload.Stop runs: agent un-locked, `IsReloading` is **still true**, timer
   still mid-reload
5. MoveAway runs, opens distance
6. As distance > 6 m, branch 3 no longer matches; branch 2 not active because
   ammo is 0 BUT `_isReloading == true` so the `!_isReloading` guard fails;
   branch 2b matches → Reload action re-enters → keeps pumping timer
7. Reload completes mid-kite → branch 5 takes over (ENGAGE)

### Scenario D: Duelist tries to flank a Marksman against a wall

1. Duelist HP 0.5, branch 3 TACTICAL → MoveToFlank fires
2. `TryPickFlank` first tries the side closer to Duelist's current pos
3. Both candidate flank positions are inside the building wall behind
   Marksman → `NavMesh.SamplePosition` fails both sides
4. MoveToFlank returns Failure → sequence fails → selector falls through
   to branch 4 / 5 (standard aggressive)
5. Behavior degrades to "head-on charge" instead of crashing or freezing.

---

## 8. BT framework strain points (evaluation evidence)

Places where the BT shape made things awkward — useful for the
ASSESSMENT.md update later.

### 8.1 No first-class "stateful mid-sequence preemption" tool

Marksman branches 2 vs 2b exist because BT actions are stateless w.r.t. the
selector. Once Reload's Sequence is preempted, the next "enter from top of
selector" needs a different branch to re-call the same Action. **Worked
around** with 2b but it's repeating logic.

In an HTN this is one operator with continuation. In BT it's two separate
selector arms with shared underlying action.

### 8.2 `.When` predicates with multi-condition logic become long lambdas

```csharp
.When((ctx, _) => ctx.HasTarget
              && !_isReloading
              && _currentAmmo > 0
              && IsTargetReloading.Check(ctx)
              && HasLineOfSight.Check(ctx),
    _ => _.Sequence(...)
```

5 conditions per branch is the high end. Without named predicate
composition, this hurts readability. Pulling them into named bool methods
on the Runner would help but adds boilerplate.

### 8.3 Per-Action ctor injection is correct but verbose

Every duel-aware Action takes `this` (Runner) in ctor → CreateTree() has
`new Reload(this)`, `new FindCover(this)`, `new PredictedShoot(this)`.
4 mentions of `this` in the Marksman tree. Tolerable but DI-friendly
context object (one slot per concern) would be cleaner.

### 8.4 `Force(Success)` + `Throttle` + `When` for sensor is incantation-level

The 4-decorator stack for "tick sensor every 0.3s when no target" is
unavoidable but reads like ceremony. Suggests the framework would benefit
from a "Sensor" composite primitive.

### 8.5 Cover lookup result lives on a Runner field — implicit coupling

`FindCover` writes to `holder.CurrentCover`. `MoveToCover` reads it.
`IsBehindCover` doesn't (re-scans registry). The implicit shared state
isn't obvious from reading the BT — you have to know "FindCover-then-
MoveToCover sequence assumes the same holder".

This is the kind of bug you'd typically find via integration test.
We haven't written one for this duel — manual play is the test plan.

---

## 9. Known limitations / explicitly skipped

- **Decoy / fake retreat** — skipped per scope agreement. Pure BT would
  need a multi-step stateful plan that LOOKS retreating but is positioning;
  better done in an HTN or a custom planner.
- **Marksman vs Marksman / Duelist vs Duelist** — Sense uses Faction
  filtering. Two Marksman (both TeamB) wouldn't see each other. Not
  designed for; the scene spawns TeamA Duelist + TeamB Marksman only.
- **Cover occupancy** — `CoverRegistry.FindBestCover` doesn't check if a
  cover is already in use by another NPC. Single duel = no contention.
- **PeekAndShoot as a dedicated action** — built via existing Sequence
  primitives in the BT instead. Engage branch 5 = FaceTarget +
  PredictedShoot + cooldown, then either re-engage or branch flips.
- **NavMesh auto-bake** — scene setup script intentionally leaves bake as
  a manual user step. `NavMeshSurface.BuildNavMesh()` doesn't persist the
  NavMeshData asset to disk, so PlayMode after a runtime bake loses it.
  The Inspector "Bake" button uses internal `NavMeshAssetManager` which is
  not in the public API. Same pattern as WarSceneSetup.

---

## 10. Test plan (after Sean approves this review)

1. Refresh assets → Unity compiles new files
2. `Tools/BT Duel/Create Duel Scene` → copies SampleScene → SampleScene_Duel
3. `Tools/BT Duel/Setup Duel Arena` → builds 40×40 arena + 2 NPCs
4. **Manual NavMesh bake** — select `/Ground` → NavMeshSurface inspector
   → click Bake → Ctrl+S to persist the NavMeshData reference into the
   scene asset
5. Play → expected:
   - Both NPCs detect each other within 0.3 s (sensor throttle)
   - Marksman opens fire from 12 m, Duelist closes via chase / flank
   - When Marksman runs out, expect FindCover → MoveToCover → Reload (3 s)
   - When Duelist HP drops < 0.6, expect a flank attempt (visible as the
     Duelist routing wide around the target instead of charging straight)
   - When Duelist HP < 0.3, expect retreat to cover, 1.5 s pause,
     re-engagement attempt
   - Outcome: undetermined — depends on flank success vs predicted-shot
     hit rate. Real test data.

---

## 11. Self-review fixes applied

Done a critical pass before delivering to Sean. Found and fixed 4 bugs:

1. **Reload.Start guarding** — both `FORCED RELOAD` and `ONGOING RELOAD`
   branches construct their own `new Reload(this)`. When selector flipped
   between them mid-reload, the new instance's Start re-called
   `_ammo.StartReload()` and reset the timer. **Fix**: `Reload.Start` skips
   `StartReload` if `IsReloading` is already true.

2. **MarksmanRunner branch order** — DANGER ZONE was below ONGOING RELOAD,
   meaning a Duelist closing during a reload kept the Marksman frozen in
   place instead of kiting. **Fix**: moved DANGER ZONE up to position 2
   (right after OPPORTUNITY). Reload state is preserved by fix #1 so the
   timer keeps progressing once distance opens and 3b re-enters.

3. **BuildRooflessBox sideIdx** — was passing yaw degrees and
   `Mathf.RoundToInt(yaw / 90)`, which uses banker's rounding so 225° / 90 =
   2.5 rounded to 2 (not 3). Entrances opened on the wrong side. **Fix**:
   API now takes `entranceSideIdx` int (0-3) directly.

4. **Pillar SafeDirection inverted** — for a wall, `SafeDirection` is the
   outward normal (NPC stands behind wall; threats come from outward).
   For pillars the NPC stands OUTSIDE the pillar, so the protected-against
   threat direction is the OPPOSITE of "outward" (pillar lies in -outward
   from NPC stand). Pillar cover with SafeDirection=+dir would falsely
   claim to protect against threats in +dir even though the pillar isn't
   between NPC and threat. **Fix**: pillar code sets `cp.SafeDirection = -dir`.

Cosmetic cleanup: wall comments in `BuildArena` were misleading ("protects
the north side from a south threat" — the convention is NPC-stand-side +
threat-blocked-side; rewrote inline).

## 13. V2 — Human-like overhaul

After V1's first play (`duel-state.jsonl` snapshot dump confirmed the
findings), Sean asked for a more human-like behaviour pass. Confirmed design
decisions: Q1=b (initial knowledge = direction only, no position),
Q2=a (LastKnownPos persists forever until reseen), Q3 = `a+b+b+a+a` →
careful aim (0.5 s) by default, snap shot allowed only in DANGER_ZONE,
no suppression fire, no fire while moving, no LOS-blind fire.

### 13.1 New files

```
Duel/PerceptionState.cs              ─ LastKnownPos? + LastKnownDir + LastSeenTime + HasContact
Duel/IPerceptionHolder.cs            ─ runner-exposes-perception interface
Duel/Actions/ConeVisionSense.cs      ─ OverlapSphere → cone FOV filter → LOS raycast filter
Duel/Actions/AdvanceToNextCover.cs   ─ pick CoverPoint closer to search target, NavMesh to it
Duel/Actions/PeekAndScan.cs          ─ stop, turn to face LastKnown, hold 1 s for vision sweep
```

### 13.2 Modified files

- `Duel/Actions/PredictedShoot.cs` — added `aimHoldSeconds` parameter.
  Aim-hold > 0 = "careful shot": rotate + lock on for N seconds BEFORE
  triggering the attack animation. Aim-hold = 0 = "snap shot": original
  behaviour, fires immediately at Start. Preemption mid-aim doesn't
  consume ammo.
- `Runners/DuelistRunner.cs` — implements `IPerceptionHolder`. New BT
  structure (see 13.4). Awake seeds `LastKnownDir` from `_ctx.EnemyDirection`.
- `Runners/MarksmanRunner.cs` — implements `IPerceptionHolder`. New BT
  structure (see 13.5). `attackRange` default 12 → 22, `retreatRange` 6 → 8,
  `engagementRange` 12 → 22, `aimHoldSeconds` 0.5, `viewRange` 30,
  `fovDeg` 110. Awake seeds `LastKnownDir`.
- `Duel/Editor/DuelSceneSetup.cs` — pass `enemyPos` per spawn so
  `_enemyDirection` becomes a true XZ unit vector (not legacy
  `(facing, 0, 0)` which mis-seeds perception on diagonal spawns).
  Marksman attackRange now spawned at 22 m to match `_engagementRange`.
- `Duel/DuelHud.cs` + `Duel/DuelStateDumper.cs` — new mode names
  (CHARGE / ADVANCE / INVESTIGATE_LAST_KNOWN / INVESTIGATE_DIRECTION /
  DANGER_SNAP_SHOT / DANGER_KITE / ...) + perception display
  (visible @ Xm / no LOS · last known @ Xm / no LOS · direction only).

### 13.3 Perception model

Each Runner owns one `PerceptionState`:

| Field | Initial | Updated by | Cleared when |
|---|---|---|---|
| `LastKnownPos: Vector3?` | `null` | ConeVisionSense on visible enemy | Never — only overwritten by new sighting |
| `LastKnownDir: Vector3` | spawn direction toward enemy spawn | ConeVisionSense on visible enemy | Never |
| `LastSeenTime: float` | 0 | ConeVisionSense on visible enemy | Never |
| `HasContact: bool` | computed `LastKnownPos != null` | derived | derived |

`ctx.Target` is now a "do I currently SEE the enemy" flag:
ConeVisionSense sets it on visible enemy + clears it on visibility loss
each frame. So `HasTarget` always means "actually visible right now",
not "ever sensed".

### 13.4 DuelistRunner BT (V2)

```
Root
└── Parallel
    ├── Sensor  · Repeater · Force(Success) · ConeVisionSense   (every frame)
    └── Decision · Repeater · Selector
        ├── 1) OPPORTUNITY        visible + target reloading
        │     Sequence: MoveToTarget(chase) → FaceTarget → WarriorCharge → Wait(0.2)
        │
        ├── 2) DEFENSIVE          visible + HpRatio < 0.30
        │     Selector
        │       ├── Sequence: FindCover → MoveToCover → Wait(1.5)
        │       └── MoveAway(attackRange × 2.5)            ← cover-failure fallback
        │
        ├── 3) TACTICAL_FLANK     visible + 0.30 ≤ HpRatio < 0.60
        │     Sequence: MoveToFlankPosition → MoveToTarget(chase) → FaceTarget → WarriorCharge → Wait
        │
        ├── 4) CHARGE             visible + dist ≤ attackRange
        │     Sequence: FaceTarget → WarriorCharge → Wait(0.2)
        │
        ├── 5) ADVANCE            (no .When — always)
        │     Sequence: AdvanceToNextCover(perception, cover) → PeekAndScan
        │     · AdvanceToNextCover picks the search position from
        │       visible target > LastKnownPos > LastKnownDir-projection.
        │     · When visibility is regained mid-scan, branches 1-4 take over
        │       on the next tick via reactive selector.
        │
        └── 6) Idle: Wait(0.5)   (perception not seeded — shouldn't normally hit)
```

### 13.5 MarksmanRunner BT (V2)

```
Root
└── Parallel
    ├── Sensor  · Repeater · Force(Success) · ConeVisionSense   (every frame)
    └── Decision · Repeater · Selector
        ├── 1) OPPORTUNITY            visible + ammo + target reloading
        │     Sequence: PredictedShoot(aim=0.5) → Wait(cooldown)
        │     (DEAD CODE in 1v1: Duelist never reloads. Kept for symmetry.)
        │
        ├── 2) DANGER + SNAP SHOT     visible + ammo + dist < retreat (8 m)
        │     Sequence: PredictedShoot(aim=0) → MoveAway(retreat + 1.5)
        │     · snap shot, then immediate kite. Q3-design.
        │
        ├── 3) DANGER + KITE          visible + dist < retreat
        │     MoveAway(retreat + 1.5)
        │     · matches when DANGER fires but ammo gone / mid-reload.
        │
        ├── 4) FORCED RELOAD          ammo = 0 + !isReloading
        │     Selector
        │       ├── Sequence: FindCover → MoveToCover → Reload
        │       └── Reload                              ← exposed-reload fallback
        │
        ├── 4b) ONGOING RELOAD        isReloading
        │     Reload     (re-entered after preemption — Reload.Start guards against double-init)
        │
        ├── 5) TACTICAL RELOAD        visible + !reloading + ammoLow + dist ≥ retreat+2
        │     Sequence: FindCover → MoveToCover → Reload
        │
        ├── 6) ENGAGE                 visible + ammo + dist ≤ engagement (22 m)
        │     Sequence: PredictedShoot(aim=0.5) → Wait(cooldown)
        │     · No LOS check needed — ConeVisionSense guarantees LOS for HasTarget.
        │
        ├── 7) ADVANCE                (no .When — always)
        │     Sequence: AdvanceToNextCover → PeekAndScan
        │     · Catches "visible target too far" AND "no LOS, only LastKnown".
        │
        └── 8) Idle: Wait(0.5)
```

### 13.6 Q3 fire-discipline matrix (as implemented)

| State | Aim hold | Why |
|---|---|---|
| ENGAGE (safe + LOS + in range) | 0.5 s | Q3c careful aim |
| OPPORTUNITY (target reloading) | 0.5 s | Q3a careful aim (target is helpless, take time) |
| DANGER_SNAP (kited close, have ammo + LOS) | 0 s | Q3b snap shot under pressure |
| DANGER_KITE (close, no ammo / reloading) | — | Q3b no fire, kite only |
| TACTICAL_RELOAD (low ammo, safe distance) | — | Don't fire, top off |
| ADVANCE (no LOS, only LastKnown) | — | Q3a no suppression fire toward LastKnown |
| Investigating (no LOS, no LastKnown) | — | Q3a no blind fire |

### 13.7 Known scope limits / next-pass candidates

- **No "find vantage point with LOS to LastKnown" search.** AdvanceToNextCover
  picks cover that's CLOSER to the search target but doesn't verify that
  cover has LOS back to enemy/LastKnown. This means peek-then-fire isn't
  guaranteed to produce a shot opportunity each cycle.
- **No cone vision visualization in Scene view.** Hard to debug without a
  Gizmos overlay drawing the FOV wedge. Worth adding for next iteration.
- **Vision raycast might self-hit** (start at eyeY = 1.2 m, inside own
  CapsuleCollider). Relies on Unity's convex-collider-from-inside behaviour.
  ArcherShoot uses the same convention; hasn't broken yet.
- **No "Q4" polish items** (reaction time, damage stress, hearing, last-seen
  velocity prediction) — Sean confirmed: record only, don't implement.

### 13.8 V2 self-review findings (fixed before delivery)

Reviewed each new file for compile / logic errors. Found and fixed in place:

- `DuelSceneSetup.ApplyBaseStats` signature change: `float facing` →
  `Vector3 enemyDir`. Callers updated. Setup script's clone-template facing
  also switched to consume the same XZ unit vector.
- `Marksman.attackRange` bumped 12 → 22 to match `_engagementRange` (was
  inconsistent: BT used 22, ApplyBaseStats wrote 12).
- `HUD/Dumper` mode-inference constants `MARKSMAN_RETREAT 6 → 8`,
  `MARKSMAN_ENGAGE 12 → 22` synced with new Runner defaults.

## 14. Open questions for Sean

Before running, three things to confirm:

1. **Stats balance** — Duelist 130 HP / 22 dmg / 4.2 speed vs Marksman
   90 HP / 22 dmg / 3.6 speed / 5 ammo / 3 s reload / 1 s cooldown.
   Untested. Quick math: Marksman max DPS in window = 5 shots / 5 s
   (1 cd × 4 + final shot) ≈ 22 dps. Duelist swing every ~1 s = 22 dps.
   Marksman's 5 shots = 110 dmg, kills Duelist if no misses. Duelist
   needs ~6 swings ≈ 6 s to kill Marksman. **Tight, slight Marksman edge
   if zero misses, Duelist wins if 1+ miss.** Want a different lean?

2. **Arena geometry** — Two 8×8 buildings + 4 high walls + 2 low walls +
   6 pillars (each pillar = 4 cover points). ~30 cover points total in a
   40×40 arena. Too many / too few / just right?

3. **Damage feedback** — Currently no UI/HUD for the duel scene.
   Should I add a tiny OnGUI block showing each NPC's HP + ammo + current
   BT branch so debugging is easier during play?
