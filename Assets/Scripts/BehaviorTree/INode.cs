namespace BehaviorTree
{
    public interface INode
    {
        NodeStatus Tick(BlackBoardMono bb, float dt);
        void Reset();
        void Abort(BlackBoardMono bb);
    }

    public interface ICondition
    {
        bool Evaluate(BlackBoardMono bb, float dt);
    }

    public enum NodeStatus
    {
        Success,
        Running,
        Failure,
    }
}