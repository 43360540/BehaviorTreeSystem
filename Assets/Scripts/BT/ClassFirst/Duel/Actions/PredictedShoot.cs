using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Duel.Actions
{
    /// <summary>
    /// Marksman shot with optional aim-hold phase + committed impact resolution.
    /// Damage applies in <see cref="Tick"/> at a fixed in-action impact time —
    /// once the bullet "fires" it can't be cancelled by BT preemption. This
    /// mirrors the V3 WarriorCharge redesign: a physical commitment that
    /// doesn't depend on BT staying in the same branch.
    ///
    /// <para>Timeline from <see cref="Start"/>:</para>
    /// <list type="bullet">
    /// <item><b>0 .. aimHoldSeconds</b>: rotate toward predicted point, no fire
    ///       (the "lining up the shot" beat).</item>
    /// <item><b>aimHoldSeconds</b>: trigger attack animation.</item>
    /// <item><b>aimHoldSeconds + IMPACT_DELAY</b>: resolve raycast, consume
    ///       one shot, deal damage if the bullet line hit the target. Emit
    ///       gunshot noise here too (the shot has actually "happened").</item>
    /// <item><b>animation end</b>: <see cref="Tick"/> returns Success and
    ///       <see cref="Stop"/> cleans up.</item>
    /// </list>
    ///
    /// <para>Two preset use-cases per Sean's Q3:</para>
    /// <list type="bullet">
    /// <item><b>Careful</b> (ENGAGE / OPPORTUNITY): aimHold = 0.5 s.</item>
    /// <item><b>Snap</b> (DANGER_ZONE): aimHold = 0 — instant trigger.</item>
    /// </list>
    ///
    /// <para>Hit resolution uses mid-chest height (y = 0.5 from transform) +
    /// a small forward offset so the raycast origin is OUTSIDE the shooter's
    /// own capsule (avoids Unity's "convex from-inside skip" being unreliable
    /// at the capsule shell edge — which would otherwise treat self as a
    /// blocker and silently miss).</para>
    /// </summary>
    public sealed class PredictedShoot : ActionBase<BTContext>
    {
        private const float IMPACT_DELAY_AFTER_TRIGGER = 0.15f;

        private readonly IRangedAmmoController _ammo;
        private readonly float _bulletSpeed;
        private readonly float _aimHoldSeconds;

        private IDamageable _aimedAt;
        private Vector3 _predictedPoint;
        private float _elapsed;
        private bool _animationStarted;
        private bool _impactResolved;
        private bool _animatorReady;

        public PredictedShoot(IRangedAmmoController ammo, float bulletSpeed = 25f, float aimHoldSeconds = 0f)
        {
            _ammo = ammo;
            _bulletSpeed = Mathf.Max(1f, bulletSpeed);
            _aimHoldSeconds = Mathf.Max(0f, aimHoldSeconds);
        }

        public override void Start(BTContext ctx)
        {
            _aimedAt = ctx.Target;
            _animatorReady = ctx.Anim != null;
            _elapsed = 0f;
            _animationStarted = false;
            _impactResolved = false;

            if (_ammo != null && _ammo.Ammo <= 0)
            {
                _aimedAt = null;
                return;
            }

            if (IsAgentReady(ctx))
            {
                ctx.Agent.updateRotation = false;
                ctx.Agent.isStopped = true;
            }

            _predictedPoint = ComputeLeadPoint(ctx);

            // Snap-shot path: aim hold is zero, fire immediately.
            if (_aimHoldSeconds <= 0f) TriggerAnimation(ctx);
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (_aimedAt == null) return NodeStatus.Failure;

            _elapsed += dt;

            // Keep refreshing the prediction every tick so a last-second
            // course change still gets reflected at impact time.
            _predictedPoint = ComputeLeadPoint(ctx);

            // Rotate toward the predicted point throughout aim + fire phases.
            Vector3 dir = _predictedPoint - ctx.Self.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-6f)
            {
                ctx.Self.rotation = Quaternion.RotateTowards(
                    ctx.Self.rotation, Quaternion.LookRotation(dir.normalized), 720f * dt);
            }

            // Aim hold phase — wait, don't trigger animation yet.
            if (!_animationStarted)
            {
                if (_elapsed < _aimHoldSeconds) return NodeStatus.Running;
                TriggerAnimation(ctx);
            }

            // Impact moment — fires inside Tick so BT preemption AFTER this
            // point can no longer cancel the shot. ConsumeShot + raycast
            // resolve damage. Idempotent via _impactResolved flag.
            float impactAt = _aimHoldSeconds + IMPACT_DELAY_AFTER_TRIGGER;
            if (!_impactResolved && _elapsed >= impactAt)
            {
                _impactResolved = true;
                TryResolveImpact(ctx);
            }

            if (!_animatorReady) return NodeStatus.Success;
            return ctx.Anim.GetBool(ctx.IsAttackingHash)
                ? NodeStatus.Running
                : NodeStatus.Success;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (_animatorReady)
            {
                ctx.Anim.ResetTrigger(ctx.AttackTriggerHash);
                ctx.Anim.SetBool(ctx.IsAttackingHash, false);
            }
            if (IsAgentReady(ctx))
            {
                ctx.Agent.updateRotation = true;
                ctx.Agent.isStopped = false;
            }
            // Damage was resolved (or not) at impact time in Tick — Stop is
            // purely cleanup now.
        }

        public override void Reset()
        {
            _aimedAt = null;
            _predictedPoint = default;
            _elapsed = 0f;
            _animationStarted = false;
            _impactResolved = false;
        }

        // -----------------------------------------------------------------

        private void TriggerAnimation(BTContext ctx)
        {
            _animationStarted = true;
            if (_animatorReady)
            {
                ctx.Anim.SetTrigger(ctx.AttackTriggerHash);
                ctx.Anim.SetBool(ctx.IsAttackingHash, true);
            }
        }

        private void TryResolveImpact(BTContext ctx)
        {
            if (_aimedAt == null || !_aimedAt.IsAlive) return;
            if (_ammo == null || !_ammo.ConsumeShot()) return;

            // Mid-chest raycast (y = 0.5 from transform). NPC capsule spans
            // transform.y +/- 1; a ray at +0.5 is well inside the body. To
            // avoid the unreliable "Unity skips convex collider from inside"
            // edge case (sometimes self IS returned at the capsule shell),
            // offset the origin 0.6 m TOWARD the target — that puts us
            // outside the shooter's own capsule (radius 0.5) before the
            // raycast even starts.
            const float eyeY = 0.5f;
            Vector3 shooterPos = ctx.Self.position + Vector3.up * eyeY;
            Vector3 targetPoint = _predictedPoint + Vector3.up * eyeY;
            Vector3 dir = targetPoint - shooterPos;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist < 0.05f)
            {
                _aimedAt.TakeDamage(ctx.AttackDamage, ctx.SelfAsDamageable);
                NoiseBus.EmitGunshot(ctx.Self.position, _ammo as INoiseListener);
                return;
            }
            dir /= dist;
            Vector3 origin = shooterPos + dir * 0.6f;
            float castDist = Mathf.Max(0.1f, dist - 0.6f) + 0.1f;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, castDist))
            {
                var hitDamageable = hit.collider.GetComponentInParent<IDamageable>();
                if (ReferenceEquals(hitDamageable, _aimedAt))
                    _aimedAt.TakeDamage(ctx.AttackDamage, ctx.SelfAsDamageable);
                // else: blocked by obstacle, or struck a different actor — miss.
            }
            // else: no collider in path within castDist — target was past it but
            // raycast missed. Spatially possible if predicted point is in open air
            // beyond the target; treat as miss to be safe.

            NoiseBus.EmitGunshot(ctx.Self.position, _ammo as INoiseListener);
        }

        private Vector3 ComputeLeadPoint(BTContext ctx)
        {
            if (_aimedAt == null) return ctx.Self.position;

            Vector3 tgtPos = _aimedAt.Transform.position;
            Vector3 tgtVel = Vector3.zero;
            var tgtAgent = _aimedAt.Transform.GetComponentInParent<NavMeshAgent>();
            if (tgtAgent != null) tgtVel = tgtAgent.velocity;

            // Iterative lead — predicted point depends on travel time, travel
            // time depends on distance to predicted point. One round of
            // refinement is enough at our ranges.
            float dist = Vector3.Distance(ctx.Self.position, tgtPos);
            float t1 = dist / _bulletSpeed;
            Vector3 pred1 = tgtPos + tgtVel * t1;

            float dist2 = Vector3.Distance(ctx.Self.position, pred1);
            float t2 = dist2 / _bulletSpeed;
            return tgtPos + tgtVel * t2;
        }

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
