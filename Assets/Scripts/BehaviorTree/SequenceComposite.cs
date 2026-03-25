namespace BehaviorTree
{
    // Memory Sequence
    public sealed class SequenceComposite<TContext> : CompositeBase<TContext>
    {
        private int _index = 0;

        public SequenceComposite(params INode<TContext>[] children) : base(children) { }

        protected override void OnStart(TContext ctx)
        {
            base.OnStart(ctx);

            _index = 0;
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            while (_index < Children.Length)
            {
                var status = Children[_index].Tick(ctx, dt);

                if (status != NodeStatus.Success)
                    return status;
                _index++;
            }
            return NodeStatus.Success;
        }

        protected override void OnStop(TContext ctx, NodeStatus stopStatus)
        {
            base.OnStop(ctx, stopStatus);

            _index = 0;
        }

        protected override void OnAbort(TContext ctx)
        {
            base.OnAbort(ctx);

            if (_index >= 0 && _index < Children.Length)
                Children[_index].Abort(ctx);
        }

        protected override void OnReset()
        {
            base.OnReset();

            _index = 0;
        }  
    }
}