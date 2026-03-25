namespace BehaviorTree
{
    public interface INode<TContext>
    {
        NodeStatus Tick(TContext ctx, float dt);
        void Reset();
        void Abort(TContext ctx);
    }

    public interface IGuard<TContext>
    {
        bool CanEnter(TContext ctx, float dt);
    }
    public interface ICondition<TContext>
    {
        bool Evaluate(TContext ctx, float dt);
    }

    public enum NodeStatus
    {
        None,
        Success,
        Running,
        Failure,
    }
}