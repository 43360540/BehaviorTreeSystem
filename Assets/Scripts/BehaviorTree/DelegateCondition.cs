using System;

namespace BehaviorTree
{
    public sealed class DelegateCondition<TContext> : ICondition<TContext>
    {
        private readonly Func<TContext, float, bool> _predicate = null;

        public DelegateCondition(Func<TContext, float, bool> predicate)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public bool Evaluate(TContext ctx, float dt)
        {
            return _predicate.Invoke(ctx, dt);
        }
    }
}