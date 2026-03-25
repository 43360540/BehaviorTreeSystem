namespace BehaviorTree
{
    // Memory Sequence
    public sealed class SequenceComposite<TContext> : CompositeBase<TContext>
    {
        private int _index = 0;

        public SequenceComposite(params INode<TContext>[] children) : base(children) { }

        protected override void OnStart(TContext bb)
        {
            base.OnStart(bb);

            _index = 0;
        }

        protected override NodeStatus OnTick(TContext bb, float dt)
        {
            while (_index < Children.Length)
            {
                var status = Children[_index].Tick(bb, dt);

                if (status != NodeStatus.Success)
                    return status;
                _index++;
            }
            return NodeStatus.Success;
        }

        protected override void OnStop(TContext bb, NodeStatus stopStatus)
        {
            base.OnStop(bb, stopStatus);

            _index = 0;
        }

        protected override void OnAbort(TContext bb)
        {
            base.OnAbort(bb);

            if (_index >= 0 && _index < Children.Length)
                Children[_index].Abort(bb);
        }

        protected override void OnReset()
        {
            base.OnReset();

            _index = 0;
        }  
    }
}