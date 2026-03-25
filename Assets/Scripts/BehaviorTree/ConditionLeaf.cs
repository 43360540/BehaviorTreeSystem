using System;

namespace BehaviorTree
{
    public class ConditionLeaf<TContext> : NodeBase<TContext>
    {
        private readonly ICondition<TContext> _condition = null;
        
        public ConditionLeaf(Func<TContext, float, bool> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            _condition = new DelegateCondition<TContext>(condition);
        }

        public ConditionLeaf(ICondition<TContext> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool Evaluate(TContext ctx, float dt)
        {
            return _condition.Evaluate(ctx, dt);
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            return Evaluate(ctx, dt)? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}
