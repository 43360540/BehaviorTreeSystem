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

        protected string Name { get; }
        protected NodeStatus DisplayStatus { get; set; } = NodeStatus.None;
        
        private NodeStatus _lastStatus = NodeStatus.None;

        public NodeBase(string? name) => 
            Name = string.IsNullOrEmpty(name) ? CleanName(GetType().Name) : name;

        public NodeStatus Tick(TContext ctx, float dt)
        {
            if (_lastStatus == NodeStatus.None)
            {
                OnStart(ctx);
                _lastStatus = NodeStatus.Running;
            }

            NodeStatus status = OnTick(ctx, dt);
            if (status == NodeStatus.None)
                throw new InvalidOperationException("OnTick must not return NodeStatus.None.");
            _lastStatus = status;
            DisplayStatus = status;

            if (_lastStatus != NodeStatus.Running)
            {
                OnStop(ctx, _lastStatus);
                Reset();
            }

            return status;
        }

        public void Abort(TContext ctx)
        {
            if (_lastStatus == NodeStatus.None)
                return;
            OnAbort(ctx);
            Reset();            
        }

        private void Reset()
        {
            _lastStatus = NodeStatus.None;
            OnReset();
        }

        protected abstract NodeStatus OnTick(TContext ctx, float dt);

        protected virtual void OnStart(TContext ctx) { }

        protected virtual void OnStop(TContext ctx, NodeStatus stopStatus) { }

        protected virtual void OnAbort(TContext ctx) { }

        protected virtual void OnReset() { }
    }
}