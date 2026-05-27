namespace BehaviorTree.ClassFirst.Duel.Conditions
{
    /// <summary>
    /// Convenience predicate over an <see cref="IRangedAmmo"/> source — usually
    /// the MarksmanRunner itself. Threshold defaults to 30% capacity.
    /// </summary>
    public static class IsAmmoLow
    {
        public static bool Check(IRangedAmmo source, float lowRatio = 0.3f)
        {
            if (source == null || source.MaxAmmo <= 0) return false;
            return (float)source.Ammo / source.MaxAmmo <= lowRatio;
        }

        public static bool IsEmpty(IRangedAmmo source)
            => source != null && source.Ammo <= 0;
    }
}
