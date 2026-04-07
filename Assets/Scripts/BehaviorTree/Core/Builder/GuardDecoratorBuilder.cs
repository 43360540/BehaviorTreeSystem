using System;

namespace BehaviorTree
{
    public sealed class GuardDecoratorBuilder<TContext> :  DecoratorBuilderBase<ICondition<TContext>, TContext>
    {
        public GuardDecoratorBuilder(ICondition<TContext> condition) : base(condition){}
        public GuardDecoratorBuilder(Func<TContext, float, bool> predicate) : base(new QuickCondition<TContext>(predicate)){}
        public GuardDecoratorBuilder(Func<float, bool> predicate) : base(new QuickCondition<TContext>(predicate)){}
        public GuardDecoratorBuilder(Func<bool> predicate) : base(new QuickCondition<TContext>(predicate)){}

        protected override INode<TContext> CreateDecorator(ICondition<TContext> logic, INode<TContext> child) =>
            new GuardDecorator<TContext>(logic, child);
    }
}