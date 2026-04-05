using System;

namespace BehaviorTree
{
    public sealed class RootBuilder<TContext>
    {
        private INode<TContext> _root = null;
        // ConditionLeaf
        public void Check(ICondition<TContext> condition) =>
            Set(BTNodeFactory<TContext>.Check(condition));
        // ActionLeaf
        public void Do(ActionBase<TContext> action) =>
            Set(BTNodeFactory<TContext>.Do(action));
        // GuardDecorator
        public void When(ICondition<TContext> condition, Action<GuardDecoratorBuilder<TContext>> buildAction) =>
            Set(BTNodeFactory<TContext>.When(condition, buildAction));

        public void Selector(Action<SelectorCompositeBuilder<TContext>> buildAction) =>
            Set(BTNodeFactory<TContext>.Selector(buildAction));

        public void Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction) =>
            Set(BTNodeFactory<TContext>.Sequence(buildAction));

        public void Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction) =>
            Set(BTNodeFactory<TContext>.Parallel(buildAction));

        public void Set(INode<TContext> node)
        {
            if (_root != null)
                throw new InvalidOperationException("Root node already set.");

            _root = node ?? throw new ArgumentNullException(nameof(node));
        }

        public INode<TContext> Build()
        {
            return _root ?? throw new InvalidOperationException("Root node cannot be null.");
        }
    }
}