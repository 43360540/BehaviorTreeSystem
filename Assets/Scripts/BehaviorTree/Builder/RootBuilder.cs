using System;

namespace BehaviorTree
{
    public sealed class RootBuilder<TContext>
    {
        private INode<TContext> _root = null;

        public void Selector(Action<SelectorCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SelectorCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            SetRoot(builder.Build());
        }

        public void Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            SequenceCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            SetRoot(builder.Build());
        }

        public void Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            ParallelCompositeBuilder<TContext> builder = new();
            buildAction(builder);

            SetRoot(builder.Build());
        }

        public void SetRoot(INode<TContext> node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (_root != null)
                throw new InvalidOperationException("Root node already set.");

            _root = node;
        }

        public INode<TContext> Build()
        {
            return _root ?? throw new InvalidOperationException("Root node cannot be null.");
        }
    }
}