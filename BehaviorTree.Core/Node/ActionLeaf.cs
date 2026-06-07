using System;

namespace BehaviorTree
{
    public sealed class ActionLeaf<TContext> : LeafBase<TContext>
    {
        private readonly ActionBase<TContext> _action;

        public ActionLeaf(ActionBase<TContext> action, string? name = null) : base(name ?? action.GetType().Name) =>
            _action = action ?? throw new ArgumentNullException(nameof(action));

        protected override void OnStart(TContext ctx) =>
            _action.Start(ctx);

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            NodeStatus status = _action.Tick(ctx, dt);
            Info = status.ToString();

            return status;
        }

        protected override void OnStop(TContext ctx, NodeStatus stopStatus) =>
            _action.Stop(ctx, stopStatus);

        protected override void OnAbort(TContext ctx) =>
            _action.Abort(ctx);

        protected override void OnReset() =>
            _action.Reset();
    }

    public static class ActionExtension
    {
        public static TSelf Do<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            ActionBase<TContext> action, string? name = null) =>
            b.Add(new ActionLeaf<TContext>(action, name));

        public static TSelf Do<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<TContext, float, NodeStatus> onTick, Action<TContext>? onStart = null,
            Action<TContext, NodeStatus>? onStop = null, Action<TContext>? onAbort = null,
            Action? onReset = null, string? name = null)
        {
            var qAction = new QuickAction<TContext>(onTick, onStart, onStop, onAbort, onReset);
            return b.Add(new ActionLeaf<TContext>(qAction, name));
        }

        public static TSelf Do<TSelf, TContext>(this IMultiChildren<TSelf, TContext> b,
            Func<float, NodeStatus> onTick, Action? onStart = null,
            Action<NodeStatus>? onStop = null, Action? onAbort = null,
            Action? onReset = null, string? name = null)
        {
            var qAction = new QuickAction<TContext>(onTick, onStart, onStop, onAbort, onReset);
            return b.Add(new ActionLeaf<TContext>(qAction, name));
        }

        public static void Do<TContext>(this ISingleChild<TContext> b,
            ActionBase<TContext> action, string? name = null) =>
            b.Set(new ActionLeaf<TContext>(action, name));

        public static void Do<TContext>(this ISingleChild<TContext> b,
            Func<TContext, float, NodeStatus> onTick, Action<TContext>? onStart = null,
            Action<TContext, NodeStatus>? onStop = null, Action<TContext>? onAbort = null,
            Action? onReset = null, string? name = null)
        {
            var qAction = new QuickAction<TContext>(onTick, onStart, onStop, onAbort, onReset);
            b.Set(new ActionLeaf<TContext>(qAction, name));
        }

        public static void Do<TContext>(this ISingleChild<TContext> b,
            Func<float, NodeStatus> onTick, Action? onStart = null,
            Action<NodeStatus>? onStop = null, Action? onAbort = null,
            Action? onReset = null, string? name = null)
        {
            var qAction = new QuickAction<TContext>(onTick, onStart, onStop, onAbort, onReset);
            b.Set(new ActionLeaf<TContext>(qAction, name));
        }
    }
}
