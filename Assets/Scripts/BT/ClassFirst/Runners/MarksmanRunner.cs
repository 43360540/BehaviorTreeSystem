using BehaviorTree.ClassFirst.Actions;
using BehaviorTree.ClassFirst.Duel;
using BehaviorTree.ClassFirst.Duel.Actions;
using BehaviorTree.ClassFirst.Duel.Conditions;
using UnityEngine;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// 1v1 ranged marksman with human-like perception (cone FOV + LOS), aim-hold
    /// fire discipline, and cover-to-cover investigate behavior.
    ///
    /// <para>Per Sean's Q3 firing rules:</para>
    /// <list type="bullet">
    /// <item>ENGAGE / OPPORTUNITY fire — 0.5 s aim hold before shot lands (careful).</item>
    /// <item>DANGER_ZONE + LOS + ammo — snap shot (0 s aim hold) then kite.</item>
    /// <item>DANGER_ZONE without LOS / ammo — pure kite.</item>
    /// <item>No fire without a visible target (no "suppression fire" toward LastKnown).</item>
    /// </list>
    ///
    /// <para>Tactical priorities, top-down:</para>
    /// <list type="number">
    /// <item>OPPORTUNITY — visible target reloading + I have ammo → careful shot.</item>
    /// <item>DANGER_ZONE + LOS + ammo → snap shot then kite.</item>
    /// <item>DANGER_ZONE → kite (preempts reload so we don't freeze under melee pressure).</item>
    /// <item>FORCED RELOAD — out of ammo → cover-then-reload (fallback: reload exposed).</item>
    /// <item>ONGOING RELOAD — keep pumping the timer when re-entered after preemption.</item>
    /// <item>TACTICAL RELOAD — ammo low + target far enough.</item>
    /// <item>ENGAGE — visible target + in range + ammo → careful shot.</item>
    /// <item>ADVANCE — visible-but-far OR investigate (no LOS, only LastKnown).</item>
    /// </list>
    /// </summary>
    public sealed class MarksmanRunner : BaseNPCRunner, ICoverHolder, IRangedAmmoController, IPerceptionHolder, INoiseListener
    {
        [Header("Marksman tactics")]
        [SerializeField] private int   _maxAmmo = 5;
        [SerializeField] private float _reloadDuration = 3f;
        [SerializeField] private float _shootCooldown = 1.0f;
        [SerializeField] private float _retreatRange = 8f;
        [SerializeField] private float _engagementRange = 22f;
        [SerializeField] private float _lowAmmoRatio = 0.4f;
        [SerializeField] private float _coverSearchRadius = 25f;
        [SerializeField] private float _bulletSpeed = 25f;
        [SerializeField] private float _aimHoldSeconds = 0.5f;

        [Header("Perception")]
        [SerializeField] private float _fovDeg = 110f;
        [SerializeField] private float _viewRange = 30f;
        [SerializeField] private float _advanceStep = 10f;
        [SerializeField] private float _scanDuration = 1.0f;

        // --- IRangedAmmo / Controller state ---
        public int  Ammo        => _currentAmmo;
        public int  MaxAmmo     => _maxAmmo;
        public bool IsReloading => _isReloading;

        public CoverPoint CurrentCover { get; set; }

        private readonly PerceptionState _perception = new PerceptionState();
        public PerceptionState PerceptionState => _perception;

        private int   _currentAmmo;
        private float _reloadTimer;
        private bool  _isReloading;

        public void StartReload()
        {
            _isReloading = true;
            _reloadTimer = _reloadDuration;
            _currentAmmo = 0;
        }

        public void TickReload(float dt)
        {
            if (!_isReloading) return;
            _reloadTimer -= dt;
            if (_reloadTimer <= 0f)
            {
                _isReloading = false;
                _currentAmmo = _maxAmmo;
            }
        }

        public bool ConsumeShot()
        {
            if (_currentAmmo <= 0) return false;
            _currentAmmo--;
            return true;
        }

        protected override void Awake()
        {
            base.Awake();
            _currentAmmo = _maxAmmo;
            _reloadTimer = 0f;
            _isReloading = false;
            // Per Sean's Q1=b: at spawn we know the direction toward the enemy
            // but not the exact position. _enemyDirection is set by DuelSceneSetup
            // to (otherSpawn - mySpawn).normalized.
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
            _perception.LastKnownPos = sourcePos;
            Vector3 d = sourcePos - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) _perception.LastKnownDir = d.normalized;
        }

        // --- Damage reaction ---
        // Taking a hit instantly locates the attacker, even without LOS.
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

                            // 1) OPPORTUNITY — visible target reloading + ammo + careful shot.
                            //    (ConeVisionSense guarantees LOS for HasTarget — no LOS check here.)
                            .When((ctx, _) => ctx.HasTarget
                                          && !_isReloading
                                          && _currentAmmo > 0
                                          && IsTargetReloading.Check(ctx),
                                _ => _.Sequence(seq => seq
                                    .Do(new PredictedShoot(this, _bulletSpeed, aimHoldSeconds: _aimHoldSeconds))
                                    .Do(new WaitTimer(_shootCooldown))
                                ))

                            // 2) DANGER + LOS + ammo → snap shot (aim hold 0) then kite.
                            .When((ctx, _) => ctx.HasTarget
                                          && !_isReloading
                                          && _currentAmmo > 0
                                          && ctx.TargetDistance < _retreatRange,
                                _ => _.Sequence(seq => seq
                                    .Do(new PredictedShoot(this, _bulletSpeed, aimHoldSeconds: 0f))
                                    .Do(new MoveAway(desiredDistance: _retreatRange + 1.5f))
                                ))

                            // 3) DANGER (no LOS / no ammo) → pure kite.
                            //    Triggers if we lost sight while close (target ducked behind
                            //    cover at point-blank) — still need to keep distance.
                            .When((ctx, _) => ctx.HasTarget && ctx.TargetDistance < _retreatRange,
                                _ => _.Do(new MoveAway(desiredDistance: _retreatRange + 1.5f)))

                            // 4) FORCED RELOAD — out of ammo.
                            .When((ctx, _) => _currentAmmo <= 0 && !_isReloading,
                                _ => _.Selector(reloadSel => reloadSel
                                    .Sequence(seq => seq
                                        .Do(new FindCover(this, _coverSearchRadius))
                                        .Do(new MoveToCover(this))
                                        .Do(new Reload(this))
                                    )
                                    .Do(new Reload(this))
                                ))

                            // 4b) ONGOING RELOAD — re-entered after preemption (e.g. DANGER kited us mid-reload).
                            .When((ctx, _) => _isReloading,
                                _ => _.Do(new Reload(this)))

                            // 5) TACTICAL RELOAD — ammo low + safe distance.
                            .When((ctx, _) => ctx.HasTarget
                                          && !_isReloading
                                          && IsAmmoLow.Check(this, _lowAmmoRatio)
                                          && ctx.TargetDistance >= _retreatRange + 2f,
                                _ => _.Sequence(seq => seq
                                    .Do(new FindCover(this, _coverSearchRadius))
                                    .Do(new MoveToCover(this))
                                    .Do(new Reload(this))
                                ))

                            // 6) ENGAGE — visible + in range + ammo + careful shot (0.5s aim).
                            .When((ctx, _) => ctx.HasTarget
                                          && _currentAmmo > 0
                                          && !_isReloading
                                          && ctx.TargetDistance <= _engagementRange,
                                _ => _.Sequence(seq => seq
                                    .Do(new PredictedShoot(this, _bulletSpeed, aimHoldSeconds: _aimHoldSeconds))
                                    .Do(new WaitTimer(_shootCooldown))
                                ))

                            // 7) ADVANCE — covers "visible but out of range" AND "investigate"
                            //    (no visible target, only LastKnown). AdvanceToNextCover
                            //    automatically picks the right search position.
                            .Sequence(seq => seq
                                .Do(new AdvanceToNextCover(this, this, _advanceStep))
                                .Do(new PeekAndScan(this, _scanDuration))
                            )

                            // 8) Idle (no perception state — shouldn't normally hit).
                            .Do(new WaitTimer(0.5f))
                        )
                    )
                )
            );
        }
    }
}
