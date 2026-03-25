namespace BehaviorTree
{
    public sealed class ActionLeaf<TContext> : NodeBase<TContext>
    {
        private ActionBundle<TContext> _bundle = null;

        public ActionLeaf(BTAction<TContext> def)
        {
            _bundle = new ActionBundle<TContext>(
                onStart: def.Start,
                onTick: def.Tick,
                onStop: def.Stop,
                onAbort: def.Abort,
                onReset: def.Reset);
        }

        public ActionLeaf(ActionBundle<TContext> bundle)
        {
            _bundle = bundle;
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
            base.OnStop(bb, LastStatus);
            _bundle.OnStop?.Invoke(bb);
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
