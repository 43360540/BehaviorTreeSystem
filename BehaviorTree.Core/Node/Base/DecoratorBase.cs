using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class DecoratorBase<TContext> : NodeBase<TContext>, IReadOnlyNode
    {
        protected INode<TContext> Child { get; }

        private readonly IReadOnlyNode[] _readOnlyChildren;

        public DecoratorBase(INode<TContext> child, string? name = null) : base(name)
        {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            _readOnlyChildren = child is IReadOnlyNode r ? [r] : [];
        }

        protected override void OnAbort(TContext ctx)
        {
            base.OnAbort(ctx);
            Child.Abort(ctx);
        }

        // IReadOnlyNode
        string IReadOnlyNode.Name => Name;
        NodeStatus IReadOnlyNode.Status => DisplayStatus;
        IReadOnlyList<IReadOnlyNode> IReadOnlyNode.SubNodes => _readOnlyChildren;  
    }
}