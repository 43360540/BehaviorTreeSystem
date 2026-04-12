using System;

namespace BehaviorTree
{
    public static class BTNodeFactory<TContext>
    {
        
        public static INode<TContext> Condition(ICondition<TContext> condition, string? name = null)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            return new ConditionLeaf<TContext>(condition, name);
        }
        
        public static INode<TContext> Action(ActionBase<TContext> action, string? name = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return new ActionLeaf<TContext>(action, name);
        }

        public static INode<TContext> Guard(ICondition<TContext> condition, 
            Action<GuardDecoratorBuilder<TContext>> buildAction, string? name = null)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            GuardDecoratorBuilder<TContext> builder = new(condition, name);
            buildAction(builder);

            return builder.Build();
        }

        public static INode<TContext> Selector(Action<SelectorCompositeBuilder<TContext>> buildAction, string? name = null)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SelectorCompositeBuilder<TContext> builder = new(name);
            buildAction(builder);

            return builder.Build();
        }

        public static INode<TContext> Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction, string? name = null)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SequenceCompositeBuilder<TContext> builder = new(name);
            buildAction(builder);

            return builder.Build();
        }

        public static INode<TContext> Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction, string? name = null)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            ParallelCompositeBuilder<TContext> builder = new(name);
            buildAction(builder);

            return builder.Build();
        }
    }
}