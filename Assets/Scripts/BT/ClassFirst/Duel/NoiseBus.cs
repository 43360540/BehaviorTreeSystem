using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// Process-wide pub-sub for audible events. NPCs register on enable, the
    /// shooter emits a noise on fire, the bus delivers it to every listener
    /// within range.
    ///
    /// <para>Currently only gunshots are emitted. Footsteps could plug in
    /// here later if needed (Q4 polish item — explicitly deferred).</para>
    ///
    /// <para>The emitter is filtered out of the dispatch — a Marksman doesn't
    /// alert ITSELF every time it fires.</para>
    /// </summary>
    public static class NoiseBus
    {
        public const float GUNSHOT_AUDIBLE_RANGE = 25f;

        private static readonly List<INoiseListener> _listeners = new List<INoiseListener>(8);

        public static void Register(INoiseListener l)
        {
            if (l != null && !_listeners.Contains(l)) _listeners.Add(l);
        }

        public static void Unregister(INoiseListener l)
        {
            _listeners.Remove(l);
        }

        /// <summary>
        /// Emit a gunshot at <paramref name="sourcePos"/>. Listeners within
        /// <paramref name="audibleRange"/> get <see cref="INoiseListener.OnNoise"/>
        /// with intensity = (1 - distance/range), so a far shot is quieter than
        /// a near one (currently consumers ignore intensity; future damping uses it).
        /// </summary>
        public static void EmitGunshot(Vector3 sourcePos, INoiseListener emitter = null, float audibleRange = GUNSHOT_AUDIBLE_RANGE)
        {
            float rangeSqr = audibleRange * audibleRange;
            for (int i = 0; i < _listeners.Count; i++)
            {
                var l = _listeners[i];
                if (l == null) continue;
                if (ReferenceEquals(l, emitter)) continue; // don't notify self

                float distSqr = (l.ListenerPosition - sourcePos).sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                float intensity = 1f - Mathf.Sqrt(distSqr) / audibleRange;
                l.OnNoise(sourcePos, intensity);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => _listeners.Clear();
    }
}
