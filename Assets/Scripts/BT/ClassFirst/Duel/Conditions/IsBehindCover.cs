using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel.Conditions
{
    /// <summary>
    /// Is the NPC currently standing close to a CoverPoint that protects from
    /// the current target's position?
    ///
    /// <para>Uses <see cref="CoverRegistry"/> to scan instead of the cached
    /// Runner cover, so this works even if the NPC drifted to cover without
    /// going through a FindCover / MoveToCover sequence.</para>
    /// </summary>
    public static class IsBehindCover
    {
        public static bool Check(BTContext ctx, float maxDistance = 1.2f)
        {
            if (!ctx.HasTarget) return false;
            var all = CoverRegistry.All;
            Vector3 self = ctx.Self.position;
            Vector3 threat = ctx.Target.Transform.position;
            float maxSqr = maxDistance * maxDistance;

            for (int i = 0; i < all.Count; i++)
            {
                var cp = all[i];
                if (cp == null) continue;
                if ((cp.StandPosition - self).sqrMagnitude > maxSqr) continue;
                if (cp.ProtectsFrom(threat)) return true;
            }
            return false;
        }
    }
}
