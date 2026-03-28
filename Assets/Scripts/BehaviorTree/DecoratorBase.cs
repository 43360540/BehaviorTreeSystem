using System;

namespace BehaviorTree
{
    public abstract class DecoratorBase<TContext> : NodeBase<TContext>
    {
        protected INode<TContext> Child;

        public DecoratorBase(INode<TContext> child)
        {
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        protected override void OnAbort(TContext ctx)
        {
            base.OnAbort(ctx);
            Child.Abort(ctx);
        }
    }
}