using BehaviorTree.ClassFirst.Actions;
using BehaviorTree.ClassFirst.Duel;
using BehaviorTree.ClassFirst.Duel.Actions;
using BehaviorTree.ClassFirst.Duel.Conditions;
using UnityEngine;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// 1v1 melee duelist with human-like perception (cone FOV + LOS) and
    /// cover-aware advance. No longer rushes blindly.
    ///
    /// <para>Behavior priorities, top-down (selector picks the first match):</para>
    /// <list type="number">
    /// <item><b>Opportunity</b> — target visible + reloading → rush + charge.</item>
    /// <item><b>Defensive</b> — HP &lt; 30% + visible target → commit to cover, wait.</item>
    /// <item><b>Tactical Flank</b> — 30% &lt;= HP &lt; 60% + visible → flank position, then engage.</item>
    /// <item><b>Charge</b> — visible target in melee range → face + WarriorCharge.</item>
    /// <item><b>Advance</b> — anything else (visible-but-far OR no visual, only LastKnown)
    ///     → cover-to-cover step toward search target + peek + scan.</item>
    /// <item><b>Idle</b> — no target and no memory whatsoever (shouldn't normally hit because
    ///     DuelSceneSetup seeds LastKnownDir at spawn).</item>
    /// </list>
    ///
    /// <para>Sensor branch runs <see cref="ConeVisionSense"/> EVERY frame
    /// (no throttle): visibility can change any frame as the opponent moves
    /// behind walls or this NPC turns its head.</para>
    /// </summary>
    public sealed class DuelistRunner : BaseNPCRunner, ICoverHolder, IPerceptionHolder, INoiseListener
    {
        [Header("Duelist tactics")]
        [SerializeField] private float _lowHpRatio = 0.30f;
        [SerializeField] private float _midHpRatio = 0.60f;
        [SerializeField] private float _attackCooldown = 0.2f;
        [SerializeField] private float _coverHoldSeconds = 1.5f;
        [SerializeField] private float _coverSearchRadius = 25f;
        [SerializeField] private float _flankAngleDeg = 120f;
        [SerializeField] private float _flankRadius = 4f;

        [Header("Perception")]
        [SerializeField] private float _fovDeg = 110f;
        [SerializeField] private float _viewRange = 25f;
        [SerializeField] private float _advanceStep = 9f;
        [SerializeField] private float _scanDuration = 1.0f;

        public CoverPoint CurrentCover { get; set; }

        private readonly PerceptionState _perception = new PerceptionState();
        public PerceptionState PerceptionState => _perception;

        protected override void Awake()
        {
            base.Awake();
            // Per Sean's Q1=b: at spawn the NPC knows the direction toward the
            // enemy but not the exact position. _enemyDirection is set by
            // DuelSceneSetup to (otherSpawn - mySpawn).normalized.
            if (_ctx != null && _ctx.EnemyDirection.sqrMagnitude > 0.01f)
                _perception.LastKnownDir = _ctx.EnemyDirection.normalized;
        }

        private void OnEnable() => NoiseBus.Register(this);

        protected override void OnDisable()
        {
            NoiseBus.Unregister(this);
            base.OnDisable(); // chain into MonoBTRunner — aborts the BT runner
        }

        // --- INoiseListener ---
        public Vector3 ListenerPosition => transform.position;

        public void OnNoise(Vector3 sourcePos, float intensity)
        {
            // Hearing updates LastKnown — gives a position fix without LOS.
            _perception.LastKnownPos = sourcePos;
            Vector3 d = sourcePos - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) _perception.LastKnownDir = d.normalized;
        }

        // --- Damage reaction ---
        // Taking a hit gives a free position fix on the attacker, even with
        // no visual LOS. Without this, an unseen Marksman could chip Duelist
        // HP from cover and Duelist would never know which way to advance.
        public override void TakeDamage(float damage, IDamageable source)
        {
            base.TakeDamage(damage, source);
            if (source != null)
            {
                _perception.LastKnownPos = source.Transform.position;
                Vector3 d = source.Transform.position - transform.position; d.y = 0f;
                if (d.sqrMagnitude > 1e-4f) _perception.LastKnownDir = d.normalized;
            }
        }

        protected override INode<BTContext> CreateTree()
        {
            float chaseStop = Mathf.Max(0.5f, _attackRange - 0.3f);

            return BTBuilder<BTContext>.Build(root => root
                .Parallel(par => par

                    // --- Sensor branch: cone FOV + LOS, every frame ---
                    .Repeater(rep => rep
                        .Force(NodeStatus.Success, force => force
                            .Do(new ConeVisionSense(this, _fovDeg, _viewRange))
                        )
                    )

                    // --- Decision branch ---
                    .Repeater(rep => rep
                        .Selector(sel => sel

                            // 1) OPPORTUNITY — visible target is reloading → rush.
                            .When((ctx, _) => ctx.HasTarget && IsTargetReloading.Check(ctx),
                                _ => _.Sequence(seq => seq
                                    .Do(new MoveToTarget(stopDistance: chaseStop))
                                    .Do(new FaceTarget())
                                    .Do(new WarriorCharge())
                                    .Do(new WaitTimer(_attackCooldown))
                                ))

                            // 2) DEFENSIVE — low HP. Hide in cover, hold.
                            //    Only triggers when we still see the threat (otherwise we'd
                            //    be in Advance/Investigate already).
                            .When((ctx, _) => ctx.HasTarget && ctx.HpRatio < _lowHpRatio,
                                _ => _.Selector(defSel => defSel
                                    .Sequence(seq => seq
                                        .Do(new FindCover(this, _coverSearchRadius))
                                        .Do(new MoveToCover(this))
                                        .Do(new WaitTimer(_coverHoldSeconds))
                                    )
                                    .Do(new MoveAway(desiredDistance: _attackRange * 2.5f))
                                ))

                            // 3) TACTICAL FLANK — mid HP + visible target → flank.
                            .When((ctx, _) => ctx.HasTarget && ctx.HpRatio < _midHpRatio,
                                _ => _.Sequence(seq => seq
                                    .Do(new MoveToFlankPosition(_flankAngleDeg, _flankRadius))
                                    .Do(new MoveToTarget(stopDistance: chaseStop))
                                    .Do(new FaceTarget())
                                    .Do(new WarriorCharge())
                                    .Do(new WaitTimer(_attackCooldown))
                                ))

                            // 4) CHARGE — visible target in melee range.
                            .When((ctx, _) => ctx.HasTarget && ctx.TargetDistance <= ctx.AttackRange,
                                _ => _.Sequence(seq => seq
                                    .Do(new FaceTarget())
                                    .Do(new WarriorCharge())
                                    .Do(new WaitTimer(_attackCooldown))
                                ))

                            // 5) ADVANCE — covers both "visible-but-far" and
                            //    "no visual, only LastKnown". AdvanceToNextCover
                            //    uses the perception+target heuristic to pick a
                            //    search position (visible target > LastKnownPos
                            //    > LastKnownDir projection).
                            .Sequence(seq => seq
                                .Do(new AdvanceToNextCover(this, this, _advanceStep))
                                .Do(new PeekAndScan(this, _scanDuration))
                            )

                            // 6) Idle (no perception at all — only if Setup forgot to seed).
                            .Do(new WaitTimer(0.5f))
                        )
                    )
                )
            );
        }
    }
}
