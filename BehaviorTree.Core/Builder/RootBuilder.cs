using System;

namespace BehaviorTree
{
    public sealed class RootBuilder<TContext> : ISingleChild<TContext>
    {
        private INode<TContext>? _root;

        public void Set(INode<TContext> node)
        {
            if (_root != null)
                throw new InvalidOperationException("Root node already set.");

            _root = node ?? throw new ArgumentNullException(nameof(node));
        }

        public INode<TContext> Build()
        {
            return _root ?? throw new InvalidOperationException("Root node cannot be null.");
        }
    }
}