namespace BehaviorTree
{
    public sealed class ActionLeaf : NodeBase
    {
        private ActionBundle _bundle = null;

        public ActionLeaf(ActionDefinition def)
        {
            _bundle = new ActionBundle(
                onStart: def.Start,
                onTick: def.Tick,
                onStop: def.Stop,
                onAbort: def.Abort,
                onReset: def.Reset);
        }

        public ActionLeaf(ActionBundle bundle)
        {
            _bundle = bundle;
        }

        protected override void OnStart(BlackBoardMono bb)
        {
            base.OnStart(bb);
            _bundle.OnStart?.Invoke(bb);
        }

        protected override NodeStatus OnTick(BlackBoardMono bb, float dt)
        {
            return _bundle.OnTick.Invoke(bb, dt);
        }

        protected override void OnStop(BlackBoardMono bb)
        {
            base.OnStop(bb);
            _bundle.OnStop?.Invoke(bb);
        }

        protected override void OnAbort(BlackBoardMono bb)
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
