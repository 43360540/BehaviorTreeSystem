namespace BehaviorTree
{
    // Reactive Selector
    public sealed class SelectorComposite<TContext> : CompositeBase<TContext>
    {
        private INode<TContext> _activeChild = null;

        public SelectorComposite(params INode<TContext>[] children) : base(children) { }

        protected override NodeStatus OnTick(TContext bb, float dt)
        {
            var prev = _activeChild;
            _activeChild = null;

            foreach (var c in Children)
            {
                // Is child a ICondition
                if (c is IGuard<TContext> guard)
                {
                    // Evaluate whether child can enter
                    if (guard.CanEnter(bb, dt))
                        _activeChild = c;
                    else
                        continue;
                }
                else
                    _activeChild = c;

        // ------------------------------------------------------------------

                if (prev != null && prev != _activeChild)
                {
                    prev.Abort(bb);
                    prev = null;
                }

                var status = _activeChild.Tick(bb, dt);

                if (status == NodeStatus.Failure)
                    continue;

                return status;
            }
            prev?.Abort(bb);
            return NodeStatus.Failure;
        }

        protected override void OnAbort(TContext bb)
        {
            base.OnAbort(bb);

            _activeChild?.Abort(bb);
        }

        protected override void OnReset()
        {
            base.OnReset();

            _activeChild = null;
        }
    }
}