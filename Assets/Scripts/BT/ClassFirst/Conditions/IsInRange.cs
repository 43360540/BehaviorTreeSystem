namespace BehaviorTree.ClassFirst.Conditions
{
    /// <summary>
    /// True when target is within the supplied range.
    /// Pass a fixed value or null to fall back to ctx.AttackRange.
    /// </summary>
    public sealed class IsInRange : ICondition<BTContext>
    {
        private readonly float? _range;

        public IsInRange(float range) => _range = range;
        public IsInRange() => _range = null;

        public bool Evaluate(BTContext ctx, float dt)
        {
            if (!ctx.HasTarget) return false;
            float r = _range ?? ctx.AttackRange;
            return ctx.TargetDistance <= r;
        }
    }
}
