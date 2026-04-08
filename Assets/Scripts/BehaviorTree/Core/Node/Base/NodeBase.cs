using System;

namespace BehaviorTree
{
    public abstract class NodeBase<TContext> : INode<TContext>
    {
        private static string CleanName(string name)
        {
            int i = name.IndexOf('`');
            return i >= 0 ? name[..i] : name;
        }

        protected string Name { get; private set; }

        protected NodeStatus LastStatus { get; set; } = NodeStatus.None;

        public NodeBase(string name) => 
            Name = string.IsNullOrEmpty(name) ? CleanName(GetType().Name) : name;

        public NodeStatus Tick(TContext ctx, float dt)
        {
            if (LastStatus == NodeStatus.None)
            {
                OnStart(ctx);
                LastStatus = NodeStatus.Running;
            }

            NodeStatus status = OnTick(ctx, dt);
            if (status == NodeStatus.None)
                throw new InvalidOperationException("OnTick must not return NodeStatus.None.");
            LastStatus = status;

            if (LastStatus != NodeStatus.Running)
            {
                OnStop(ctx, LastStatus);
                SelfReset();
            }

            return status;
        }

        public void Abort(TContext ctx)
        {
            if (LastStatus == NodeStatus.None)
                return;
            OnAbort(ctx);
            SelfReset();            
        }

        protected abstract NodeStatus OnTick(TContext ctx, float dt);

        protected virtual void OnStart(TContext ctx) { }

        protected virtual void OnStop(TContext ctx, NodeStatus stopStatus) { }

        protected virtual void OnAbort(TContext ctx) { }

        protected virtual void OnReset() { }

        private void SelfReset()
        {
            LastStatus = NodeStatus.None;
            OnReset();
        }
    }
}