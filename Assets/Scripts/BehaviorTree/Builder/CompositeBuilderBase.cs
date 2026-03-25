using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class CompositeBuilderBase<TContext, TSelf> where TSelf : CompositeBuilderBase<TContext, TSelf>
    {
        protected readonly List<INode<TContext>> Children = new();
        protected TSelf Self => (TSelf)this;

        public TSelf Condition(ICondition<TContext> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            Children.Add(new ConditionLeaf<TContext>(predicate));
            return Self;
        }

        public TSelf Condition(Func<TContext, float, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            Children.Add(new ConditionLeaf<TContext>(predicate));
            return Self;
        }

        public TSelf Action(BTAction<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Children.Add(new ActionLeaf<TContext>(action));
            return Self;
        }

        public TSelf Action(ActionBundle<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Children.Add(new ActionLeaf<TContext>(action));
            return Self;
        }

        public TSelf Guard(ICondition<TContext> condition, Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            else if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new GuardDecoratorBuilder<TContext>(condition);
            buildAction(builder);
            Children.Add(builder.Build());
            return Self;
        }

        public TSelf Guard(Func<TContext, float, bool> predicate, Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            else if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new GuardDecoratorBuilder<TContext>(predicate);
            buildAction(builder);
            Children.Add(builder.Build());
            return Self;
        }

        public TSelf Selector(Action<SelectorCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new SelectorCompositeBuilder<TContext>();
            buildAction(builder);
            Children.Add(builder.Build());

            return Self;
        }

        public TSelf Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new SequenceCompositeBuilder<TContext>();
            buildAction(builder);
            Children.Add(builder.Build());

            return Self;
        }

        protected abstract INode<TContext> CreateComposite(INode<TContext>[] children);

        public INode<TContext> Build() 
        {
            if (Children == null || Children.Exists(c => c == null))
                throw new InvalidOperationException("Composite node cannot be null or contains null.");

            return CreateComposite(Children.ToArray());
        }
    }
}