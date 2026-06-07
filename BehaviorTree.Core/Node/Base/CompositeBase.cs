using System;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorTree
{
    public abstract class CompositeBase<TContext> : NodeBase<TContext>
    {
        protected INode<TContext>[] Children => ChildNodes!;

        protected CompositeBase(string? name = null, params INode<TContext>[] children) : base(name)
        {
            if (children == null)
                throw new ArgumentNullException(nameof(children));

            if (Array.Exists(children, c => c == null))
                throw new ArgumentException("Children cannot contain null.", nameof(children));

            ChildNodes = [.. children];
        }

        protected override void OnTimeElapse(float dt)
        {
            foreach (var c in Children)
                c.TimeElapse(dt);
        }
    }
}
