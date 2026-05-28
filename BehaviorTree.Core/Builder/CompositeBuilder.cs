using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public class CompositeBuilder<TContext> : IMultiChildren<CompositeBuilder<TContext>, TContext>
    {
        private readonly List<INode<TContext>> _children = [];
        private readonly string? _name;

        public CompositeBuilder(string? name) => _name = name;

        public CompositeBuilder<TContext> Add(INode<TContext> node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            _children.Add(node);
            return this;
        }

        public INode<TContext> Build(Func<INode<TContext>[], string?, INode<TContext>> factory)
        {
            if (_children.Count <= 0)
                throw new InvalidOperationException("Composite must have at least ONE child.");

            return factory([.. _children], _name);
        }
    }
}
