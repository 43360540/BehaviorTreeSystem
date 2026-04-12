using System;

namespace BehaviorTree
{
    public sealed class QuickCondition<TContext> : ICondition<TContext>
    {
        private readonly Func<TContext, float, bool> _predicate;

        public QuickCondition(Func<TContext, float, bool> predicate)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        } 

        public QuickCondition(Func<float, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _predicate = (_, dt) => predicate(dt); 
        }

        public QuickCondition(Func<bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _predicate = (_, _) => predicate();
        }

        public bool Evaluate(TContext ctx, float dt)
        {
            return _predicate.Invoke(ctx, dt);
        }
    }
}