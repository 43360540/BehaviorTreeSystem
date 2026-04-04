using BehaviorTree;

namespace BehaviorTree.StateStyle
{
    public class StateStyleSample : StateStyleBase<StateStyleSample, StateStyleSample.State>
    {
        public int Number { get; private set;}

        # region Idle
        [StateDef(Lifecycle.Start)]
        private void IdleStart()
        {
            // ...
        }
        [StateDef(Lifecycle.Tick)]
        private NodeStatus IdleTick(float dt)
        {
            // ...
            return NodeStatus.Success;   
        }
        [StateDef(Lifecycle.Stop)]
        private void IdleStop(NodeStatus stopStatus)
        {
            // ...  
        }
        # endregion

        # region Attack
        [StateDef(Lifecycle.Start)]
        private void AttackStart()
        {
            // ...
        }
        [StateDef(Lifecycle.Tick)]
        private NodeStatus AttackTick(float dt)
        {
            // ...
            return NodeStatus.Success;   
        }
        [StateDef(Lifecycle.Stop)]
        private void AttackStop(NodeStatus stopStatus)
        {
            // ...  
        }
        # endregion

        # region Alert
        [StateDef(Lifecycle.Start)]
        private void AlertStart()
        {
            // ...
        }
        [StateDef(Lifecycle.Tick)]
        private NodeStatus AlertTick(float dt)
        {
            // ...
            return NodeStatus.Success;   
        }
        [StateDef(Lifecycle.Stop)]
        private void AlertStop(NodeStatus stopStatus)
        {
            // ...  
        }
        # endregion
        // ...

        public enum State
        {
            Idle,
            Attack,
            Alert,
            // ...
        }

        protected override StateStyleSample CreateContext() => this;

        protected override INode<StateStyleSample> CreateTree()
        {
            var tree = BT<StateStyleSample>.Build(root => root
                .Selector(main => main
                    .When(new QuickCondition<StateStyleSample>((ctx, dt) => Number > 0), _ => _
                        .Do(Action(State.Attack))
                    )
                    .Do(Action(State.Alert))
                    .Do(Action(State.Idle))
                // ...
                )
            );

            return tree;
        }
    }
}