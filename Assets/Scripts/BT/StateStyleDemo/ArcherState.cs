using BehaviorTree.ClassFirst;
using BehaviorTree.StateStyle;
using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.StateStyleDemo
{
    /// <summary>
    /// State-Style Archer: kite the nearest enemy.
    /// Retreat if too close, shoot if in shoot range, approach otherwise.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ArcherState
        : StateStyleBase<ArcherState, ArcherState.State>, IDamageable
    {
        public enum State { Sense, Approach, Retreat, Shoot, Idle }

        [Header("Refs")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Animator _anim;
        [Header("Identity")]
        [SerializeField] private Faction _faction;
        [Header("Stats")]
        [SerializeField] private float _walkSpeed = 3.5f;
        [SerializeField] private float _attackRange = 8f;     // shoot range (max)
        [SerializeField] private float _retreatRange = 4.5f;  // run away if closer than this
        [SerializeField] private float _shootCooldown = 1.0f;
        [SerializeField] private float _sensorRadius = 14f;
        [SerializeField] private float _maxHp = 70f;
        [SerializeField] private float _attackDamage = 18f;
        [SerializeField] private LayerMask _sensorLayer;

        private float _hp;
        private IDamageable _target;
        private IDamageable _aimedAt;
        private float _idleElapsed;
        private float _shootElapsed;
        private readonly Collider[] _overlapBuffer = new Collider[16];
        private int _attackTriggerHash;
        private int _isAttackingHash;

        public bool IsAlive => _hp > 0f;
        public Faction Faction => _faction;
        public Transform Transform => transform;
        public void TakeDamage(float damage, IDamageable source)
        {
            if (!IsAlive) return;
            _hp = Mathf.Max(0f, _hp - damage);
            if (_hp <= 0f) gameObject.SetActive(false);
        }

        protected override void Awake()
        {
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (_anim == null) _anim = GetComponentInChildren<Animator>(true);
            _hp = _maxHp;
            _attackTriggerHash = Animator.StringToHash("AttackTrigger");
            _isAttackingHash = Animator.StringToHash("IsAttacking");
            base.Awake();
        }

        private bool HasTarget => _target != null && _target.IsAlive;
        private float TargetDistance =>
            HasTarget ? Vector3.Distance(transform.position, _target.Transform.position) : float.PositiveInfinity;
        private Vector3 TargetDirFlat
        {
            get
            {
                if (!HasTarget) return transform.forward;
                Vector3 d = _target.Transform.position - transform.position;
                d.y = 0;
                return d.sqrMagnitude > 1e-6f ? d.normalized : transform.forward;
            }
        }

        // --- Sense ----------------------------------------------------------
        [StateDef("Sense", Phase.Tick)]
        private NodeStatus SenseTick(float dt)
        {
            _target = StateHelpers.FindClosestEnemy(transform, _sensorLayer, _sensorRadius, _overlapBuffer, _faction);
            return _target != null ? NodeStatus.Success : NodeStatus.Failure;
        }

        // --- Approach (close to shoot range) -------------------------------
        [StateDef("Approach", Phase.Start)]
        private void ApproachStart() { _agent.speed = _walkSpeed; _agent.isStopped = false; }
        [StateDef("Approach", Phase.Tick)]
        private NodeStatus ApproachTick(float dt)
        {
            if (!HasTarget) return NodeStatus.Failure;
            float closeShoot = Mathf.Max(_retreatRange + 0.5f, _attackRange - 1f);
            NodeStatus s = StateHelpers.WalkTo(_agent, _target.Transform.position);
            return TargetDistance <= closeShoot ? NodeStatus.Success : s;
        }
        [StateDef("Approach", Phase.Stop)]
        private void ApproachStop(NodeStatus s)
        {
            if (_agent.isOnNavMesh) _agent.ResetPath();
        }

        // --- Retreat -------------------------------------------------------
        [StateDef("Retreat", Phase.Start)]
        private void RetreatStart() { _agent.speed = _walkSpeed; _agent.isStopped = false; }
        [StateDef("Retreat", Phase.Tick)]
        private NodeStatus RetreatTick(float dt)
        {
            if (!HasTarget) return NodeStatus.Failure;
            float dist = TargetDistance;
            float desired = _retreatRange + 1.5f;
            if (dist >= desired) return NodeStatus.Success;
            Vector3 away = -TargetDirFlat;
            Vector3 goal = transform.position + away * (desired - dist + 1.5f);
            if (NavMesh.SamplePosition(goal, out NavMeshHit hit, 3f, _agent.areaMask))
            {
                _agent.SetDestination(hit.position);
                return NodeStatus.Running;
            }
            // perpendicular dodge fallback
            Vector3 dodge = Vector3.Cross(Vector3.up, TargetDirFlat).normalized;
            if (NavMesh.SamplePosition(transform.position + dodge * 1.5f, out hit, 3f, _agent.areaMask))
            {
                _agent.SetDestination(hit.position);
                return NodeStatus.Running;
            }
            return NodeStatus.Failure;
        }
        [StateDef("Retreat", Phase.Stop)]
        private void RetreatStop(NodeStatus s)
        {
            if (_agent.isOnNavMesh) _agent.ResetPath();
        }

        // --- Shoot ---------------------------------------------------------
        [StateDef("Shoot", Phase.Start)]
        private void ShootStart()
        {
            _aimedAt = _target;
            _shootElapsed = 0f;
            if (_anim == null) return;
            _agent.updateRotation = false;
            _agent.isStopped = true;
            _anim.SetTrigger(_attackTriggerHash);
            _anim.SetBool(_isAttackingHash, true);
        }
        [StateDef("Shoot", Phase.Tick)]
        private NodeStatus ShootTick(float dt)
        {
            if (_anim == null) return NodeStatus.Failure;

            // Keep aiming at the original target.
            if (_aimedAt != null && _aimedAt.IsAlive)
            {
                Vector3 dir = _aimedAt.Transform.position - transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 1e-6f)
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, Quaternion.LookRotation(dir.normalized), 720f * dt);
            }

            if (_anim.GetBool(_isAttackingHash))
                return NodeStatus.Running;

            _shootElapsed += dt;
            return _shootElapsed >= _shootCooldown ? NodeStatus.Success : NodeStatus.Running;
        }
        [StateDef("Shoot", Phase.Stop)]
        private void ShootStop(NodeStatus s)
        {
            if (_anim != null)
            {
                _anim.ResetTrigger(_attackTriggerHash);
                _anim.SetBool(_isAttackingHash, false);
            }
            _agent.updateRotation = true;
            _agent.isStopped = false;

            if (s != NodeStatus.Success || _aimedAt == null || !_aimedAt.IsAlive) return;

            // Line-of-sight raycast — miss if an obstacle is in the way.
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 to = _aimedAt.Transform.position + Vector3.up * 1.2f;
            Vector3 dirHit = (to - origin);
            float dist = dirHit.magnitude;
            if (dist < 0.01f) { _aimedAt.TakeDamage(_attackDamage, this); return; }
            dirHit /= dist;
            if (Physics.Raycast(origin, dirHit, out RaycastHit h, dist + 0.1f))
            {
                var d = h.collider.GetComponentInParent<IDamageable>();
                if (ReferenceEquals(d, _aimedAt))
                    _aimedAt.TakeDamage(_attackDamage, this);
            }
            else _aimedAt.TakeDamage(_attackDamage, this);
        }

        // --- Idle ----------------------------------------------------------
        [StateDef("Idle", Phase.Start)]
        private void IdleStart() { _idleElapsed = 0f; }
        [StateDef("Idle", Phase.Tick)]
        private NodeStatus IdleTick(float dt)
        {
            _idleElapsed += dt;
            return _idleElapsed >= 0.5f ? NodeStatus.Success : NodeStatus.Running;
        }

        // --- Tree ----------------------------------------------------------
        protected override INode<ArcherState> CreateTree()
        {
            return BTBuilder<ArcherState>.Build(root => root
                .Parallel(par => par
                    .Repeater(rep => rep
                        .Force(NodeStatus.Success, force => force
                            .Throttle(0.3f, thr => thr
                                .When((self, _) => !self.HasTarget, _ => _.Set(Get(State.Sense)))
                            )
                        )
                    )
                    .Repeater(rep => rep
                        .Selector(sel => sel
                            .When((self, _) => self.HasTarget && self.TargetDistance < self._retreatRange,
                                _ => _.Set(Get(State.Retreat)))
                            .When((self, _) => self.HasTarget && self.TargetDistance <= self._attackRange,
                                _ => _.Set(Get(State.Shoot)))
                            .When((self, _) => self.HasTarget,
                                _ => _.Set(Get(State.Approach)))
                            .Add(Get(State.Idle))
                        )
                    )
                )
            );
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _sensorRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, _retreatRange);
        }
    }
}
