namespace BehaviorTree
{
    public sealed class SequenceNode : CompositeBase
    {
        private int _index = 0;

        public SequenceNode(params INode[] children) : base(children) { }

        protected override void OnStart(BlackBoardMono bb)
        {
            base.OnStart(bb);

            _index = 0;
        }

        protected override NodeStatus OnTick(BlackBoardMono bb, float dt)
        {
            while (_index < Children.Length)
            {
                var status = Children[_index].Tick(bb, dt);

                if (status == NodeStatus.Failure || status == NodeStatus.Running)
                    return status;
                _index++;
            }
            return NodeStatus.Success;
        }

        protected override void OnAbort(BlackBoardMono bb)
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

        protected override void OnStop(BlackBoardMono bb)
        {
            base.OnStop(bb);

            _index = 0;
        }
    }
}