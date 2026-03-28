using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class CompositeBuilderBase<TContext, TSelf> where TSelf : CompositeBuilderBase<TContext, TSelf>
    {
        private readonly List<INode<TContext>> _children = new();
        private TSelf Self => (TSelf)this;

        public TSelf Condition(ICondition<TContext> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return Node(new ConditionLeaf<TContext>(predicate));
        }

        public TSelf Condition(Func<TContext, float, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return Node(new ConditionLeaf<TContext>(predicate));
        }

        public TSelf Action(BTAction<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return Node(new ActionLeaf<TContext>(action));
        }

        public TSelf Action(ActionBundle<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return Node(new ActionLeaf<TContext>(action));
        }

        public TSelf Guard(ICondition<TContext> condition, Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            else if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new GuardDecoratorBuilder<TContext>(condition);
            buildAction(builder);

            return Node(builder.Build());
        }

        public TSelf Guard(Func<TContext, float, bool> predicate, Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            else if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new GuardDecoratorBuilder<TContext>(predicate);
            buildAction(builder);

            return Node(builder.Build());
        }

        public TSelf Selector(Action<SelectorCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new SelectorCompositeBuilder<TContext>();
            buildAction(builder);

            return Node(builder.Build());
        }

        public TSelf Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new SequenceCompositeBuilder<TContext>();
            buildAction(builder);

            return Node(builder.Build());
        }

        public TSelf Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new ParallelCompositeBuilder<TContext>();
            buildAction(builder);

            return Node(builder.Build());
        }

        public TSelf Node(INode<TContext> node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            
            _children.Add(node);
            return Self;
        }

        protected abstract INode<TContext> CreateComposite(INode<TContext>[] children);

        public INode<TContext> Build() 
        {
            if (_children.Exists(c => c == null))
                throw new InvalidOperationException("Composite node cannot be null or contains null.");

            return CreateComposite(_children.ToArray());
        }
    }
}