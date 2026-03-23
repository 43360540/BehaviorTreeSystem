using System;

namespace BehaviorTree
{
    public sealed class ActionBundle<TContext>
    {
        public Action<TContext> OnStart { get; }
        public Func<TContext, float, NodeStatus> OnTick { get; }
        public Action<TContext> OnStop { get; }
        public Action<TContext> OnAbort { get; }
        public Action OnReset { get; }

        public ActionBundle(Func<TContext, float, NodeStatus> onTick, Action<TContext> onStart = null,
                            Action<TContext> onStop = null, Action<TContext> onAbort = null, Action onReset = null)
        {
            OnStart = onStart;
            OnTick = onTick ?? throw new ArgumentNullException(nameof(onTick));
            OnStop = onStop;
            OnAbort = onAbort;
            OnReset = onReset;
        }
    }
}