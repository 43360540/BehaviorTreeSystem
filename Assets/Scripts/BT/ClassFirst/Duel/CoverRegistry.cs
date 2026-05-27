using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// Process-wide registry of every active <see cref="CoverPoint"/>. Lets BT
    /// actions ask "find me the best cover from this threat without walking the
    /// scene every tick".
    ///
    /// <para>Auto-populates via CoverPoint's OnEnable/OnDisable. Reset via
    /// <c>RuntimeInitializeOnLoadMethod</c> so PlayMode restarts start clean.</para>
    /// </summary>
    public static class CoverRegistry
    {
        private static readonly List<CoverPoint> _all = new List<CoverPoint>(64);

        public static IReadOnlyList<CoverPoint> All => _all;

        public static void Register(CoverPoint cp)
        {
            if (cp != null && !_all.Contains(cp)) _all.Add(cp);
        }

        public static void Unregister(CoverPoint cp)
        {
            _all.Remove(cp);
        }

        /// <summary>
        /// Find the best cover within <paramref name="maxSearchRadius"/> of
        /// <paramref name="seekerPos"/> that protects from a threat at
        /// <paramref name="threatPos"/>.
        ///
        /// <para>Scoring rewards (a) closeness to seeker (so the NPC doesn't run
        /// across the map) and (b) good alignment of the cover's protection arc
        /// against the threat direction. Reservation/occupancy isn't tracked yet
        /// — single duel demo, low contention.</para>
        /// </summary>
        public static CoverPoint FindBestCover(Vector3 seekerPos, Vector3 threatPos, float maxSearchRadius)
        {
            CoverPoint best = null;
            float bestScore = float.NegativeInfinity;
            float maxSqr = maxSearchRadius * maxSearchRadius;

            for (int i = 0; i < _all.Count; i++)
            {
                var cp = _all[i];
                if (cp == null) continue;

                Vector3 standPos = cp.StandPosition;
                float distSqr = (standPos - seekerPos).sqrMagnitude;
                if (distSqr > maxSqr) continue;

                if (!cp.ProtectsFrom(threatPos)) continue;

                // Closer = better. Subtract distance so closer cover wins; multiply
                // by a small factor so distance dominates over angular alignment
                // (which we already gated via ProtectsFrom).
                float dist = Mathf.Sqrt(distSqr);
                float score = -dist;

                // Mild penalty if the cover is between the seeker and the threat
                // in a way that forces the seeker to RUN THROUGH the line of fire
                // (i.e. seeker is on the threat side of cover already).
                Vector3 coverToThreat = (threatPos - standPos).normalized;
                Vector3 coverToSeeker = (seekerPos - standPos).normalized;
                if (Vector3.Dot(coverToThreat, coverToSeeker) > 0.5f)
                    score -= 4f; // seeker is on the wrong side — discouraged

                if (score > bestScore)
                {
                    bestScore = score;
                    best = cp;
                }
            }
            return best;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => _all.Clear();
    }
}
