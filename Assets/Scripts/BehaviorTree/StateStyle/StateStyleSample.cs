using BehaviorTree;

namespace BehaviorTree.StateStyle
{
    public class StateStyleSample : StateStyleBase<StateStyleSample, StateStyleSample.State>
    {
        public int Number { get; private set;}

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
            return NodeStatus.Success;   
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
            return NodeStatus.Success;   
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
        }

        protected override INode<StateStyleSample> CreateTree()
        {
            var tree = BT<StateStyleSample>.Build(root => root
                .Selector(main => main
                    .When(new QuickCondition<StateStyleSample>(() => Number > 0), _ => _
                        .Do(Action(State.Attack))
                    )
                    .Do(Action(State.Alert))
                    .Do(Action(State.Idle))
                )
            );

            return tree;
        }
    }
}