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
        protected float? DeltaTime { get; private set; }

        private NodeStatus _lastStatus = NodeStatus.None;

        public NodeBase(string? name) =>
            Name = string.IsNullOrEmpty(name) ? CleanName(GetType().Name) : name;

        public void TimeElapse(float dt)
        {
            DeltaTime = dt;
            OnTimeElapse(dt);
        }
        public NodeStatus Tick(TContext ctx)
        {
            if (DeltaTime == null)
                throw new InvalidOperationException("TimeElapse() must be called before Tick().");

            if (_lastStatus == NodeStatus.None)
            {
                OnStart(ctx);
                _lastStatus = NodeStatus.Running;
            }

            NodeStatus status = OnTick(ctx, DeltaTime.Value);
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
            DisplayStatus = NodeStatus.None;
            OnAbort(ctx);
            Reset();
        }

        private void Reset()
        {
            _lastStatus = NodeStatus.None;
            OnReset();
        }

        protected virtual void OnTimeElapse(float dt) { }

        protected abstract NodeStatus OnTick(TContext ctx, float dt);

        protected virtual void OnStart(TContext ctx) { }

        protected virtual void OnStop(TContext ctx, NodeStatus stopStatus) { }

        protected virtual void OnAbort(TContext ctx) { }

        protected virtual void OnReset() { }
    }
}