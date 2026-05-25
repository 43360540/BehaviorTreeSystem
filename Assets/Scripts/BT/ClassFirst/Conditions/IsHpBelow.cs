namespace BehaviorTree.ClassFirst.Conditions
{
    /// <summary>
    /// True when current HP ratio is below threshold (0..1).
    /// </summary>
    public sealed class IsHpBelow : ICondition<BTContext>
    {
        private readonly float _ratio;

        public IsHpBelow(float ratio) => _ratio = ratio;

        public bool Evaluate(BTContext ctx, float dt) => ctx.HpRatio < _ratio;
    }
}
