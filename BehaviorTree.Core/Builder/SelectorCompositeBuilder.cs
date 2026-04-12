namespace BehaviorTree
{
    public sealed class SelectorCompositeBuilder<TContext> : CompositeBuilderBase<TContext, SelectorCompositeBuilder<TContext>>
    {
        public SelectorCompositeBuilder(string? name) : base(name) { }

        protected override INode<TContext> CreateComposite(INode<TContext>[] children, string? name) =>
            new SelectorComposite<TContext>(name, children);
    }
}