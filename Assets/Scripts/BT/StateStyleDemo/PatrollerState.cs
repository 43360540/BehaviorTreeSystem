using BehaviorTree.ClassFirst;
using BehaviorTree.StateStyle;
using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.StateStyleDemo
{
    /// <summary>
    /// State-Style Patroller: walks a route, will chase + attack briefly if an
    /// enemy enters the sensor radius but gives up past _giveUpRange.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class PatrollerState
        : StateStyleBase<PatrollerState, PatrollerState.State>, IDamageable
    {
        public enum State { Sense, Chase, Attack, Patrol }

        [Header("Refs")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Animator _anim;
        [Header("Identity")]
        [SerializeField] private Faction _faction;
        [Header("Stats")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _giveUpRange = 12f;
        [SerializeField] private float _sensorRadius = 9f;
        [SerializeField] private float _maxHp = 100f;
        [SerializeField] private float _attackDamage = 15f;
        [SerializeField] private LayerMask _sensorLayer;
        [Header("Patrol")]
        [SerializeField] private Vector3[] _patrolPoints;
        [SerializeField] private float _patrolWaitAtPoint = 1.5f;

        private float _hp;
        private IDamageable _target;
        private int _patrolIndex;
        private bool _patrolWaiting;
        private float _patrolWaitElapsed;
        private bool _patrolInit;
        private float _attackCooldownElapsed;
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

        // --- Sense ---------------------------------------------------------
        [StateDef("Sense", Phase.Tick)]
        private NodeStatus SenseTick(float dt)
        {
            _target = StateHelpers.FindClosestEnemy(transform, _sensorLayer, _sensorRadius, _overlapBuffer, _faction);
            return _target != null ? NodeStatus.Success : NodeStatus.Failure;
        }

        // --- Chase ---------------------------------------------------------
        [StateDef("Chase", Phase.Start)]
        private void ChaseStart() { _agent.speed = _walkSpeed; _agent.isStopped = false; }
        [StateDef("Chase", Phase.Tick)]
        private NodeStatus ChaseTick(float dt)
        {
            if (!HasTarget) return NodeStatus.Failure;
            float stop = Mathf.Max(0.5f, _attackRange - 0.3f);
            NodeStatus s = StateHelpers.WalkTo(_agent, _target.Transform.position);
            return TargetDistance <= stop ? NodeStatus.Success : s;
        }
        [StateDef("Chase", Phase.Stop)]
        private void ChaseStop(NodeStatus s)
        {
            if (_agent.isOnNavMesh) _agent.ResetPath();
        }

        // --- Attack --------------------------------------------------------
        [StateDef("Attack", Phase.Start)]
        private void AttackStart()
        {
            if (_anim == null) return;
            _agent.updateRotation = false;
            _agent.isStopped = true;
            _anim.SetTrigger(_attackTriggerHash);
            _anim.SetBool(_isAttackingHash, true);
            _attackCooldownElapsed = 0f;
        }
        [StateDef("Attack", Phase.Tick)]
        private NodeStatus AttackTick(float dt)
        {
            if (_anim == null) return NodeStatus.Failure;
            if (HasTarget)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(TargetDirFlat), 720f * dt);
            if (_anim.GetBool(_isAttackingHash))
                return NodeStatus.Running;
            _attackCooldownElapsed += dt;
            return _attackCooldownElapsed >= 0.25f ? NodeStatus.Success : NodeStatus.Running;
        }
        [StateDef("Attack", Phase.Stop)]
        private void AttackStop(NodeStatus s)
        {
            if (_anim != null)
            {
                _anim.ResetTrigger(_attackTriggerHash);
                _anim.SetBool(_isAttackingHash, false);
            }
            _agent.updateRotation = true;
            _agent.isStopped = false;
            if (s == NodeStatus.Success && HasTarget && TargetDistance <= _attackRange)
                _target.TakeDamage(_attackDamage, this);
        }

        // --- Patrol --------------------------------------------------------
        [StateDef("Patrol", Phase.Start)]
        private void PatrolStart()
        {
            _patrolWaiting = false;
            _patrolWaitElapsed = 0f;
            if (_patrolPoints == null || _patrolPoints.Length == 0) { _patrolInit = false; return; }

            int closest = 0; float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                float sqr = (_patrolPoints[i] - transform.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; closest = i; }
            }
            _patrolIndex = closest;
            _patrolInit = true;
            _agent.speed = _walkSpeed;
            _agent.isStopped = false;
            _agent.SetDestination(_patrolPoints[_patrolIndex]);
        }
        [StateDef("Patrol", Phase.Tick)]
        private NodeStatus PatrolTick(float dt)
        {
            if (!_patrolInit || _patrolPoints.Length == 0) return NodeStatus.Failure;

            if (_patrolWaiting)
            {
                _patrolWaitElapsed += dt;
                if (_patrolWaitElapsed >= _patrolWaitAtPoint)
                {
                    _patrolWaiting = false;
                    _patrolWaitElapsed = 0f;
                    _patrolIndex = (_patrolIndex + 1) % _patrolPoints.Length;
                    _agent.SetDestination(_patrolPoints[_patrolIndex]);
                }
                return NodeStatus.Running;
            }

            if (_agent.pathPending) return NodeStatus.Running;
            if (!_agent.hasPath || _agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
            {
                _patrolWaiting = true;
                return NodeStatus.Running;
            }
            return NodeStatus.Running;
        }
        [StateDef("Patrol", Phase.Stop)]
        private void PatrolStop(NodeStatus s)
        {
            if (_agent.isOnNavMesh) _agent.ResetPath();
        }

        // --- Tree ----------------------------------------------------------
        protected override INode<PatrollerState> CreateTree()
        {
            return BTBuilder<PatrollerState>.Build(root => root
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
                            .When((self, _) => self.HasTarget && self.TargetDistance <= self._attackRange,
                                _ => _.Set(Get(State.Attack)))
                            .When((self, _) => self.HasTarget && self.TargetDistance <= self._giveUpRange,
                                _ => _.Set(Get(State.Chase)))
                            .Add(Get(State.Patrol))
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
