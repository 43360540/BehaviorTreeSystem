using BehaviorTree;
using BehaviorTree.StateStyle;
using UnityEngine;
using UnityEngine.AI;

public class NPCSample : StateStyleBase<NPCSample, NPCSample.State>
{
    [Header("Move")]
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] private float _walkSp = 3f;
    [Header("Sensor")]
    [SerializeField] private float _senserMaxRadius = 10f;
    [SerializeField] private LayerMask _targetLayer;
    [Header("Anim")]
    [SerializeField] private Animator _anim;

    private readonly int _attackTrigger = Animator.StringToHash("AttackTrigger");
    private readonly int _isAttacking = Animator.StringToHash("IsAttacking");
    private Transform _target = null;
    private Vector3 _destination;
    private readonly Collider[] _overlapBuffer = new Collider[10];

    public bool HasTarget => _target != null;
    public float TargetDistance =>
        _target == null ? 0f : (_target.position - transform.position).magnitude;
    public Vector3 TargetDirection
    {
        get
        {
            if (_target == null)
                return transform.eulerAngles;
            var want = (_target.position - transform.position).normalized;
            want.y = 0;
            return want;
        }
    }

    #region Sense
    [StateDef("Sense", Phase.Tick)]
    private NodeStatus SenseTick(float dt)
    {
        int hit = Physics.OverlapSphereNonAlloc(
            transform.position, _senserMaxRadius, _overlapBuffer, _targetLayer);
        if (hit <= 0)
            return NodeStatus.Failure;
        _target = _overlapBuffer[Random.Range(0, hit)].transform;
        return NodeStatus.Success;
    }
    #endregion

    #region Chase
    [StateDef("Chase", Phase.Start)]
    private void ChaseStart()
    {
        _agent.speed = _walkSp;
        _destination = _target.position;
    }

    [StateDef("Chase", Phase.Tick)]
    private NodeStatus ChaseTick(float dt)
    {
        return Walk(_target.position, 2.5f);
    }

    [StateDef("Chase", Phase.Stop)]
    private void ChaseStop(NodeStatus status)
    {
        _agent.ResetPath();
    }
    #endregion

    #region Attack
    [StateDef("Attack", Phase.Start)]
    private void AttackStart()
    {
        _destination = _target.position;
        _anim.SetTrigger(_attackTrigger);
        _anim.SetBool(_isAttacking, true);
        _agent.updateRotation = false;
    }

    [StateDef("Attack", Phase.Tick)]
    private NodeStatus AttackTick(float dt)
    {
        transform.rotation = Quaternion.LookRotation(TargetDirection);
        if (_anim.GetBool(_isAttacking))
            return NodeStatus.Running;
        return NodeStatus.Success;
    }

    [StateDef("Attack", Phase.Stop)]
    private void AttackStop(NodeStatus status)
    {
        _anim.ResetTrigger(_attackTrigger);
        _agent.updateRotation = true;
    }
    #endregion

    #region Wander
    [StateDef("Wander", Phase.Start)]
    private void WanderStart()
    {
        _target = null;
        _destination = transform.position + GetRandonPos(-10f, 10f);
        _agent.speed = _walkSp;
    }

    [StateDef("Wander", Phase.Tick)]
    private NodeStatus WanderTick(float dt)
    {
        return Walk(_destination);
    }

    [StateDef("Wander", Phase.Stop)]
    private void WanderStop(NodeStatus status)
    {
        _agent.ResetPath();
    }
    #endregion

    private Vector3 GetRandonPos(float a, float b) =>
        new Vector3(Random.Range(a, b), transform.position.y, Random.Range(a, b));

    private NodeStatus Walk(Vector3 destination, float range = 1e-6f)
    {
        if (Vector3.Magnitude(destination - _agent.destination) >= range)
            _agent.destination = destination;

        if (_agent.pathPending)
            return NodeStatus.Running;
        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid || _agent.pathStatus == NavMeshPathStatus.PathPartial)
            return NodeStatus.Failure;
        if (_agent.pathStatus == NavMeshPathStatus.PathComplete && _agent.remainingDistance <= _agent.stoppingDistance)
            return NodeStatus.Success;
        return NodeStatus.Running;
    }

    private bool IsTargetInRange(float distance) =>
        _target != null && TargetDistance <= distance;

    protected override INode<NPCSample> CreateTree()
    {
        var tree = BTBuilder<NPCSample>.Build(root => root
            .Parallel(x => x
                // Sensor
                .Repeater(x => x
                    .Force(NodeStatus.Success, x => x
                        .Throttle(0.3f, x => x
                            .When(() => !HasTarget, x => x.Set(Get(State.Sense)))
                        )
                    )
                )
                // Decitions
                .Repeater(x => x
                    .Selector(x => x
                        .When(() => IsTargetInRange(3f), x => x.Set(Get(State.Attack)))
                        .When(() => IsTargetInRange(10f), x => x.Set(Get(State.Chase)))
                        .Add(Get(State.Wander))
                    )
                )
            )
        );

        return tree;
    }

    public enum State
    {
        Wander,
        Chase,
        Attack,
        Sense,
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, _senserMaxRadius);
    }
}
