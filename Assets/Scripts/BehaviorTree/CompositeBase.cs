using System;

namespace BehaviorTree
{
    public abstract class CompositeBase<TContext> : NodeBase<TContext>
    {
        protected INode<TContext>[] Children { get; }

        protected CompositeBase(params INode<TContext>[] children)
        {
            if (children == null)
                throw new ArgumentNullException(nameof(children));

            if (Array.Exists(children, c => c == null))
                throw new ArgumentException("Children cannot contain null.", nameof(children));

            Children = (INode<TContext>[])children.Clone();
        }

        public override void HardReset()
        {
            base.HardReset();

            foreach (var c in Children)
                c.HardReset();
        }
    }
}