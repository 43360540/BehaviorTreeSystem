using System;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorTree
{
    public abstract class NodeBase<TContext> : INode<TContext>, IReadOnlyNode
    {
        private static string CleanName(string name)
        {
            int i = name.IndexOf('`');
            return i >= 0 ? name[..i] : name;
        }

        protected string Name { get; }
        protected string Info { get; set; } = "";
        protected INode<TContext>[]? ChildNodes { get; set; } = null;
        protected int LastTickedCycle { get; private set; } = 0;
        protected float? DeltaTime { get; private set; }

        private int _tickCycle = 0;
        private NodeStatus _lastStatus = NodeStatus.None;

        public NodeBase(string? name) =>
            Name = string.IsNullOrEmpty(name) ? CleanName(GetType().Name) : name;

        public void TimeElapse(float dt)
        {
            _tickCycle++;
            DeltaTime = dt;
            OnTimeElapse(dt);
        }

        public NodeStatus Tick(TContext ctx)
        {
            if (DeltaTime == null)
                throw new InvalidOperationException("TimeElapse() must be called before Tick().");

            LastTickedCycle = _tickCycle;

            if (_lastStatus == NodeStatus.None)
            {
                OnStart(ctx);
                _lastStatus = NodeStatus.Running;
            }

            NodeStatus status = OnTick(ctx, DeltaTime.Value);
            if (status == NodeStatus.None)
                throw new InvalidOperationException("OnTick must not return NodeStatus.None.");
            _lastStatus = status;

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

        protected void UpdateTickedCycle() => LastTickedCycle = _tickCycle;

        protected virtual void OnTimeElapse(float dt) { }

        protected abstract NodeStatus OnTick(TContext ctx, float dt);

        protected virtual void OnStart(TContext ctx) { }

        protected virtual void OnStop(TContext ctx, NodeStatus stopStatus) { }

        protected virtual void OnAbort(TContext ctx) { }

        protected virtual void OnReset() { }

        int IReadOnlyNode.SerialNumber => LastTickedCycle;
        string IReadOnlyNode.Name => Name;
        string IReadOnlyNode.DisplayInfo => Info;
        IReadOnlyList<IReadOnlyNode>? IReadOnlyNode.SubNodes => ChildNodes?.OfType<IReadOnlyNode>().ToArray();
    }
}
