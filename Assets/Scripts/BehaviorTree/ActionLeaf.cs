using System;

namespace BehaviorTree
{
    public sealed class ActionLeaf<TContext> : NodeBase<TContext>
    {
        private readonly ActionBase<TContext> _action;

        public ActionLeaf(ActionBase<TContext> action) =>
            _action = action ?? throw new ArgumentNullException(nameof(action));

        protected override void OnStart(TContext ctx) =>
            _action.Start(ctx);

        protected override NodeStatus OnTick(TContext ctx, float dt) =>
            _action.Tick(ctx, dt);

        protected override void OnStop(TContext ctx, NodeStatus stopStatus) =>
            _action.Stop(ctx, stopStatus);

        protected override void OnAbort(TContext ctx) =>
            _action.Abort(ctx);

        protected override void OnReset() =>
            _action.Reset();
    }
}
