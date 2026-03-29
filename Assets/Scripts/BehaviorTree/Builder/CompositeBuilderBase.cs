using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class CompositeBuilderBase<TContext, TSelf> where TSelf : CompositeBuilderBase<TContext, TSelf>
    {
        private readonly List<INode<TContext>> _children = new();
        private TSelf Self => (TSelf)this;

        public TSelf Condition(ICondition<TContext> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            return AddChild(new ConditionLeaf<TContext>(condition));
        }

        public TSelf Condition(Func<TContext, float, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return AddChild(new ConditionLeaf<TContext>(predicate));
        }

        public TSelf Action(ActionBase<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return AddChild(new ActionLeaf<TContext>(action));
        }

        public TSelf Guard(ICondition<TContext> condition, Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            else if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            GuardDecoratorBuilder<TContext> builder = new(condition);
            buildAction(builder);

            return AddChild(builder.Build());
        }

        public TSelf Guard(Func<TContext, float, bool> predicate, Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            else if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            GuardDecoratorBuilder<TContext> builder = new(predicate);
            buildAction(builder);

            return AddChild(builder.Build());
        }

        public TSelf Selector(Action<SelectorCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SelectorCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            return AddChild(builder.Build());
        }

        public TSelf Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SequenceCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            return AddChild(builder.Build());
        }

        public TSelf Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            ParallelCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            return AddChild(builder.Build());
        }

        public TSelf AddChild(INode<TContext> node)
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