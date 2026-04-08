using System;

namespace BehaviorTree
{
    public sealed class GuardDecoratorBuilder<TContext> :  DecoratorBuilderBase<ICondition<TContext>, TContext>
    {
        public GuardDecoratorBuilder(ICondition<TContext> condition, string name = null) : base(condition, name){}
        public GuardDecoratorBuilder(Func<TContext, float, bool> predicate, string name = null) : base(new QuickCondition<TContext>(predicate), name){}
        public GuardDecoratorBuilder(Func<float, bool> predicate, string name = null) : base(new QuickCondition<TContext>(predicate), name){}
        public GuardDecoratorBuilder(Func<bool> predicate, string name = null) : base(new QuickCondition<TContext>(predicate), name){}

        protected override INode<TContext> CreateDecorator(ICondition<TContext> logic, INode<TContext> child, string name = null) =>
            new GuardDecorator<TContext>(logic, child, name);
    }
}