using System.Collections.Generic;

namespace BehaviorTree
{
    public interface IReadOnlyNode
    {
        string Name { get; }
        NodeStatus Status { get; }
        IReadOnlyList<IReadOnlyNode> SubNodes { get; }
    }
}