using System;

namespace BehaviorTree
{
    public class GuardDecorator<TContext> : NodeBase<TContext>, IGuard<TContext>
    {
        private readonly ICondition<TContext> _condition = null;
        private readonly INode<TContext> _child = null;

        public GuardDecorator(Func<TContext, float, bool> condition, INode<TContext> child)
        {
            _child = child ?? throw new ArgumentNullException(nameof(child));
            if (condition == null) 
                throw new ArgumentNullException(nameof(condition));
            _condition = new DelegateCondition<TContext>(condition);
            
        }

        public GuardDecorator(ICondition<TContext> condition, INode<TContext> child)
        {
            _child = child ?? throw new ArgumentNullException(nameof(child));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool CanEnter(TContext ctx, float dt)
        {
            return _condition.Evaluate(ctx, dt);
        }

        protected override NodeStatus OnTick(TContext ctx, float dt)
        {
            if (!_condition.Evaluate(ctx, dt))
            {
                _child.Abort(ctx);
                return NodeStatus.Failure;
            }
            return _child.Tick(ctx, dt);
        }

        protected override void OnAbort(TContext ctx)
        {
            base.OnAbort(ctx);
            _child.Abort(ctx);
        }

        protected override void OnReset()
        {
            base.OnReset();
            _child.Reset();
        }
    }
}
