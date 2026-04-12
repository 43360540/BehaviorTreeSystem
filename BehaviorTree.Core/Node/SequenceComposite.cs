using System;

namespace BehaviorTree
{
    // Memory Sequence
    public sealed class SequenceComposite<TContext> : CompositeBase<TContext>
    {
        private int _index = 0;

        public SequenceComposite(string? name = null, params INode<TContext>[] children) : 
            base(name ?? "Sequence", children) { }

        protected override void OnStart(TContext ctx)
        {
            base.OnStart(ctx);

            _index = 0;
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            while (_index < Children.Length)
            {
                NodeStatus status = Children[_index].Tick(ctx, dt);

                if (status != NodeStatus.Success)
                    return status;
                _index++;
            }
            return NodeStatus.Success;
        }

        protected override void OnAbort(TContext ctx)
        {
            base.OnAbort(ctx);

            if (_index >= 0 && _index < Children.Length)
                Children[_index].Abort(ctx);
        }

        protected override void OnReset()
        {
            base.OnReset();

            _index = 0;
        }
    }

    public static class SequenceExtension
    {
        public static TSelf Sequence<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Action<IMultiChildren<CompositeBuilder<TContext>, TContext>> buildAction, string? name = null)
        {
            var builder = new CompositeBuilder<TContext>(name);
            buildAction(builder);
            return b.Add(builder.Build((c, n) => new SequenceComposite<TContext>(n, c)));
        }

        public static void Sequence<TContext>(this ISingleChild<TContext> b,
            Action<IMultiChildren<CompositeBuilder<TContext>, TContext>> buildAction, string? name = null)
        {
            var builder = new CompositeBuilder<TContext>(name);
            buildAction(builder);
            b.Set(builder.Build((c, n) => new SequenceComposite<TContext>(n, c)));
        }
    }
}