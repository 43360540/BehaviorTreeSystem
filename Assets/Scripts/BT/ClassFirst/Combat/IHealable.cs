namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Anything that a Healer's beam can top up. Implemented by BaseNPCRunner.
    /// Separate from IDamageable so older StateStyle NPCs don't have to add a no-op Heal().
    /// </summary>
    public interface IHealable
    {
        void Heal(float amount, IDamageable source);
    }
}
