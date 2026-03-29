using System.Collections.Generic;

namespace BehaviorTree
{
    public sealed class ParallelComposite<TContext> : CompositeBase<TContext>
    {
        public ParallelComposite(params INode<TContext>[] children) : base(children){}

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            bool hasFailure = false;
            bool hasRunning = false;

            foreach (INode<TContext> c in Children)
            {
                NodeStatus status = c.Tick(ctx, dt);

                if (status == NodeStatus.Failure)
                    hasFailure = true;
                if (status == NodeStatus.Running)
                    hasRunning = true;
            }
            
            if (hasFailure)
            {
                foreach (INode<TContext> c in Children)
                    c.Abort(ctx);
                return NodeStatus.Failure;
            }

            return hasRunning ? NodeStatus.Running : NodeStatus.Success;
        }

        protected override void OnAbort(TContext ctx)
        {
            base.OnAbort(ctx);
            foreach (INode<TContext> c in Children)
                c.Abort(ctx);
        }
    }
}