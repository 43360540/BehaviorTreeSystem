using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// Marks a position in the arena as a usable cover spot for the duel demo.
    ///
    /// <para>Each cover has a <see cref="SafeDirection"/> — the direction the
    /// protecting wall faces "out" toward, i.e. the side the threat is expected
    /// to attack from. For a wall running north-south facing east, SafeDirection
    /// = east, and standing on the cover's west side gives protection.</para>
    ///
    /// <para><see cref="ProtectionArcDeg"/> describes how wide the protected arc
    /// is around <see cref="SafeDirection"/>. A pillar with omnidirectional
    /// protection uses 360°; a flat wall uses ~120°.</para>
    ///
    /// <para>Auto-registers with <see cref="CoverRegistry"/> on enable / disable.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoverPoint : MonoBehaviour
    {
        [Tooltip("Outward normal of the protecting wall. The 'safe' side is OPPOSITE this direction.")]
        public Vector3 SafeDirection = Vector3.forward;

        [Tooltip("How wide the protected arc is, in degrees around SafeDirection. 360 = omnidirectional (pillar).")]
        [Range(30f, 360f)]
        public float ProtectionArcDeg = 120f;

        [Tooltip("Radius of the actual 'crouch here' footprint. Used to validate the NPC can stand here.")]
        public float Radius = 0.6f;

        /// <summary>
        /// World position the NPC should stand at when using this cover.
        /// Default = transform.position; override for asymmetric cover.
        /// </summary>
        public Vector3 StandPosition => transform.position;

        /// <summary>
        /// Returns true if this cover would protect a defender standing here
        /// from a threat at <paramref name="threatPos"/>. That requires the
        /// threat to lie within the protected arc relative to SafeDirection.
        /// </summary>
        public bool ProtectsFrom(Vector3 threatPos)
        {
            Vector3 toThreat = threatPos - transform.position;
            toThreat.y = 0f;
            if (toThreat.sqrMagnitude < 1e-4f) return false;
            toThreat.Normalize();

            Vector3 safe = SafeDirection;
            safe.y = 0f;
            if (safe.sqrMagnitude < 1e-4f) return false;
            safe.Normalize();

            float dot = Vector3.Dot(toThreat, safe);
            float minDot = Mathf.Cos(ProtectionArcDeg * 0.5f * Mathf.Deg2Rad);
            return dot >= minDot;
        }

        private void OnEnable()  => CoverRegistry.Register(this);
        private void OnDisable() => CoverRegistry.Unregister(this);

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, Radius);

            // Draw a wedge indicating protected arc (drawn opposite SafeDirection
            // because the NPC stands behind SafeDirection).
            Vector3 safe = SafeDirection.sqrMagnitude > 1e-4f ? SafeDirection.normalized : Vector3.forward;
            int seg = 16;
            float halfArc = ProtectionArcDeg * 0.5f;
            Vector3 origin = transform.position;
            Vector3 prev = origin + Quaternion.Euler(0, -halfArc, 0) * safe * 1.5f;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            for (int i = 1; i <= seg; i++)
            {
                float ang = -halfArc + (ProtectionArcDeg * i) / seg;
                Vector3 cur = origin + Quaternion.Euler(0, ang, 0) * safe * 1.5f;
                Gizmos.DrawLine(prev, cur);
                Gizmos.DrawLine(origin, cur);
                prev = cur;
            }
        }
    }
}
