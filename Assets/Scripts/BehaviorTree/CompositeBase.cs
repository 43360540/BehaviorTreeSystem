namespace BehaviorTree
{
    public abstract class CompositeBase<TContext> : NodeBase<TContext>
    {
        protected INode<TContext>[] Children { get; }

        protected CompositeBase(params INode<TContext>[] children)
        {
            Children = children;
        }

        public override void Reset()
        {
            base.Reset();

            foreach (var c in Children)
                c.Reset();
        }
    }
}