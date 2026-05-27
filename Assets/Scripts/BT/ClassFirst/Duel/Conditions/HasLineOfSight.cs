using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel.Conditions
{
    /// <summary>
    /// Stateless condition: raycast from self (eye-height) to target (chest-height)
    /// and report whether the first hit IS the target. Anything in between
    /// (walls, pillars) breaks LOS.
    ///
    /// <para>Made a regular class (not <see cref="System.Func{T1,T2,bool}"/>) so
    /// the predicate can be parameterised (eye/chest offsets, max range)
    /// without allocating closures.</para>
    /// </summary>
    public static class HasLineOfSight
    {
        /// <summary>
        /// Default predicate suitable for plugging into <c>.When((ctx, _) =&gt; HasLineOfSight.Check(ctx))</c>.
        /// Eye / chest height = 1.2 m; matches what ArcherShoot uses.
        /// </summary>
        public static bool Check(BTContext ctx, float eyeY = 1.2f, float chestY = 1.2f, float maxRange = 60f)
        {
            if (!ctx.HasTarget) return false;
            Vector3 origin = ctx.Self.position + Vector3.up * eyeY;
            Vector3 to     = ctx.Target.Transform.position + Vector3.up * chestY;
            Vector3 dir    = to - origin;
            float dist     = dir.magnitude;
            if (dist < 0.05f) return true;          // adjacent
            if (dist > maxRange) return false;      // out of LOS-budget range
            dir /= dist;

            if (!Physics.Raycast(origin, dir, out RaycastHit hit, dist + 0.1f))
                return true;                        // nothing in the way

            var hitDamageable = hit.collider.GetComponentInParent<IDamageable>();
            return ReferenceEquals(hitDamageable, ctx.Target);
        }
    }
}
