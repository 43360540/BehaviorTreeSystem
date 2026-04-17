using System.Collections.Generic;
using System.Linq;
using BehaviorTree;
using BehaviorTree.StateStyle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class NPCSample : StateStyleBase<NPCSample, NPCSample.State>
{
    public enum State
    {
        Wander,
        Chase,
        Sense,
    }

    [SerializeField] NavMeshAgent _agent;
    [SerializeField] private Vector3 _destination;
    private Transform _target;
    [SerializeField] private float _senserMaxRadius = 10f;
    [SerializeField] private LayerMask _targetLayer;

    public float TargetDistance =>
        _target == null ? 0f : (_target.position - transform.position).magnitude;

    private List<Collider> _targets = new();

    #region Sense
    [StateDef("Sense", Phase.Start)]
    private void SenseStart()
    {
        _targets.Clear();
    }

    [StateDef("Sense", Phase.Tick)]
    private NodeStatus SenseTick(float dt)
    {
        List<Collider> condidate = _targets
            .Where(t =>
                Vector3.Magnitude(t.transform.position - transform.position) <= _senserMaxRadius)
            .ToList();

        if (condidate.Count == 0)
            condidate = Physics.OverlapSphere(
                transform.position, _senserMaxRadius, _targetLayer).ToList();
        if (condidate.Count == 0)
            return NodeStatus.Failure;
        _target = condidate[Random.Range(0, condidate.Count - 1)].transform;
        return NodeStatus.Success;
    }

    [StateDef("Sense", Phase.Stop)]
    private void SenseStop(NodeStatus status)
    {
        _targets.Clear();
    }
    #endregion

    #region Chase
    [StateDef("Chase", Phase.Start)]
    private void ChaseStart()
    {
        _agent.speed = GetRamdonFloat(1f, 10f);
        _destination = _target.position;
    }

    [StateDef("Chase", Phase.Tick)]
    private NodeStatus ChaseTick(float dt)
    {
        return Walk();
    }

    [StateDef("Chase", Phase.Stop)]
    private void ChaseStop(NodeStatus status)
    {
        _agent.ResetPath();
    }
    #endregion

    #region Wander
    [StateDef("Wander", Phase.Start)]
    private void WanderStart()
    {
        _destination = transform.position + GetRandonPos(-10f, 10f);
        _agent.speed = GetRamdonFloat(1f, 10f);
    }

    [StateDef("Wander", Phase.Tick)]
    private NodeStatus WanderTick(float dt)
    {
        return Walk();
    }

    [StateDef("Wander", Phase.Stop)]
    private void WanderStop(NodeStatus status)
    {
        _agent.ResetPath();
    }
    #endregion

    private Vector3 GetRandonPos(float a, float b) =>
        new Vector3(GetRamdonFloat(a, b), transform.position.y, GetRamdonFloat(a, b));

    private float GetRamdonFloat(float a, float b) =>
        Random.Range(a, b);

    private NodeStatus Walk()
    {
        if (Vector3.Magnitude(_destination - _agent.destination) >= 1e-6)
            _agent.destination = _destination;

        if (_agent.pathPending)
            return NodeStatus.Running;
        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid || _agent.pathStatus == NavMeshPathStatus.PathPartial)
            return NodeStatus.Failure;
        if (_agent.pathStatus == NavMeshPathStatus.PathComplete && _agent.remainingDistance <= _agent.stoppingDistance)
            return NodeStatus.Success;
        return NodeStatus.Running;
    }

    protected override INode<NPCSample> CreateTree()
    {
        var tree = BTBuilder<NPCSample>.Build(root => root
            .Parallel(_ => _
                .Repeater(_ => _
                    .Force(NodeStatus.Success, _ => _
                        .Set(GetState(State.Sense))
                    )
                )
                .Repeater(_ => _
                    .Selector(_ => _
                        .When(() => _target != null && TargetDistance <= 10f, _ => _
                            .Set(GetState(State.Chase))
                        )
                        .Add(GetState(State.Wander))
                    )
                )
            )
        );

        return tree;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, _senserMaxRadius);
    }
}
