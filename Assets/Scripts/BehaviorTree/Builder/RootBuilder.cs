using System;

namespace BehaviorTree
{
    public sealed class RootBuilder<TContext>
    {
        private INode<TContext> _root = null;

        public void Check(ICondition<TContext> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            SetRoot(new ConditionLeaf<TContext>(condition));
        }

        public void Do(ActionBase<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            SetRoot(new ActionLeaf<TContext>(action));
        }

        public void When(Func<TContext, float, bool> predicate, Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            GuardDecoratorBuilder<TContext> builder = new(new DelegateCondition<TContext>(predicate));
            buildAction(builder);

            SetRoot(builder.Build());
        }

        public void When(ICondition<TContext> condition, Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            GuardDecoratorBuilder<TContext> builder = new(condition);
            buildAction(builder);

            SetRoot(builder.Build());
        }

        public void Selector(Action<SelectorCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SelectorCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            SetRoot(builder.Build());
        }

        public void Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SequenceCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            SetRoot(builder.Build());
        }

        public void Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            ParallelCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            SetRoot(builder.Build());
        }

        public void SetRoot(INode<TContext> node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (_root != null)
                throw new InvalidOperationException("Root node already set.");

            _root = node;
        }

        public INode<TContext> Build()
        {
            return _root ?? throw new InvalidOperationException("Root node cannot be null.");
        }
    }
}