using System;

namespace BehaviorTree
{
    public class ConditionDecorator : NodeBase, ICondition
    {
        private Func<bool> _condition = null;
        private INode _child = null;

        public ConditionDecorator(Func<bool> condition, INode child)
        {
            _child = child;
            _condition = condition;
        }

        public bool Evaluate(BlackBoardMono bb, float dt)
        {
            return _condition.Invoke();
        }

        protected override NodeStatus OnTick(BlackBoardMono bb, float dt)
        {
            return _child.Tick(bb, dt);
        }
    }
}
