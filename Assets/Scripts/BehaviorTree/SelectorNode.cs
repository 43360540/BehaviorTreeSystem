namespace BehaviorTree
{
    public sealed class SelectorNode : CompositeBase
    {
        private INode _activeChild = null;

        public SelectorNode(params INode[] children) : base(children) { }

        protected override NodeStatus OnTick(BlackBoardMono bb, float dt)
        {
            var prev = _activeChild;
            _activeChild = null;

            foreach (var c in Children)
            {
                // Is child a ICondition
                if (c is ISelectable con)
                {
                    // Evaluate whether child can enter
                    if (con.IsSelectable())
                        _activeChild = c;
                    else
                        continue;
                }
                else
                    _activeChild = c;

                if (prev != null && prev != _activeChild)
                {
                    prev.Abort(bb);
                }

                var status = _activeChild.Tick(bb, dt);

                return status;
            }

            return NodeStatus.Failure;
        }

        protected override void OnStop(BlackBoardMono bb)
        {
            base.OnStop(bb);

            _activeChild = null;
        }

        protected override void OnReset()
        {
            base.OnReset();

            _activeChild = null;
        }

        protected override void OnAbort(BlackBoardMono bb)
        {
            base.OnAbort(bb);

            _activeChild?.Abort(bb);
            _activeChild = null;
        }
    }
}