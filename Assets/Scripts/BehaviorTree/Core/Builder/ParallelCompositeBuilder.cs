namespace BehaviorTree
{
    public sealed class ParallelCompositeBuilder<TContext> : CompositeBuilderBase<TContext, ParallelCompositeBuilder<TContext>>
    {
        protected override INode<TContext> CreateComposite(INode<TContext>[] children) =>
            new ParallelComposite<TContext>(children);
    }
}