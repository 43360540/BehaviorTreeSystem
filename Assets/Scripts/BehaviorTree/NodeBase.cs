namespace BehaviorTree
{
    public abstract class NodeBase<TContext> : INode<TContext>
    {
        private bool _started;
        public NodeStatus LastStatus { get; private set; } = NodeStatus.Failure;

        public NodeStatus Tick(TContext bb, float dt)
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

        public void Abort(TContext bb)
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

        protected abstract NodeStatus OnTick(TContext bb, float dt);

        protected virtual void OnStart(TContext bb) { }

        protected virtual void OnStop(TContext bb) { }

        protected virtual void OnAbort(TContext bb) { }

        protected virtual void OnReset() { }
    }
}