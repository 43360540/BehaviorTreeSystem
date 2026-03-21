namespace BehaviorTree
{
    public abstract class NodeBase : INode
    {
        private bool _started;
        public NodeStatus LastStatus { get; private set; } = NodeStatus.Failure;

        public NodeStatus Tick(BlackBoardMono bb, float dt)
        {
            if (!_started)
            {
                _started = true;
                OnStart(bb);
            }

            var status = OnTick(bb, dt);
            LastStatus = status;

            if (LastStatus != NodeStatus.Running)
            {
                OnStop(bb);
                _started = false;
            }

            return status;
        }

        public void Abort(BlackBoardMono bb)
        {
            if (!_started)
                return;
            _started = false;
            LastStatus = NodeStatus.Failure;

            OnAbort(bb);
        }

        public virtual void Reset()
        {
            _started = false;
            LastStatus = NodeStatus.Failure;

            OnReset();
        }

        protected abstract NodeStatus OnTick(BlackBoardMono bb, float dt);

        protected virtual void OnStart(BlackBoardMono bb) { }

        protected virtual void OnStop(BlackBoardMono bb) { }

        protected virtual void OnAbort(BlackBoardMono bb) { }

        protected virtual void OnReset() { }
    }
}