namespace BehaviorTree.ClassFirst.Duel.Conditions
{
    /// <summary>
    /// Check whether the current target exposes <see cref="IRangedAmmo"/> and is
    /// currently reloading. Returns false silently if the target isn't ranged
    /// — non-ranged opponents don't have a "reloading" window.
    /// </summary>
    public static class IsTargetReloading
    {
        public static bool Check(BTContext ctx)
        {
            if (!ctx.HasTarget) return false;
            // Target.Transform.gameObject is the cheapest way to reach the
            // MonoBehaviour-side ammo component without polluting IDamageable.
            var ammo = ctx.Target.Transform.GetComponentInParent<IRangedAmmo>();
            return ammo != null && ammo.IsReloading;
        }
    }
}
