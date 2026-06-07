using System;

namespace BehaviorTree
{
    public sealed class InverterDecorator<TContext> : DecoratorBase<TContext>
    {
        public InverterDecorator(INode<TContext> child, string? name = null) : base(child, name ?? "Inverter") { }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            var childStatus = Child.Tick(ctx);
            if (childStatus == NodeStatus.Running)
                return NodeStatus.Running;
            Info = childStatus == NodeStatus.Success ?
                $"{NodeStatus.Failure} ({NodeStatus.Success})" : $"{NodeStatus.Success} ({NodeStatus.Failure})";
            return childStatus == NodeStatus.Success ? NodeStatus.Failure : NodeStatus.Success;
        }
    }

    public static class InverterExtension
    {
        public static TSelf Inverter<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<TContext>(name);
            buildAction(builder);
            return b.Add(builder.Build((c, n) => new InverterDecorator<TContext>(c, n)));
        }

        public static void Inverter<TContext>(this ISingleChild<TContext> b,
            Action<ISingleChild<TContext>> buildAction, string? name = null)
        {
            var builder = new DecoratorBuilder<TContext>(name);
            buildAction(builder);
            b.Set(builder.Build((c, n) => new InverterDecorator<TContext>(c, n)));
        }
    }
}
