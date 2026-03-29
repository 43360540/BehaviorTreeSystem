using System;

namespace BehaviorTree
{
    public static class BT<TContext>
    {
        public static INode<TContext> Build(Action<RootBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            RootBuilder<TContext> rootBuilder = new();
            buildAction(rootBuilder);
            return rootBuilder.Build();
        }
    }
}
