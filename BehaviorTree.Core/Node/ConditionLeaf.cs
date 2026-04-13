using System;

namespace BehaviorTree
{
    public sealed class ConditionLeaf<TContext> : LeafBase<TContext>
    {
        private readonly ICondition<TContext> _condition;
        private readonly bool _expected;

        public ConditionLeaf(ICondition<TContext> condition, bool expected = true, string? name = null) : base(name ?? condition.GetType().Name)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _expected = expected;
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            return _condition.Evaluate(ctx, dt) == _expected ? NodeStatus.Success : NodeStatus.Failure;
        }
    }

    public static class ConditionExtension
    {
        public static TSelf Check<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            ICondition<TContext> condition, bool expected = true, string? name = null) =>
            b.Add(new ConditionLeaf<TContext>(condition, expected, name));

        public static TSelf Check<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<TContext, float, bool> predicate, bool expected = true, string? name = "Check")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            return b.Add(new ConditionLeaf<TContext>(qCondition, expected, name));
        }

        public static TSelf Check<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<float, bool> predicate, bool expected = true, string? name = "Check")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            return b.Add(new ConditionLeaf<TContext>(qCondition, expected, name));
        }

        public static TSelf Check<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<bool> predicate, bool expected = true, string? name = "Check")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            return b.Add(new ConditionLeaf<TContext>(qCondition, expected, name));
        }

        public static void Check<TContext>(this ISingleChild<TContext> b,
            ICondition<TContext> condition, bool expected = true, string? name = null) =>
            b.Set(new ConditionLeaf<TContext>(condition, expected, name));

        public static void Check<TContext>(this ISingleChild<TContext> b,
            Func<TContext, float, bool> predicate, bool expected = true, string? name = "Check")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            b.Set(new ConditionLeaf<TContext>(qCondition, expected, name));
        }

        public static void Check<TContext>(this ISingleChild<TContext> b,
            Func<float, bool> predicate, bool expected = true, string? name = "Check")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            b.Set(new ConditionLeaf<TContext>(qCondition, expected, name));
        }

        public static void Check<TContext>(this ISingleChild<TContext> b,
            Func<bool> predicate, bool expected = true, string? name = "Check")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            b.Set(new ConditionLeaf<TContext>(qCondition, expected, name));
        }
    }
}
