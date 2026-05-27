namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// Implemented by NPC Runners that participate in the cover system. The
    /// "current cover" is the result of the most recent FindCover; MoveToCover
    /// and IsBehindCover read it.
    ///
    /// <para>This avoids stuffing duel-specific fields into the shared BTContext
    /// while still letting BT Actions reach the per-NPC cover slot via ctor
    /// injection (Runner passes `this` when constructing the action).</para>
    /// </summary>
    public interface ICoverHolder
    {
        CoverPoint CurrentCover { get; set; }
    }
}
