using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public sealed class ParallelComposite<TContext> : CompositeBase<TContext>
    {
        private readonly NodeStatus[] _statuses;

        public ParallelComposite(string? name = null, params INode<TContext>[] children) : 
            base(name ?? "Parallel", children) =>
            _statuses = new NodeStatus[children.Length];

        protected override void OnStart(TContext ctx)
        {
            base.OnStart(ctx);
            ClearStatuses();
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            bool hasRunning = false;

            for (int i = 0; i < Children.Length; i++)
            {
                if (_statuses[i] == NodeStatus.Success)
                    continue;

                _statuses[i] = Children[i].Tick(ctx, dt);

                if (_statuses[i] == NodeStatus.Failure)
                {
                    AbortAllChildren(ctx);
                    return NodeStatus.Failure;
                }
                    
                if (_statuses[i] == NodeStatus.Running)
                    hasRunning = true;
            }

            return hasRunning ? NodeStatus.Running : NodeStatus.Success;
        }

        protected override void OnAbort(TContext ctx)
        {
            base.OnAbort(ctx);
            AbortAllChildren(ctx);
        }

        protected override void OnReset()
        {
            base.OnReset();
            ClearStatuses();
        }

        private void AbortAllChildren(TContext ctx)
        {
            foreach (INode<TContext> c in Children)
                c.Abort(ctx);
        }

        private void ClearStatuses()
        {
            for (int i = 0; i < _statuses.Length; i++)
                _statuses[i] = NodeStatus.None;
        }
    }

    public static class ParallelExtension
    {
        public static TSelf Parallel<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Action<IMultiChildren<CompositeBuilder<TContext>, TContext>> buildAction, string? name = null)
        {
            var builder = new CompositeBuilder<TContext>(name);
            buildAction(builder);
            return b.Add(builder.Build((c, n) => new ParallelComposite<TContext>(n, c)));
        }

        public static void Parallel<TContext>(this ISingleChild<TContext> b,
            Action<IMultiChildren<CompositeBuilder<TContext>, TContext>> buildAction, string? name = null)
        {
            var builder = new CompositeBuilder<TContext>(name);
            buildAction(builder);
            b.Set(builder.Build((c, n) => new ParallelComposite<TContext>(n, c)));
        }
    }
}