using System;

namespace BehaviorTree
{
    public sealed class QuickAction<TContext> : ActionBase<TContext>
    {
        public Action<TContext> OnStart { get; }
        public Func<TContext, float, NodeStatus> OnTick { get; }
        public Action<TContext, NodeStatus> OnStop { get; }
        public Action<TContext> OnAbort { get; }
        public Action OnReset { get; }

        public QuickAction(Func<TContext, float, NodeStatus> onTick, Action<TContext> onStart = null,
                            Action<TContext, NodeStatus> onStop = null, Action<TContext> onAbort = null, Action onReset = null)
        {
            OnStart = onStart;
            OnTick = onTick ?? throw new ArgumentNullException(nameof(onTick));
            OnStop = onStop;
            OnAbort = onAbort;
            OnReset = onReset;
        }

        public override void Start(TContext ctx) =>
            OnStart?.Invoke(ctx);

        public override NodeStatus Tick(TContext ctx, float dt) =>
            OnTick.Invoke(ctx, dt);

        public override void Stop(TContext ctx, NodeStatus stopStatus) =>
            OnStop?.Invoke(ctx, stopStatus);

        public override void Abort(TContext ctx) =>
            OnAbort?.Invoke(ctx);

        public override void Reset() =>
            OnReset?.Invoke();
    }
}