using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// Per-NPC perception memory for the duel demo. Lives on the Runner (so it
    /// survives between BT ticks) and is updated by <c>ConeVisionSense</c>.
    ///
    /// <para>Semantics:</para>
    /// <list type="bullet">
    /// <item><b>LastKnownPos</b>: null means "never visually confirmed". Otherwise
    /// holds the position the enemy was at on the most recent vision contact.
    /// Per Sean's Q2: persistent — never decays until a new sighting updates it.</item>
    /// <item><b>LastKnownDir</b>: direction-only hint, initialized at spawn from
    /// the rough enemy spawn area (per Sean's Q1=b: "know the direction, not the
    /// position"). Always set even before first contact.</item>
    /// <item><b>LastSeenTime</b>: Time.time of the last contact. 0 = never seen.</item>
    /// <item><b>HasContact</b>: shorthand for LastKnownPos != null.</item>
    /// </list>
    /// </summary>
    public sealed class PerceptionState
    {
        public Vector3? LastKnownPos;
        public Vector3 LastKnownDir = Vector3.forward;
        public float LastSeenTime;
        public bool HasContact => LastKnownPos.HasValue;
    }
}
