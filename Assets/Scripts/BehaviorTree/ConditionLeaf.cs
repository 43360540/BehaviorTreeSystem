using System;

namespace BehaviorTree
{
    public class ConditionLeaf : NodeBase, ICondition
    {
        private readonly Func<bool> _condition = null;

        public ConditionLeaf(Func<bool> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool Evaluate()
        {
            return _condition.Invoke();
        }

        protected override NodeStatus OnTick(BlackBoardMono bb, float dt)
        {
            return Evaluate()? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}
