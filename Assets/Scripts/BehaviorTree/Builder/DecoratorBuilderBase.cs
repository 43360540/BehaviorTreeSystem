using System;

namespace BehaviorTree
{
    public abstract class DecoratorBuilderBase<TLogic, TContext> where TLogic : class
    {
        private INode<TContext> _child;
        private readonly TLogic _logic;

        public DecoratorBuilderBase(TLogic logic)
        {
            _logic = logic ?? throw new ArgumentNullException(nameof(logic));
        }
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
            if (_child != null)
                throw new InvalidOperationException("Child has been set.");

            _child = node ?? throw new ArgumentNullException(nameof(node));
        }

        protected abstract INode<TContext> CreateDecorator(TLogic logic, INode<TContext> child);

        public INode<TContext> Build()
        {
            if (_child == null)
                throw new InvalidOperationException("Decorator must have a child node.");

            return CreateDecorator(_logic, _child);
        }
    }
}