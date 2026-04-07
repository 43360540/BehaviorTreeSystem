using System.Collections.Generic;

namespace BehaviorTree
{
    public interface INode<TContext>
    {
        NodeStatus Tick(TContext ctx, float dt);
        void Abort(TContext ctx);
    }

    public interface IGuard<TContext>
    {
        bool CanEnter(TContext ctx, float dt);
    }

    public interface ICondition<TContext>
    {
        bool Evaluate(TContext ctx, float dt);
    }

    public interface IReadOnlyNode
    {
        string Name { get; }
        NodeStatus Status { get; }
        IReadOnlyList<IReadOnlyNode> SubNodes { get; }
    }

    public sealed class DebugInfo
    {
        public string Name { get; }
        public NodeStatus Status { get; }
        public DebugInfo[] SubInfo { get; }

        public DebugInfo(string name, NodeStatus status, params DebugInfo[] subInfo)
        {
            Name = name;
            Status = status;
            SubInfo = subInfo;
        }
    }

    public enum NodeStatus
    {
        None,
        Success,
        Running,
        Failure,
    }
}