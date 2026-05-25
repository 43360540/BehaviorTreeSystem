using BehaviorTree.ClassFirst; // shared Faction / IDamageable
using BehaviorTree.StateStyle;  // StateStyleBase / StateDef / Phase
using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.StateStyleDemo
{
    /// <summary>
    /// State-Style equivalent of WarriorRunner (Class-First). Aggressive melee:
    /// chase the nearest enemy, attack when in range, idle when nothing to do.
    /// All state methods live on this class; the BT just wires them by name.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class WarriorState
        : StateStyleBase<WarriorState, WarriorState.State>, IDamageable
    {
        public enum State { Sense, Chase, Attack, Idle }

        [Header("Refs")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Animator _anim;

        [Header("Identity")]
        [SerializeField] private Faction _faction;

        [Header("Stats")]
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _sensorRadius = 12f;
        [SerializeField] private float _maxHp = 120f;
        [SerializeField] private float _attackDamage = 22f;
        [SerializeField] private LayerMask _sensorLayer;

        // Runtime
        private float _hp;
        private IDamageable _target;
        private float _idleElapsed;
        private float _attackCooldownElapsed;
        private readonly Collider[] _overlapBuffer = new Collider[16];
        private int _attackTriggerHash;
        private int _isAttackingHash;

        // --- IDamageable ----------------------------------------------------
        public bool IsAlive => _hp > 0f;
        public Faction Faction => _faction;
        public Transform Transform => transform;
        public void TakeDamage(float damage, IDamageable source)
        {
            if (!IsAlive) return;
            _hp = Mathf.Max(0f, _hp - damage);
            if (_hp <= 0f) Die();
        }
        private void Die() { gameObject.SetActive(false); }

        // --- Lifecycle ------------------------------------------------------
        protected override void Awake()
        {
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (_anim == null) _anim = GetComponentInChildren<Animator>(true);
            _hp = _maxHp;
            _attackTriggerHash = Animator.StringToHash("AttackTrigger");
            _isAttackingHash = Animator.StringToHash("IsAttacking");
            base.Awake();
        }

        // Derived helpers used by state methods.
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

        // --- State: Sense ---------------------------------------------------
        [StateDef("Sense", Phase.Tick)]
        private NodeStatus SenseTick(float dt)
        {
            _target = StateHelpers.FindClosestEnemy(transform, _sensorLayer, _sensorRadius, _overlapBuffer, _faction);
            return _target != null ? NodeStatus.Success : NodeStatus.Failure;
        }

        // --- State: Chase ---------------------------------------------------
        [StateDef("Chase", Phase.Start)]
        private void ChaseStart()
        {
            _agent.speed = _walkSpeed;
            _agent.isStopped = false;
        }
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

        // --- State: Attack --------------------------------------------------
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
            // Rotate to face during the swing.
            if (HasTarget)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(TargetDirFlat), 720f * dt);

            if (_anim.GetBool(_isAttackingHash))
                return NodeStatus.Running;

            // Swing finished — small cooldown post-anim before reporting Success.
            _attackCooldownElapsed += dt;
            return _attackCooldownElapsed >= 0.2f ? NodeStatus.Success : NodeStatus.Running;
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

        // --- State: Idle ----------------------------------------------------
        [StateDef("Idle", Phase.Start)]
        private void IdleStart() { _idleElapsed = 0f; }
        [StateDef("Idle", Phase.Tick)]
        private NodeStatus IdleTick(float dt)
        {
            _idleElapsed += dt;
            return _idleElapsed >= 0.5f ? NodeStatus.Success : NodeStatus.Running;
        }

        // --- Tree -----------------------------------------------------------
        protected override INode<WarriorState> CreateTree()
        {
            return BTBuilder<WarriorState>.Build(root => root
                .Parallel(par => par
                    // Sensor branch: refresh target while we don't have one.
                    .Repeater(rep => rep
                        .Force(NodeStatus.Success, force => force
                            .Throttle(0.3f, thr => thr
                                .When((self, _) => !self.HasTarget, _ => _.Set(Get(State.Sense)))
                            )
                        )
                    )
                    // Decision branch
                    .Repeater(rep => rep
                        .Selector(sel => sel
                            .When((self, _) => self.HasTarget && self.TargetDistance <= self._attackRange,
                                _ => _.Set(Get(State.Attack)))
                            .When((self, _) => self.HasTarget,
                                _ => _.Set(Get(State.Chase)))
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
        }
    }
}
