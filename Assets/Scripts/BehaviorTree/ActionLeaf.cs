using System;

namespace BehaviorTree
{
    public sealed class ActionLeaf<TContext> : NodeBase<TContext>
    {
        private ActionBundle<TContext> _bundle = null;

        public ActionLeaf(BTAction<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _bundle = new ActionBundle<TContext>(
                onStart: action.Start,
                onTick: action.Tick,
                onStop: action.Stop,
                onAbort: action.Abort,
                onReset: action.Reset);
        }

        public ActionLeaf(ActionBundle<TContext> bundle)
        {
            _bundle = bundle ?? throw new ArgumentNullException(nameof(bundle));
        }

        protected override void OnStart(TContext ctx)
        {
            base.OnStart(ctx);
            _bundle.OnStart?.Invoke(ctx);
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            return _bundle.OnTick.Invoke(ctx, dt);
        }

        protected override void OnStop(TContext ctx, NodeStatus stopStatus)
        {
            base.OnStop(ctx, stopStatus);
            _bundle.OnStop?.Invoke(ctx, stopStatus);
        }

        protected override void OnAbort(TContext ctx)
        {
            base.OnAbort(ctx);
            _bundle.OnAbort?.Invoke(ctx);
        }

        protected override void OnReset()
        {
            base.OnReset();
            _bundle.OnReset?.Invoke();
        }
    }
}
