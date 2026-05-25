namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Combat allegiance — same-faction NPCs don't target each other.
    /// Two-team setup; used by both the small Sample demos (3v3) and the
    /// large War demo (100v100). Name kept as "Faction" so older code that
    /// references it doesn't need refactoring.
    /// </summary>
    public enum Faction
    {
        TeamA,
        TeamB,
    }
}
