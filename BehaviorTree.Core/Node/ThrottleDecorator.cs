using System;

namespace BehaviorTree
{
    public sealed class ThrottleDecorator<TContext> : DecoratorBase<TContext>, IGuard<TContext>
    {
        private readonly float _period;
        private float _elapsed;
        private bool? _cachedResult = null;

        protected override void OnTimeElapse(float dt)
        {
            base.OnTimeElapse(dt);

            if (_elapsed < _period)
                _elapsed += dt;
        }

        public ThrottleDecorator(float period, INode<TContext> child, string? name = null) : base(child, name ?? "Throttle")
        {
            if (period <= 0)
                throw new ArgumentException("Throttle.period cannot be below 0.");

            _period = period;
            _elapsed = period + 1e-4f;
        }

        public bool CanEnter(TContext ctx, float dt)
        {
            _cachedResult = _elapsed >= _period;
            DisplayStatus = _cachedResult.Value ? NodeStatus.Success : NodeStatus.Failure;
            return _cachedResult.Value;
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            bool canContinue = _cachedResult ?? CanEnter(ctx, dt);

            if (!canContinue)
            {
                Child.Abort(ctx);
                return NodeStatus.Failure;
            }

            return Child.Tick(ctx);
        }

        protected override void OnStop(TContext ctx, NodeStatus stopStatus)
        {
            if (_cachedResult == true)
                _elapsed = 0;
        }

        protected override void OnReset()
        {
            _cachedResult = null;
        }
    }

    public static class ThrottleExtension
    {
        public static TSelf Throttle<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            float coolDown, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<float, TContext>(coolDown, name);
            buildAction(builder);
            return b.Add(builder.Build((cd, c, n) => new ThrottleDecorator<TContext>(cd, c, n)));
        }

        public static void Throttle<TContext>(this ISingleChild<TContext> b,
            float coolDown, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<float, TContext>(coolDown, name);
            buildAction(builder);
            b.Set(builder.Build((cd, c, n) => new ThrottleDecorator<TContext>(cd, c, n)));
        }
    }
}