using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class CompositeBuilderBase<TContext, TSelf> : IMultiChildren<TSelf, TContext> where TSelf : CompositeBuilderBase<TContext, TSelf>
    {
        private readonly List<INode<TContext>> _children = new();
        private string _name;
        private TSelf Self => (TSelf)this;

        public CompositeBuilderBase(string name) => _name = name;

        // ConditionLeaf
        public TSelf Check(ICondition<TContext> condition, string name = null) =>
            Add(BTNodeFactory<TContext>.Condition(condition, name));
        // ActionLeaf
        public TSelf Do(ActionBase<TContext> action, string name = null) =>
            Add(BTNodeFactory<TContext>.Action(action, name));
        // GuardDecorator
        public TSelf When(ICondition<TContext> condition, 
            Action<GuardDecoratorBuilder<TContext>> buildAction, string name = null) =>
            Add(BTNodeFactory<TContext>.Guard(condition, buildAction, name));

        public TSelf Selector(Action<SelectorCompositeBuilder<TContext>> buildAction, string name = null) =>
            Add(BTNodeFactory<TContext>.Selector(buildAction, name));

        public TSelf Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction, string name = null) =>
            Add(BTNodeFactory<TContext>.Sequence(buildAction, name));

        public TSelf Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction, string name = null) =>
            Add(BTNodeFactory<TContext>.Parallel(buildAction, name));

        public TSelf Add(INode<TContext> node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            
            _children.Add(node);
            return Self;
        }

        protected abstract INode<TContext> CreateComposite(INode<TContext>[] children, string name);

        public INode<TContext> Build() 
        {
            if (_children.Count == 0 || _children.Exists(c => c == null))
                throw new InvalidOperationException("Children cannot be null or contains null in Composite.");

            return CreateComposite(_children.ToArray(), _name);
        }
    }
}