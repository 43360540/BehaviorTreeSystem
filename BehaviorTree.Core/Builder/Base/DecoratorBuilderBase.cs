using System;

namespace BehaviorTree
{
    public abstract class DecoratorBuilderBase<TLogic, TContext> : ISingleChild<TContext> where TLogic : class
    {
        private INode<TContext>? _child;
        private readonly TLogic _logic;
        private readonly string? _name;

        public DecoratorBuilderBase(TLogic logic, string? name = null)
        {
            _logic = logic ?? throw new ArgumentNullException(nameof(logic));
            _name = name;
        }
        // ConditionLeaf
        public void Check(ICondition<TContext> condition, string? name = null) =>
            Set(BTNodeFactory<TContext>.Condition(condition, name));
        // ActionLeaf
        public void Do(ActionBase<TContext> action, string? name = null) =>
            Set(BTNodeFactory<TContext>.Action(action, name));
        // GuardDecorator
        public void When(ICondition<TContext> condition, 
            Action<GuardDecoratorBuilder<TContext>> buildAction, string? name = null) =>
            Set(BTNodeFactory<TContext>.Guard(condition, buildAction, name));

        public void Selector(Action<SelectorCompositeBuilder<TContext>> buildAction, string? name = null) =>
            Set(BTNodeFactory<TContext>.Selector(buildAction, name));

        public void Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction, string? name = null) =>
            Set(BTNodeFactory<TContext>.Sequence(buildAction, name));

        public void Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction, string? name = null) =>
            Set(BTNodeFactory<TContext>.Parallel(buildAction, name));

        public void Set(INode<TContext> node)
        {
            if (_child != null)
                throw new InvalidOperationException("Child has been set.");

            _child = node ?? throw new ArgumentNullException(nameof(node));
        }

        protected abstract INode<TContext> CreateDecorator(TLogic logic, INode<TContext> child, string? name);

        public INode<TContext> Build()
        {
            if (_child == null)
                throw new InvalidOperationException("Decorator must have a child node.");

            return CreateDecorator(_logic, _child, _name);
        }
    }
}