using System;

namespace BehaviorTree
{
    public class GuardDecorator : NodeBase, ISelectable
    {
        private readonly Func<bool> _condition = null;
        private readonly INode _child = null;

        public GuardDecorator(Func<bool> condition, INode child)
        {
            _child = child ?? throw new ArgumentNullException(nameof(child));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool IsSelectable()
        {
            return _condition.Invoke();
        }

        protected override NodeStatus OnTick(BlackBoardMono bb, float dt)
        {
            if (!_condition.Invoke())
            {
                _child.Abort(bb);
                return NodeStatus.Failure;
            }
            return _child.Tick(bb, dt);
        }

        protected override void OnAbort(BlackBoardMono bb)
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
