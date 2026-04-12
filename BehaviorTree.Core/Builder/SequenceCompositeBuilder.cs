namespace BehaviorTree
{
    public sealed class SequenceCompositeBuilder<TContext> : CompositeBuilderBase<TContext, SequenceCompositeBuilder<TContext>>
    {
        public SequenceCompositeBuilder(string? name) : base(name) { }

        protected override INode<TContext> CreateComposite(INode<TContext>[] children, string? name) =>
            new SequenceComposite<TContext>(name, children);
    }
}