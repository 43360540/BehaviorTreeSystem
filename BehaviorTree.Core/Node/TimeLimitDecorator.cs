using System;

namespace BehaviorTree
{
    public sealed class TimeLimitDecorator<TContext> : DecoratorBase<TContext>
    {
        private readonly float _timeLimit;
        private float _timer = 0;

        public TimeLimitDecorator(float timeLimit, INode<TContext> child, string? name = null) : base(child, name ?? $"TimeLimit: {timeLimit}")
        {
            if (timeLimit <= 0)
                throw new ArgumentException("TimeLimitDecorator.timeLimit cannot be below 0.");

            _timeLimit = timeLimit;
        }


        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            _timer += dt;
            if (_timer >= _timeLimit)
            {
                Child.Abort(ctx);
                return NodeStatus.Failure;
            }

            return Child.Tick(ctx);
        }

        protected override void OnReset()
        {
            _timer = 0;
        }
    }

    public static class TimeLimitExtension
    {
        public static TSelf TimeLimit<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            float timeLimit, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<float, TContext>(timeLimit, name);
            buildAction(builder);
            return b.Add(builder.Build((t, c, n) => new TimeLimitDecorator<TContext>(t, c, n)));
        }

        public static void TimeLimit<TContext>(this ISingleChild<TContext> b,
            float timeLimit, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<float, TContext>(timeLimit, name);
            buildAction(builder);
            b.Set(builder.Build((t, c, n) => new TimeLimitDecorator<TContext>(t, c, n)));
        }
    }
}