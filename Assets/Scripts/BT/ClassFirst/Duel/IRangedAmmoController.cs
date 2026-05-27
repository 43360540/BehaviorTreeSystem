namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// Owner-side counterpart of <see cref="IRangedAmmo"/> — exposes the mutating
    /// operations a Reload action needs, while the read-only IRangedAmmo can be
    /// safely handed to opposing NPCs (so their BT can check "is target reloading").
    /// </summary>
    public interface IRangedAmmoController : IRangedAmmo
    {
        /// <summary>
        /// Begin a reload cycle. Pull ammo to zero immediately if you want
        /// reloading-while-not-empty to feel "tactical".
        /// </summary>
        void StartReload();

        /// <summary>
        /// Advance the reload timer. Called by the Reload action every Tick
        /// with the frame's deltaTime.
        /// </summary>
        void TickReload(float dt);

        /// <summary>Consume one shot. Returns false when out of ammo.</summary>
        bool ConsumeShot();
    }
}
