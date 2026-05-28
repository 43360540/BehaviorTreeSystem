using System;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorTree
{
    public abstract class CompositeBase<TContext> : NodeBase<TContext>, IReadOnlyNode
    {
        protected INode<TContext>[] Children { get; }

        private readonly IReadOnlyNode[] _readOnlyChildren;

        protected CompositeBase(string? name = null, params INode<TContext>[] children) : base(name)
        {
            if (children == null)
                throw new ArgumentNullException(nameof(children));

            if (Array.Exists(children, c => c == null))
                throw new ArgumentException("Children cannot contain null.", nameof(children));

            Children = (INode<TContext>[])children.Clone();
            _readOnlyChildren = [.. children.OfType<IReadOnlyNode>()];
        }

        protected override void OnTimeElapse(float dt)
        {
            foreach (var c in Children)
                c.TimeElapse(dt);
        }

        // IReadOnlyNode
        string IReadOnlyNode.Name => Name;
        NodeStatus IReadOnlyNode.Status => DisplayStatus;
        IReadOnlyList<IReadOnlyNode> IReadOnlyNode.SubNodes => _readOnlyChildren;
    }
}
