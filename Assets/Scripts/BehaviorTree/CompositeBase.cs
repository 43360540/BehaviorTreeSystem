namespace BehaviorTree
{
    public abstract class CompositeBase : NodeBase
    {
        protected INode[] Children { get; }

        protected CompositeBase(params INode[] children)
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