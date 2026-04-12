using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class LeafBase<TContext> : NodeBase<TContext>, IReadOnlyNode
    {
        public LeafBase(string? name = null) : base(name) {}

        string IReadOnlyNode.Name => Name;
        NodeStatus IReadOnlyNode.Status => LastStatus;
        IReadOnlyList<IReadOnlyNode> IReadOnlyNode.SubNodes => Array.Empty<IReadOnlyNode>();
    }
}