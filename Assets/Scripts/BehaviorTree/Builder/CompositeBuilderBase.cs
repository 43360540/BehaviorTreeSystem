using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class CompositeBuilderBase<TContext, TSelf> where TSelf : CompositeBuilderBase<TContext, TSelf>
    {
        private readonly List<INode<TContext>> _children = new();
        private TSelf Self => (TSelf)this;
        // ConditionLeaf
        public TSelf Check(ICondition<TContext> condition) =>
            Add(BTNodeFactory<TContext>.Check(condition));
        // ActionLeaf
        public TSelf Do(ActionBase<TContext> action) =>
            Add(BTNodeFactory<TContext>.Do(action));
        // GuardDecorator
        public TSelf When(ICondition<TContext> condition, Action<GuardDecoratorBuilder<TContext>> buildAction) =>
            Add(BTNodeFactory<TContext>.When(condition, buildAction));

        public TSelf Selector(Action<SelectorCompositeBuilder<TContext>> buildAction) =>
            Add(BTNodeFactory<TContext>.Selector(buildAction));

        public TSelf Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction) =>
            Add(BTNodeFactory<TContext>.Sequence(buildAction));

        public TSelf Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction) =>
            Add(BTNodeFactory<TContext>.Parallel(buildAction));

        public TSelf Add(INode<TContext> node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            
            _children.Add(node);
            return Self;
        }

        protected abstract INode<TContext> CreateComposite(INode<TContext>[] children);

        public INode<TContext> Build() 
        {
            if (_children.Count == 0 || _children.Exists(c => c == null))
                throw new InvalidOperationException("Children cannot be null or contains null in Composite.");

            return CreateComposite(_children.ToArray());
        }
    }
}