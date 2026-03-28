using System;

namespace BehaviorTree
{
    public abstract class DecoratorBuilderBase<TLogic, TContext> where TLogic : class
    {
        private INode<TContext> _child;
        private readonly TLogic _logic;

        public DecoratorBuilderBase(TLogic logic)
        {
            _logic = logic ?? throw new ArgumentNullException(nameof(logic));
        }

        public void Action(BTAction<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Node(new ActionLeaf<TContext>(action));
        }

        public void Action(ActionBundle<TContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Node(new ActionLeaf<TContext>(action));
        }

        public void Selector(Action<SelectorCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new SelectorCompositeBuilder<TContext>();
            buildAction(builder);

            Node(builder.Build());
        }

        public void Sequence(Action<SequenceCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = new SequenceCompositeBuilder<TContext>();
            buildAction(builder);

            Node(builder.Build());
        }

        public void Parallel(Action<ParallelCompositeBuilder<TContext>> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));


            var builder = new ParallelCompositeBuilder<TContext>();
            buildAction(builder);

            Node(builder.Build());
        }

        public void Node(INode<TContext> node)
        {
            if (_child != null)
                throw new InvalidOperationException("Child has been set.");

            _child = node ?? throw new ArgumentNullException(nameof(node));
        }

        protected abstract INode<TContext> CreateDecorator(TLogic logic, INode<TContext> child);

        public INode<TContext> Build()
        {
            if (_child == null)
                throw new InvalidOperationException("Decorator must have a child node.");

            return CreateDecorator(_logic, _child);
        }
    }
}