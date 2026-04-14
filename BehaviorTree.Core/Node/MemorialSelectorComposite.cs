using System;
using System.Linq;

namespace BehaviorTree
{
    // Memorial Selector
    public sealed class MemorialSelectorComposite<TContext> : CompositeBase<TContext>
    {
        private int _activeIndex = 0;

        public MemorialSelectorComposite(string? name = null, params INode<TContext>[] children) : 
            base(name ?? "MemorialSelector", children) { }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            var prevStatus = Children[_activeIndex].Tick(ctx, dt);
            if (prevStatus != NodeStatus.Failure)
                return prevStatus;

            for (int i = _activeIndex + 1; i < Children.Count(); i++)
            {
                var status = Children[i].Tick(ctx, dt);
                if (status != NodeStatus.Failure)
                    return status;
            }
            return NodeStatus.Failure;
        }

        protected override void OnAbort(TContext ctx)
        {
            Children[_activeIndex].Abort(ctx);
        }

        protected override void OnReset()
        {
            _activeIndex = 0;
        }
    }

    public static class MemorialSelectorCompositeExtension
    {
        public static TSelf MemorialSelector<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Action<IMultiChildren<CompositeBuilder<TContext>, TContext>> buildAction, string? name = null)
        {
            var builder = new CompositeBuilder<TContext>(name);
            buildAction(builder);
            return b.Add(builder.Build((c, n) => new SelectorComposite<TContext>(n, c)));
        }

        public static void MemorialSelector<TContext>(this ISingleChild<TContext> b,
            Action<IMultiChildren<CompositeBuilder<TContext>, TContext>> buildAction, string? name = null)
        {
            var builder = new CompositeBuilder<TContext>(name);
            buildAction(builder);
            b.Set(builder.Build((c, n) => new SelectorComposite<TContext>(n, c)));
        }
    }
}