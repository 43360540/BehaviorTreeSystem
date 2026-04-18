using System;

namespace BehaviorTree
{
    public sealed class RepeaterDecorator<TContext> : DecoratorBase<TContext>
    {
        private readonly int? _times;
        private int _currentTimes = 0;

        public RepeaterDecorator(int times, INode<TContext> child, string? name = null) : base(child, name ?? $"Repeat: {times} time(s)")
        {
            if (times <= 0)
                throw new ArgumentException("RepeaterDecorator.times cannot be below 1 time");

            _times = times;
        }

        public RepeaterDecorator(INode<TContext> child, string? name = null) : base(child, name ?? "Repeater")
        {
            _times = null;
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            var status = Child.Tick(ctx);
            if (status == NodeStatus.Success)
            {
                _currentTimes++;
                if (_times == null)
                    return NodeStatus.Running;
                else if (_currentTimes >= _times)
                    return NodeStatus.Success;

                return NodeStatus.Running;
            }

            return status;
        }

        protected override void OnReset()
        {
            _currentTimes = 0;
        }
    }

    public static class RepeaterExtension
    {
        public static TSelf Repeater<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            int times, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<int, TContext>(times, name);
            buildAction(builder);
            return b.Add(builder.Build((t, c, n) => new RepeaterDecorator<TContext>(t, c, n)));
        }

        public static TSelf Repeater<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<TContext>(name);
            buildAction(builder);
            return b.Add(builder.Build((c, n) => new RepeaterDecorator<TContext>(c, n)));
        }

        public static void Repeater<TContext>(this ISingleChild<TContext> b,
            int times, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<int, TContext>(times, name);
            buildAction(builder);
            b.Set(builder.Build((t, c, n) => new RepeaterDecorator<TContext>(t, c, n)));
        }

        public static void Repeater<TContext>(this ISingleChild<TContext> b,
            Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<TContext>(name);
            buildAction(builder);
            b.Set(builder.Build((c, n) => new RepeaterDecorator<TContext>(c, n)));
        }
    }
}