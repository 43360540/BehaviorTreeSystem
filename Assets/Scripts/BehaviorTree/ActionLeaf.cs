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

        protected override void OnStart(TContext bb)
        {
            base.OnStart(bb);
            _bundle.OnStart?.Invoke(bb);
        }

        protected override NodeStatus OnTick(TContext bb, float dt)
        {
            return _bundle.OnTick.Invoke(bb, dt);
        }

        protected override void OnStop(TContext bb, NodeStatus stopStatus)
        {
            base.OnStop(bb, stopStatus);
            _bundle.OnStop?.Invoke(bb, stopStatus);
        }

        protected override void OnAbort(TContext bb)
        {
            base.OnAbort(bb);
            _bundle.OnAbort?.Invoke(bb);
        }

        protected override void OnReset()
        {
            base.OnReset();
            _bundle.OnReset?.Invoke();
        }
    }
}
