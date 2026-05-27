namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// Implemented by Runners that own a <see cref="PerceptionState"/>.
    /// BT actions take this interface in their constructor so they can read /
    /// write perception memory without poking into class-specific Runner types.
    /// </summary>
    public interface IPerceptionHolder
    {
        PerceptionState PerceptionState { get; }
    }
}
