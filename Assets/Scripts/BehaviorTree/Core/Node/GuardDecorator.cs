using System;

namespace BehaviorTree
{
    public sealed class GuardDecorator<TContext> : DecoratorBase<TContext>, IGuard<TContext>
    {
        private readonly ICondition<TContext> _condition;
        private bool? _cachedResult = null;

        public GuardDecorator(ICondition<TContext> condition, 
                                INode<TContext> child, string name = null) : base(child, name ?? condition.GetType().Name)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool CanEnter(TContext ctx, float dt)
        {
            _cachedResult = _condition.Evaluate(ctx, dt);
            return _cachedResult.Value;
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            bool canContinue = _cachedResult ?? _condition.Evaluate(ctx, dt);

            if (!canContinue)
            {
                Child.Abort(ctx);
                return NodeStatus.Failure;
            }
            return Child.Tick(ctx, dt);
        }

        protected override void OnReset()
        {
            _cachedResult = null;
        }
    }
}
