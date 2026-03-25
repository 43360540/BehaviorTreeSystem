using System;

namespace BehaviorTree
{
    public abstract class DecoratorBuilderBase<TLogic, TContext>
    {
        private INode<TContext> _child;
        private TLogic _logic;

        public DecoratorBuilderBase(TLogic logic)
        {
            _logic = logic;
        }

        public void Action(BTAction<TContext> action)
        {
            if (_child != null)
                throw new InvalidOperationException("Child has been set.");

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _child = new ActionLeaf<TContext>(action);
        }

        public void Action(ActionBundle<TContext> action)
        {
            if (_child != null)
                throw new InvalidOperationException("Child has been set.");

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _child = new ActionLeaf<TContext>(action);
        }

        public void Selector(Action<SelectorCompositeBuilder<TContext>> buildAction)
        {
            if (_child != null)
                throw new InvalidOperationException("Child has been set.");

            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new SelectorCompositeBuilder<TContext>();
            buildAction(builder);

            _child = builder.Build();
        }

        public void Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction)
        {
            if (_child != null)
                throw new InvalidOperationException("Child has been set.");
                
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new SequenceCompositeBuilder<TContext>();
            buildAction(builder);

            _child = builder.Build();
        }

        protected abstract INode<TContext> CreateDecorator(TLogic logic, INode<TContext> child);

        public INode<TContext> Build()
        {
            if (_logic == null)
                throw new InvalidOperationException("Decorator logic cannot be null.");
            if (_child == null)
                throw new InvalidOperationException("Decorator must have a child note.");

            return CreateDecorator(_logic, _child);
        }
    }
}