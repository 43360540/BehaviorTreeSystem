namespace BehaviorTree
{
    public interface INode
    {
        NodeStatus Tick(BlackBoardMono bb, float dt);
        void Reset();
        void Abort(BlackBoardMono bb);
    }

    public interface ISelectable
    {
        bool IsSelectable();
    }
    public interface ICondition
    {
        bool Evaluate();
    }

    public enum NodeStatus
    {
        Success,
        Running,
        Failure,
    }
}