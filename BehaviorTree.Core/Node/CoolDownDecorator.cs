using System;

namespace BehaviorTree
{
    public sealed class CoolDownDecorator<TContext> : DecoratorBase<TContext>, IGuard<TContext>
    {
        private readonly float _cd;
        private float _elapsed;
        private bool? _cachedResult = null;

        protected override void OnTimeElapse(float dt)
        {
            base.OnTimeElapse(dt);

            if (_elapsed < _cd)
                _elapsed += dt;
        }

        public CoolDownDecorator(float coolDown, INode<TContext> child, string? name = null) : base(child, name ?? "CoolDown")
        {
            if (coolDown <= 0)
                throw new ArgumentException("CoolDownDecorator.coolDown cannot be below 0.");

            _cd = coolDown;
            _elapsed = coolDown + 1e-4f;
        }

        public bool CanEnter(TContext ctx, float dt)
        {
            _cachedResult = _elapsed >= _cd;
            DisplayStatus = _cachedResult.Value ? NodeStatus.Success : NodeStatus.Failure;
            return _cachedResult.Value;
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            bool canContinue = _cachedResult ?? _elapsed >= _cd;

            if (!canContinue)
            {
                Child.Abort(ctx);
                return NodeStatus.Failure;
            }
            return Child.Tick(ctx);
        }

        protected override void OnStop(TContext ctx, NodeStatus stopStatus)
        {
            if (stopStatus == NodeStatus.Success)
                _elapsed = 0;
        }

        protected override void OnReset()
        {
            _cachedResult = null;
        }
    }

    public static class CoolDownExtension
    {
        public static TSelf CoolDown<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            float coolDown, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<float, TContext>(coolDown, name);
            buildAction(builder);
            return b.Add(builder.Build((cd, c, n) => new CoolDownDecorator<TContext>(cd, c, n)));
        }

        public static void CoolDown<TContext>(this ISingleChild<TContext> b,
            float coolDown, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<float, TContext>(coolDown, name);
            buildAction(builder);
            b.Set(builder.Build((cd, c, n) => new CoolDownDecorator<TContext>(cd, c, n)));
        }
    }
}