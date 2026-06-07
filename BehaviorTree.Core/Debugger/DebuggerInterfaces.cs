using System.Collections.Generic;

namespace BehaviorTree
{
    public interface IReadOnlyNode
    {
        int SerialNumber { get; }
        string Name { get; }
        string DisplayInfo { get; }
        IReadOnlyList<IReadOnlyNode>? SubNodes { get; }
    }
}
