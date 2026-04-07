using System;
namespace BehaviorTree
{
    public static class SingleChildBuilderExtension
    {
        // ConditionLeaf
        public static void Check<TContext>(
            this ISingleChild<TContext> b, Func<TContext, float, bool> predicate)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            b.Set(BTNodeFactory<TContext>.Condition(qCondition));
        }

        public static void Check<TContext>(
            this ISingleChild<TContext> b, Func<float, bool> predicate)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            b.Set(BTNodeFactory<TContext>.Condition(qCondition));
        }

        public static void Check<TContext>(
            this ISingleChild<TContext> b, Func<bool> predicate)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            b.Set(BTNodeFactory<TContext>.Condition(qCondition));
        }

        // ActionLeaf - Params: builder, tick, start, stop, abort, reset
        public static void Do<TContext>(
            this ISingleChild<TContext> b,
            Func<TContext, float, NodeStatus> tick, 
            Action<TContext> start = null, Action<TContext, NodeStatus> stop = null,
            Action<TContext> abort = null, Action reset = null)
        {
            var qAction = new QuickAction<TContext>(tick, start, stop, abort, reset);
            b.Set(BTNodeFactory<TContext>.Action(qAction));
        }

        public static void Do<TContext>(
            this ISingleChild<TContext> b,
            Func<float, NodeStatus> tick, 
            Action start = null, Action<NodeStatus> stop = null,
            Action abort = null, Action reset = null)
        {
            var qAction = new QuickAction<TContext>(tick, start, stop, abort, reset);
            b.Set(BTNodeFactory<TContext>.Action(qAction));
        }

        // GuardDecorator
        public static void When<TContext>(this ISingleChild<TContext> b,
            Func<TContext, float, bool> predicate, 
            Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            b.Set(BTNodeFactory<TContext>.Guard(qCondition, buildAction));
        }

        public static void When<TContext>(this ISingleChild<TContext> b,
            Func<float, bool> predicate, 
            Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            b.Set(BTNodeFactory<TContext>.Guard(qCondition, buildAction));
        }

        public static void When<TContext>(this ISingleChild<TContext> b,
            Func<bool> predicate, 
            Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            b.Set(BTNodeFactory<TContext>.Guard(qCondition, buildAction));
        }
    }
}
