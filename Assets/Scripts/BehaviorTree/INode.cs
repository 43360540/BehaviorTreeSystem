namespace BehaviorTree
{
    public interface INode<TContext>
    {
        NodeStatus Tick(TContext bb, float dt);
        void Reset();
        void Abort(TContext bb);
    }

    public interface IGuard<TContext>
    {
        bool CanEnter(TContext bb, float dt);
    }
    public interface ICondition<TContext>
    {
        bool Evaluate(TContext bb, float dt);
    }

    public enum NodeStatus
    {
        Success,
        Running,
        Failure,
    }
}