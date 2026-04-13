using System;

namespace BehaviorTree
{
    public sealed class GuardDecorator<TContext> : DecoratorBase<TContext>, IGuard<TContext>
    {
        private readonly ICondition<TContext> _condition;
        private bool? _cachedResult = null;

        public GuardDecorator(ICondition<TContext> condition,
                                INode<TContext> child, string? name = null) : base(child, name ?? condition.GetType().Name)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool CanEnter(TContext ctx, float dt)
        {
            _cachedResult = _condition.Evaluate(ctx, dt);
            DisplayStatus = _cachedResult.Value ? NodeStatus.Success : NodeStatus.Failure;
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

    public static class GuardExtension
    {
        public static TSelf When<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            ICondition<TContext> condition, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<ICondition<TContext>, TContext>(condition, name);
            buildAction(builder);
            return b.Add(builder.Build((ic, c, n) => new GuardDecorator<TContext>(
                ic ?? throw new InvalidOperationException("GuardDecorator must have ICondition when new()."), c, n)));
        }

        public static TSelf When<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<TContext, float, bool> predicate, Action<ISingleChild<TContext>> buildAction, string? name = "When")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            var builder = new DecoratorBuilder<ICondition<TContext>, TContext>(qCondition, name);
            buildAction(builder);
            return b.Add(builder.Build((ic, c, n) => new GuardDecorator<TContext>(
                ic ?? throw new InvalidOperationException("GuardDecorator must have ICondition when new()."), c, n)));
        }

        public static TSelf When<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<float, bool> predicate, Action<ISingleChild<TContext>> buildAction, string? name = "When")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            var builder = new DecoratorBuilder<ICondition<TContext>, TContext>(qCondition, name);
            buildAction(builder);
            return b.Add(builder.Build((ic, c, n) => new GuardDecorator<TContext>(
                ic ?? throw new InvalidOperationException("GuardDecorator must have ICondition when new()."), c, n)));
        }

        public static TSelf When<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<bool> predicate, Action<ISingleChild<TContext>> buildAction, string? name = "When")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            var builder = new DecoratorBuilder<ICondition<TContext>, TContext>(qCondition, name);
            buildAction(builder);
            return b.Add(builder.Build((ic, c, n) => new GuardDecorator<TContext>(
                ic ?? throw new InvalidOperationException("GuardDecorator must have ICondition when new()."), c, n)));
        }

        public static void When<TContext>(this ISingleChild<TContext> b,
            ICondition<TContext> condition, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<ICondition<TContext>, TContext>(condition, name);
            buildAction(builder);
            b.Set(builder.Build((ic, c, n) => new GuardDecorator<TContext>(
                ic ?? throw new InvalidOperationException("GuardDecorator must have ICondition when new()."), c, n)));
        }

        public static void When<TContext>(this ISingleChild<TContext> b,
            Func<TContext, float, bool> predicate, Action<ISingleChild<TContext>> buildAction, string? name = "When")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            var builder = new DecoratorBuilder<ICondition<TContext>, TContext>(qCondition, name);
            buildAction(builder);
            b.Set(builder.Build((ic, c, n) => new GuardDecorator<TContext>(
                ic ?? throw new InvalidOperationException("GuardDecorator must have ICondition when new()."), c, n)));
        }

        public static void When<TContext>(this ISingleChild<TContext> b,
            Func<float, bool> predicate, Action<ISingleChild<TContext>> buildAction, string? name = "When")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            var builder = new DecoratorBuilder<ICondition<TContext>, TContext>(qCondition, name);
            buildAction(builder);
            b.Set(builder.Build((ic, c, n) => new GuardDecorator<TContext>(
                ic ?? throw new InvalidOperationException("GuardDecorator must have ICondition when new()."), c, n)));
        }

        public static void When<TContext>(this ISingleChild<TContext> b,
            Func<bool> predicate, Action<ISingleChild<TContext>> buildAction, string? name = "When")
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            var builder = new DecoratorBuilder<ICondition<TContext>, TContext>(qCondition, name);
            buildAction(builder);
            b.Set(builder.Build((ic, c, n) => new GuardDecorator<TContext>(
                ic ?? throw new InvalidOperationException("GuardDecorator must have ICondition when new()."), c, n)));
        }
    }
}
