using System;

namespace BehaviorTree
{
    public abstract class NodeBase<TContext> : INode<TContext>
    {
        public NodeStatus LastStatus { get; private set; } = NodeStatus.None;

        public NodeStatus Tick(TContext ctx, float dt)
        {
            if (LastStatus == NodeStatus.None)
            {
                LastStatus = NodeStatus.Running;
                OnStart(ctx);
            }

            var status = OnTick(ctx, dt);
            if (status == NodeStatus.None)
                throw new InvalidOperationException("Node cannot return NodeStatus.None from OnTick.");
            LastStatus = status;

            if (LastStatus != NodeStatus.Running)
            {
                OnStop(ctx, LastStatus);

                LastStatus = NodeStatus.None;
                OnReset();
            }

            return status;
        }

        public void Abort(TContext ctx)
        {
            if (LastStatus == NodeStatus.None)
                return;
            OnAbort(ctx);

            LastStatus = NodeStatus.None;
            OnReset();
        }

        public virtual void Reset()
        {
            LastStatus = NodeStatus.None;

            OnReset();
        }

        protected abstract NodeStatus OnTick(TContext ctx, float dt);

        protected virtual void OnStart(TContext ctx) { }

        protected virtual void OnStop(TContext ctx, NodeStatus stopStatus) { }

        protected virtual void OnAbort(TContext ctx) { }

        protected virtual void OnReset() { }
    }
}