using System;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorTree
{
    public abstract class CompositeBase<TContext> : NodeBase<TContext>, IReadOnlyNode
    {
        protected INode<TContext>[] Children { get; }

        private readonly IReadOnlyNode[] _readOnlyChildren;

        protected CompositeBase(params INode<TContext>[] children)
        {
            if (children == null)
                throw new ArgumentNullException(nameof(children));

            if (Array.Exists(children, c => c == null))
                throw new ArgumentException("Children cannot contain null.", nameof(children));

            Children = (INode<TContext>[])children.Clone();
            _readOnlyChildren = children
                .OfType<IReadOnlyNode>()
                .ToArray();
        }

        // IReadOnlyNode
        public string Name => GetType().Name;

        public NodeStatus Status => LastStatus;

        public IReadOnlyList<IReadOnlyNode> SubNodes => _readOnlyChildren;
    }
}