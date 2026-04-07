using System;

namespace BehaviorTree
{
    public static class BTNodeFactory<TContext>
    {
        
        public static INode<TContext> Condition(ICondition<TContext> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            return new ConditionLeaf<TContext>(condition);
        }
        
        public static INode<TContext> Action(ActionBase<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return new ActionLeaf<TContext>(action);
        }

        public static INode<TContext> Guard(ICondition<TContext> condition, Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            GuardDecoratorBuilder<TContext> builder = new(condition);
            buildAction(builder);

            return builder.Build();
        }

        public static INode<TContext> Selector(Action<SelectorCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SelectorCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            return builder.Build();
        }

        public static INode<TContext> Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SequenceCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            return builder.Build();
        }

        public static INode<TContext> Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            ParallelCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            return builder.Build();
        }
    }
}