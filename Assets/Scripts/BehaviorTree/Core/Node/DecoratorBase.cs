using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class DecoratorBase<TContext, TLogic> : NodeBase<TContext>, IReadOnlyNode
    {
        protected INode<TContext> Child { get; }

        private readonly IReadOnlyNode[] _readOnlyChildren;

        public DecoratorBase(INode<TContext> child)
        {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            _readOnlyChildren = child is IReadOnlyNode r ? new[] { r } : Array.Empty<IReadOnlyNode>();
        }

        protected override void OnAbort(TContext ctx)
        {
            base.OnAbort(ctx);
            Child.Abort(ctx);
        }

        // IReadOnlyNode
        public string Name => GetType().Name;

        public NodeStatus Status => LastStatus;

        public IReadOnlyList<IReadOnlyNode> SubNodes => _readOnlyChildren;  
    }
}