using System;

namespace BehaviorTree
{
    public class BTRunner<TContext>
    {
        public TContext Context { get; }
        public INode<TContext> Tree { get; }

        public BTRunner(TContext context, INode<TContext> tree)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Tree = tree ?? throw new ArgumentNullException(nameof(tree));
        }

        public void Tick(float duration)
        {
            Tree.TimeElapse(duration);
            Tree.Tick(Context, duration);
        }

        public void Abort()
        {
            Tree.Abort(Context);
        }
    }
}