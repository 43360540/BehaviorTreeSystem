using System;

namespace BehaviorTree
{
    public class ConditionLeaf<TContext> : LeafBase<TContext, ICondition<TContext>>
    {
        private readonly ICondition<TContext> _condition = null;
        
        public ConditionLeaf(Func<TContext, float, bool> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            _condition = new QuickCondition<TContext>(condition);
        }

        public ConditionLeaf(ICondition<TContext> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            return _condition.Evaluate(ctx, dt) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}
