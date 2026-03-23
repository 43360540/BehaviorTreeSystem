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

        public bool CanEnter(TContext bb, float dt)
        {
            return _condition.Evaluate(bb, dt);
        }

        protected override NodeStatus OnTick(TContext bb, float dt)
        {
            if (!_condition.Evaluate(bb, dt))
            {
                _child.Abort(bb);
                return NodeStatus.Failure;
            }
            return _child.Tick(bb, dt);
        }

        protected override void OnAbort(TContext bb)
        {
            base.OnAbort(bb);
            _child.Abort(bb);
        }

        protected override void OnReset()
        {
            base.OnReset();
            _child.Reset();
        }
    }
}
