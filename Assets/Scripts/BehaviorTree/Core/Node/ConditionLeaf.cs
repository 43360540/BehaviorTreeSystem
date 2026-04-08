using System;

namespace BehaviorTree
{
    public sealed class ConditionLeaf<TContext> : LeafBase<TContext>
    {
        private readonly ICondition<TContext> _condition = null;

        public ConditionLeaf(ICondition<TContext> condition, string name = null) : base(name ?? condition.GetType().Name)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            return _condition.Evaluate(ctx, dt) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}
