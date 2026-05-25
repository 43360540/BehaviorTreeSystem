namespace BehaviorTree.ClassFirst.Conditions
{
    /// <summary>
    /// True when context has a live target (set by Sense).
    /// </summary>
    public sealed class HasTarget : ICondition<BTContext>
    {
        public bool Evaluate(BTContext ctx, float dt) => ctx.HasTarget;
    }
}
