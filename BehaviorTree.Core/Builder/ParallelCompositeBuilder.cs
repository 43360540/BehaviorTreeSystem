namespace BehaviorTree
{
    public sealed class ParallelCompositeBuilder<TContext> : CompositeBuilderBase<TContext, ParallelCompositeBuilder<TContext>>
    {
        public ParallelCompositeBuilder(string? name) : base(name) { }

        protected override INode<TContext> CreateComposite(INode<TContext>[] children, string? name) =>
            new ParallelComposite<TContext>(name, children);
    }
}