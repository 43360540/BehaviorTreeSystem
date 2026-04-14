using System;

namespace BehaviorTree
{
    public sealed class RepeaterDecorator<TContext> : DecoratorBase<TContext>
    {
        private readonly int _times;
        private int _currentTimes = 0;

        public RepeaterDecorator(int times, INode<TContext> child, string? name = null) : base(child, name ?? "Repeater")
        {
            if (times <= 0)
                throw new ArgumentException("RepeaterDecoration.times cannot be below 1 time");

            _times = times;
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            var status = Child.Tick(ctx, dt);
            if (status == NodeStatus.Success)
            {
                if (_currentTimes >= _times)
                    return NodeStatus.Success;
                _currentTimes++;

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

        public static void Repeater<TContext>(this ISingleChild<TContext> b,
             int times, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<int, TContext>(times, name);
            buildAction(builder);
            b.Set(builder.Build((t, c, n) => new RepeaterDecorator<TContext>(t, c, n)));
        }
    }
}