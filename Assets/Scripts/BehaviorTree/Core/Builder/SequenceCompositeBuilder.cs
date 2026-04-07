namespace BehaviorTree
{
    public sealed class SequenceCompositeBuilder<TContext> : CompositeBuilderBase<TContext, SequenceCompositeBuilder<TContext>>
    {
        protected override INode<TContext> CreateComposite(INode<TContext>[] children) =>
            new SequenceComposite<TContext>(children);
    }
}