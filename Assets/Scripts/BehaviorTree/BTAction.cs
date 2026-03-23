namespace BehaviorTree
{
    public abstract class BTAction<TContext>
    {
        public virtual void Start(TContext bb) { }

        public abstract NodeStatus Tick(TContext bb, float dt);

        public virtual void Stop(TContext bb) { }

        public virtual void Abort(TContext bb) { }

        public virtual void Reset() { }
    }
}