using System;

namespace BehaviorTree
{
    public class DecoratorBuilder<TLogic, TContext> : ISingleChild<TContext> where TLogic : class
    {
        private INode<TContext>? _child;
        private readonly TLogic? _logic;
        private readonly string? _name;

        public DecoratorBuilder(TLogic? logic, string? name = null)
        {
            _logic = logic;
            _name = name;
        }

        public void Set(INode<TContext> node)
        {
            if (_child != null)
                throw new InvalidOperationException("Child has been set.");

            _child = node ?? throw new ArgumentNullException(nameof(node));
        }

        public INode<TContext> Build(Func<TLogic?, INode<TContext>, string?, INode<TContext>> factory)
        {
            if (_child == null)
                throw new InvalidOperationException("Decorator must have One child.");

            return factory(_logic, _child, _name);
        }
    }
}