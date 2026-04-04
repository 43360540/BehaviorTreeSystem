using System;

namespace BehaviorTree
{
    public sealed class RootBuilder<TContext>
    {
        private INode<TContext> _root = null;
        // ConditionLeaf
        public void Check(ICondition<TContext> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            Set(new ConditionLeaf<TContext>(condition));
        }
        // ActionLeaf
        public void Do(ActionBase<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Set(new ActionLeaf<TContext>(action));
        }
        // GuardDecorator
        public void When(ICondition<TContext> condition, Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            GuardDecoratorBuilder<TContext> builder = new(condition);
            buildAction(builder);

            Set(builder.Build());
        }

        public void Selector(Action<SelectorCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SelectorCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            Set(builder.Build());
        }

        public void Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SequenceCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            Set(builder.Build());
        }

        public void Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            ParallelCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            Set(builder.Build());
        }

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