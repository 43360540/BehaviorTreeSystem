using System;

namespace BehaviorTree
{
    public sealed class ForceDecorator<TContext> : DecoratorBase<TContext>
    {
        private readonly NodeStatus _forced;

        public ForceDecorator(NodeStatus forced, INode<TContext> child, string? name = null)
            : base(child, name ?? $"Force: {forced}")
        {
            if (forced != NodeStatus.Success && forced != NodeStatus.Failure)
                throw new ArgumentException("ForceDecorator only accepts Success or Failure.");

            _forced = forced;
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            var status = Child.Tick(ctx);
            if (status != NodeStatus.Running)
                return _forced;

            return NodeStatus.Running;
        }
    }

    public static class ForceExtension
    {
        public static TSelf Force<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            NodeStatus status, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<NodeStatus, TContext>(status, name);
            buildAction(builder);
            return b.Add(builder.Build((s, c, n) => new ForceDecorator<TContext>(s, c, n)));
        }

        public static void Force<TContext>(this ISingleChild<TContext> b,
            NodeStatus status, Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<NodeStatus, TContext>(status, name);
            buildAction(builder);
            b.Set(builder.Build((s, c, n) => new ForceDecorator<TContext>(s, c, n)));
        }
    }
}