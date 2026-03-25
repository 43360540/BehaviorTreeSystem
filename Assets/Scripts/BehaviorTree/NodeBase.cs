using System;

namespace BehaviorTree
{
    public abstract class NodeBase<TContext> : INode<TContext>
    {
        public NodeStatus LastStatus { get; private set; } = NodeStatus.None;

        public NodeStatus Tick(TContext bb, float dt)
        {
            if (LastStatus == NodeStatus.None)
            {
                LastStatus = NodeStatus.Running;
                OnStart(bb);
            }

            var status = OnTick(bb, dt);
            if (status == NodeStatus.None)
                throw new InvalidOperationException("Node cannot return NodeStatus.None from OnTick.");
            LastStatus = status;

            if (LastStatus != NodeStatus.Running)
            {
                OnStop(bb, LastStatus);

                LastStatus = NodeStatus.None;
                OnReset();
            }

            return status;
        }

        public void Abort(TContext bb)
        {
            if (LastStatus == NodeStatus.None)
                return;
            OnAbort(bb);

            LastStatus = NodeStatus.None;
            OnReset();
        }

        public virtual void Reset()
        {
            LastStatus = NodeStatus.None;

            OnReset();
        }

        protected abstract NodeStatus OnTick(TContext bb, float dt);

        protected virtual void OnStart(TContext bb) { }

        protected virtual void OnStop(TContext bb, NodeStatus stopStatus) { }

        protected virtual void OnAbort(TContext bb) { }

        protected virtual void OnReset() { }
    }
}