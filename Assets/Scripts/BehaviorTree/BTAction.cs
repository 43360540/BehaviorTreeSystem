namespace BehaviorTree
{
    public abstract class BTAction<TContext>
    {
        public virtual void Start(TContext ctx) { }

        public abstract NodeStatus Tick(TContext ctx, float dt);

        public virtual void Stop(TContext ctx, NodeStatus stopStatus) { }

        public virtual void Abort(TContext ctx) { }

        public virtual void Reset() { }
    }
}