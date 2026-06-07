using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class DecoratorBase<TContext> : NodeBase<TContext>
    {
        protected INode<TContext> Child => ChildNodes![0];

        public DecoratorBase(INode<TContext> child, string? name = null) : base(name)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            ChildNodes = [child];
        }

        protected override void OnTimeElapse(float dt)
        {
            Child.TimeElapse(dt);
        }

        protected override void OnAbort(TContext ctx)
        {
            base.OnAbort(ctx);
            Child.Abort(ctx);
        }
    }
}
