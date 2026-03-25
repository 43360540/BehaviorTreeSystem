namespace BehaviorTree
{
    public sealed class SelectorCompositeBuilder<TContext> : CompositeBuilderBase<TContext, SelectorCompositeBuilder<TContext>>
    {
        protected override INode<TContext> CreateComposite(INode<TContext>[] children) =>
            new SelectorComposite<TContext>(children);
    }
}