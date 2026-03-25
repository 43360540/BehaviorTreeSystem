using System;

namespace BehaviorTree
{
    public static class BT<TContext>
    {
        public static INode<TContext> Build(Action<RootBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var rootBuilder = new RootBuilder<TContext>();
            buildAction(rootBuilder);
            return rootBuilder.Build();
        }
    }
}
