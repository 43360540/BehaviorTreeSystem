using System;

namespace BehaviorTree
{
    public class GuardDecorator<TContext> : DecoratorBase<TContext>, IGuard<TContext>
    {
        private readonly ICondition<TContext> _condition;

        public GuardDecorator(ICondition<TContext> condition, INode<TContext> child) : base(child)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool CanEnter(TContext ctx, float dt)
        {
            return _condition.Evaluate(ctx, dt);
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            if (!_condition.Evaluate(ctx, dt))
            {
                Child.Abort(ctx);
                return NodeStatus.Failure;
            }
            return Child.Tick(ctx, dt);
        }
    }
}
