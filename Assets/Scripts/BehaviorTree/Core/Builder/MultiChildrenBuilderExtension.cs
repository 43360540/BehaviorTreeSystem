using System;
namespace BehaviorTree
{
    public static class MultiChildrenBuilderExtension
    {
        // ConditionLeaf
        public static TSelf Check<TSelf, TContext>(
            this IMultiChildren<TSelf, TContext> b, Func<TContext, float, bool> predicate)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            return b.Add(BTNodeFactory<TContext>.Condition(qCondition));
        } 
        public static TSelf Check<TSelf, TContext>(
            this IMultiChildren<TSelf, TContext> b, Func<float, bool> predicate)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            return b.Add(BTNodeFactory<TContext>.Condition(qCondition));
        }

        public static TSelf Check<TSelf, TContext>(
            this IMultiChildren<TSelf, TContext> b, Func<bool> predicate)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            return b.Add(BTNodeFactory<TContext>.Condition(qCondition));
        }

        // ActionLeaf - Params: builder, tick, start, stop, abort, reset
        public static TSelf Do<TSelf, TContext>(
            this IMultiChildren<TSelf, TContext> b,
            Func<TContext, float, NodeStatus> tick, 
            Action<TContext> start = null, Action<TContext, NodeStatus> stop = null,
            Action<TContext> abort = null, Action reset = null)
        {
            var qAction = new QuickAction<TContext>(tick, start, stop, abort, reset);
            return b.Add(BTNodeFactory<TContext>.Action(qAction));
        }

        public static TSelf Do<TSelf, TContext>(
            this IMultiChildren<TSelf, TContext> b,
            Func<float, NodeStatus> tick, 
            Action start = null, Action<NodeStatus> stop = null,
            Action abort = null, Action reset = null)
        {
            var qAction = new QuickAction<TContext>(tick, start, stop, abort, reset);
            return b.Add(BTNodeFactory<TContext>.Action(qAction));
        }

        // GuardDecorator
        public static TSelf When<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<TContext, float, bool> predicate, 
            Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            return b.Add(BTNodeFactory<TContext>.Guard(qCondition, buildAction));
        }

        public static TSelf When<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<float, bool> predicate, 
            Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            return b.Add(BTNodeFactory<TContext>.Guard(qCondition, buildAction));
        }

        public static TSelf When<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<bool> predicate, 
            Action<GuardDecoratorBuilder<TContext>> buildAction)
        {
            var qCondition = new QuickCondition<TContext>(predicate);
            return b.Add(BTNodeFactory<TContext>.Guard(qCondition, buildAction));
        }
    }
}
