namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Returns Running for <see cref="_seconds"/> seconds, then Success.
    /// Each construction instance has its own timer — don't share one instance
    /// across multiple positions in a tree.
    /// </summary>
    public sealed class WaitTimer : ActionBase<BTContext>
    {
        private readonly float _seconds;
        private float _elapsed;

        public WaitTimer(float seconds) => _seconds = seconds;

        public override void Start(BTContext ctx) => _elapsed = 0f;

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            _elapsed += dt;
            return _elapsed >= _seconds ? NodeStatus.Success : NodeStatus.Running;
        }

        public override void Reset() => _elapsed = 0f;
    }
}
