using BehaviorTree;
using UnityEngine;

namespace BehaviorTree.StateStyle
{
    public class StateStyleSample : StateStyleBase<StateStyleSample, StateStyleSample.State>
    {
        public int Number { get; private set; } = 0;
        private Transform _target;

        #region See
        [StateDef("See", Phase.Tick)]
        private NodeStatus SeeTick(float dt)
        {
            return NodeStatus.Running;
        }
        #endregion

        #region Hear
        [StateDef("Hear", Phase.Tick)]
        private NodeStatus Hear(float dt)
        {
            return NodeStatus.Running;
        }
        #endregion

        #region Idle
        [StateDef("Idle", Phase.Start)]
        private void IdleStart()
        {
            // ...
        }
        [StateDef("Idle", Phase.Tick)]
        private NodeStatus IdleTick(float dt)
        {
            // ...
            return NodeStatus.Success;
        }
        [StateDef("Idle", Phase.Stop)]
        private void IdleStop(NodeStatus stopStatus)
        {
            // ...
        }
        #endregion

        #region Attack
        [StateDef("Attack", Phase.Start)]
        private void AttackStart()
        {
            // ...
        }
        [StateDef("Attack", Phase.Tick)]
        private NodeStatus AttackTick(float dt)
        {
            // ...
            return NodeStatus.Running;
        }
        [StateDef("Attack", Phase.Stop)]
        private void AttackStop(NodeStatus stopStatus)
        {
            // ...  
        }
        #endregion

        #region Alert
        [StateDef("Alert", Phase.Start)]
        private void AlertStart()
        {
            // ...
        }
        [StateDef("Alert", Phase.Tick)]
        private NodeStatus AlertTick(float dt)
        {
            // ...
            return NodeStatus.Running;
        }
        [StateDef("Alert", Phase.Stop)]
        private void AlertStop(NodeStatus stopStatus)
        {
            // ...  
        }
        #endregion

        public enum State
        {
            Idle,
            Attack,
            Alert,
            Hear,
            See,
        }

        protected override INode<StateStyleSample> CreateTree()
        {
            var tree = BTBuilder<StateStyleSample>.Build(root => root
                .Parallel(root => root
                    .Parallel(sense => sense
                        .Add(GetState(State.See))
                        .Add(GetState(State.Hear))
                    )
                    .Selector(action => action
                        .When(() => GetDistance(_target) <= 1f, _ => _
                            .Set(GetState(State.Attack))
                        )
                        .When(() => GetDistance(_target) <= 10f, _ => _
                            .Set(GetState(State.Alert))
                        )
                        .Add(GetState(State.Idle))
                    )
                )
            );

            return tree;
        }

        private float GetDistance(Transform target)
        {
            return Vector3.Magnitude(transform.position - target.position);
        }
    }
}