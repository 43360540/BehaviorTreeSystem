using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class LeafBase<TContext, TLogic> : NodeBase<TContext>, IReadOnlyNode
    {
        public string Name => GetType().Name;

        public NodeStatus Status => LastStatus;

        public IReadOnlyList<IReadOnlyNode> SubNodes => null;
    }
}