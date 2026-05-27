namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// Optional interface a ranged NPC can expose so its opponent's BT can ask
    /// questions like "is the enemy currently reloading?" without taking a hard
    /// dependency on a specific Runner class.
    ///
    /// <para>Implemented by <see cref="BehaviorTree.ClassFirst.MarksmanRunner"/>.</para>
    /// </summary>
    public interface IRangedAmmo
    {
        int Ammo { get; }
        int MaxAmmo { get; }
        bool IsReloading { get; }
    }
}
