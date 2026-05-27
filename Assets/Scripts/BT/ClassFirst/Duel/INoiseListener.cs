using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// Implemented by NPCs that should be alerted by world-space noise events
    /// (gunshots, footsteps, explosions). Audible-range filtering happens in
    /// <see cref="NoiseBus"/> before <see cref="OnNoise"/> is invoked, so the
    /// implementation just has to react.
    /// </summary>
    public interface INoiseListener
    {
        /// <summary>World position of the noise source.</summary>
        Vector3 ListenerPosition { get; }

        /// <summary>
        /// Called by <see cref="NoiseBus"/> when a noise is emitted within
        /// audible range of this listener.
        /// </summary>
        /// <param name="sourcePos">World position the noise came from.</param>
        /// <param name="intensity">0..1 normalised loudness (1 = at-source).</param>
        void OnNoise(Vector3 sourcePos, float intensity);
    }
}
