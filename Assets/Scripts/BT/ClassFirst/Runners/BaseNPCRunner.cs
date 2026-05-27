using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Shared base for Class-First NPC runners.
    /// Holds Inspector-tunable stats, wires up the BTContext, implements
    /// IDamageable for the sensor system, and handles death.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class BaseNPCRunner : MonoBTRunner<BTContext>, IDamageable, IHealable
    {
        [Header("Refs")]
        [SerializeField] protected NavMeshAgent _agent;
        [SerializeField] protected Animator _anim;
        [SerializeField] protected Renderer _bodyRenderer;

        [Header("Identity")]
        [SerializeField] protected Faction _faction;

        [Header("Stats")]
        [SerializeField] protected float _walkSpeed = 3f;
        [SerializeField] protected float _attackRange = 3f;
        [SerializeField] protected float _sensorRadius = 10f;
        [SerializeField] protected float _maxHp = 100f;
        [SerializeField] protected float _attackDamage = 10f;

        [Header("Sensor")]
        [SerializeField] protected LayerMask _sensorLayer;

        [Header("Patrol (optional)")]
        [SerializeField] protected Vector3[] _patrolPoints;

        [Header("March (war demo)")]
        [Tooltip("Direction to march when no enemy is in sense. Zero = stay put.")]
        [SerializeField] protected Vector3 _enemyDirection = Vector3.zero;

        protected BTContext _ctx;

        // ---- IDamageable --------------------------------------------------
        public bool IsAlive => _ctx != null && _ctx.Hp > 0f;
        public Faction Faction => _faction;
        public Transform Transform => transform;

        // Exposed for SenseAlly / debug tools.
        public float HpRatio => _ctx?.HpRatio ?? 0f;

        // Virtual so duel runners can hook in "damage = perception update" —
        // taking a hit locates the attacker even without LOS.
        public virtual void TakeDamage(float damage, IDamageable source)
        {
            if (!IsAlive) return;
            _ctx.Hp = Mathf.Max(0f, _ctx.Hp - damage);
            if (_ctx.Hp <= 0f)
                Die();
        }

        public void Heal(float amount, IDamageable source)
        {
            if (!IsAlive) return;
            _ctx.Hp = Mathf.Min(_ctx.MaxHp, _ctx.Hp + amount);
        }

        protected virtual void Die()
        {
            // Stop the BT cleanly first.
            try { Tree?.Abort(_ctx); } catch { /* ignore */ }
            if (_agent != null && _agent.isOnNavMesh)
                _agent.ResetPath();
            gameObject.SetActive(false);
        }

        // ---- Lifecycle ----------------------------------------------------
        protected override void Awake()
        {
            var swTotal = System.Diagnostics.Stopwatch.StartNew();

            // ---- ctx build segment ----
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            // includeInactive=true so we still find Animators on disabled visuals.
            if (_anim == null) _anim = GetComponentInChildren<Animator>(true);
            if (_bodyRenderer == null) _bodyRenderer = GetComponentInChildren<Renderer>(true);

            _ctx = new BTContext(
                self: transform,
                selfAsDamageable: this,
                agent: _agent,
                anim: _anim,
                sensorLayer: _sensorLayer,
                faction: _faction,
                walkSpeed: _walkSpeed,
                attackRange: _attackRange,
                sensorRadius: _sensorRadius,
                maxHp: _maxHp,
                attackDamage: _attackDamage,
                enemyDirection: _enemyDirection,
                patrolPoints: _patrolPoints,
                // BTContext defaults to 16, which is too small for any scene
                // with NPCs + arena obstacles — Physics.OverlapSphereNonAlloc
                // can fill the buffer with static walls/pillars and Sense will
                // silently miss the actual enemy (observed in the duel demo:
                // Marksman took 6 s to acquire Duelist that was already 41 m
                // inside its 45 m sensor radius). 64 leaves headroom.
                overlapBufferSize: 64);

            SetContext(_ctx);
            long ctxBuildTicks = swTotal.ElapsedTicks;

            // ---- base.Awake segment (includes CreateTree + new BTRunner) ----
            base.Awake();
            swTotal.Stop();
            long totalTicks = swTotal.ElapsedTicks;
            long baseAwakeTicks = totalTicks - ctxBuildTicks;

            War.BTInitStats.Record(totalTicks, ctxBuildTicks, baseAwakeTicks);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _sensorRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            if (_patrolPoints != null && _patrolPoints.Length > 0)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < _patrolPoints.Length; i++)
                {
                    Gizmos.DrawWireCube(_patrolPoints[i], Vector3.one * 0.5f);
                    Vector3 next = _patrolPoints[(i + 1) % _patrolPoints.Length];
                    Gizmos.DrawLine(_patrolPoints[i], next);
                }
            }
        }
    }
}
