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

            if (_root != null)
                throw new InvalidOperationException("Root node already set.");

            var builder = new SelectorCompositeBuilder<TContext>();
            buildAction(builder);

            _root = builder.Build();
        }

        public void Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));
                
            if (_root != null)
                throw new InvalidOperationException("Root node already set.");

            var builder = new SequenceCompositeBuilder<TContext>();
            buildAction(builder);

            _root = builder.Build();
        }

        public INode<TContext> Build()
        {
            return _root ?? throw new InvalidOperationException("Root node cannot be null.");
        }
    }
}