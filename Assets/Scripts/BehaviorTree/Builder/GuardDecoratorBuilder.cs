using System;

namespace BehaviorTree
{
    public sealed class GuardDecoratorBuilder<TContext> :  DecoratorBuilderBase<ICondition<TContext>, TContext>
    {
        public GuardDecoratorBuilder(ICondition<TContext> condition) : base(condition){}

        protected override INode<TContext> CreateDecorator(ICondition<TContext> logic, INode<TContext> child) =>
            new GuardDecorator<TContext>(logic, child);
    }
}