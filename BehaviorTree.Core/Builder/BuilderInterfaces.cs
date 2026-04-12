namespace BehaviorTree
{
    public interface ISingleChild<TContext>
    {
        void Set(INode<TContext> node);
    }

    public interface IMultiChildren<TSelf, TContext>
    {
        TSelf Add(INode<TContext> node);
    }
}
