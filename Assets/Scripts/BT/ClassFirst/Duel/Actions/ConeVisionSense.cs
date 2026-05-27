using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel.Actions
{
    /// <summary>
    /// Duel-only replacement for <c>Sense</c>. Uses an OverlapSphere as the
    /// candidate pool but then filters with (a) a forward-cone FOV check and
    /// (b) a LOS raycast — so NPCs can't see through walls and can't see what's
    /// behind them.
    ///
    /// <para>Side effects on each tick:</para>
    /// <list type="bullet">
    /// <item>If a visible enemy is found → <c>ctx.Target</c> = enemy +
    ///       <c>PerceptionState.LastKnownPos</c> / <c>LastKnownDir</c> /
    ///       <c>LastSeenTime</c> are updated. Returns Success.</item>
    /// <item>If no visible enemy → <c>ctx.Target</c> = null. <c>LastKnownPos</c>
    ///       is NOT cleared (per Sean's Q2 = persistent memory). Returns Failure.</item>
    /// </list>
    ///
    /// <para>Designed to be ticked EVERY frame (no Throttle): visibility can
    /// change between any two frames as the opponent moves behind cover or
    /// the NPC turns its head. With only 2 NPCs the cost is trivial.</para>
    /// </summary>
    public sealed class ConeVisionSense : ActionBase<BTContext>
    {
        private readonly IPerceptionHolder _perception;
        private readonly float _fovDeg;
        private readonly float _viewRange;
        private readonly float _proximityRange;
        private readonly float _eyeY;
        private readonly float _fovDotThreshold;
        private readonly float _proximitySqr;

        public ConeVisionSense(
            IPerceptionHolder perception,
            float fovDeg = 100f,
            float viewRange = 25f,
            float eyeY = 1.2f,
            float proximityRange = 3f)
        {
            _perception = perception;
            _fovDeg     = Mathf.Clamp(fovDeg, 10f, 360f);
            _viewRange  = Mathf.Max(0.1f, viewRange);
            _proximityRange = Mathf.Max(0f, proximityRange);
            _eyeY       = eyeY;
            _fovDotThreshold = Mathf.Cos(_fovDeg * 0.5f * Mathf.Deg2Rad);
            _proximitySqr = _proximityRange * _proximityRange;
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            int hit = Physics.OverlapSphereNonAlloc(
                ctx.Self.position, _viewRange, ctx.OverlapBuffer, ctx.SensorLayer);

            IDamageable best = null;
            float bestSqr = float.PositiveInfinity;

            Vector3 selfPos = ctx.Self.position;
            Vector3 eyePos  = selfPos + Vector3.up * _eyeY;
            Vector3 forwardFlat = ctx.Self.forward;
            forwardFlat.y = 0f;
            if (forwardFlat.sqrMagnitude < 1e-4f) forwardFlat = Vector3.forward;
            forwardFlat.Normalize();

            for (int i = 0; i < hit; i++)
            {
                var d = ctx.OverlapBuffer[i].GetComponentInParent<IDamageable>();
                if (d == null) continue;
                if (!d.IsAlive) continue;
                if (ReferenceEquals(d.Transform, ctx.Self)) continue;
                if (d.Faction == ctx.Faction) continue;

                Vector3 candPos = d.Transform.position;
                Vector3 toCand = candPos - selfPos;
                toCand.y = 0f;
                float sqr = toCand.sqrMagnitude;
                if (sqr < 1e-4f) continue;

                // Cone FOV check, with a proximity fallback for "I can feel
                // them right behind me" — peripheral vision + hearing of nearby
                // movement. If outside the cone but within proximityRange, still
                // accept (subject to the LOS check below — pillar between still blocks).
                Vector3 toCandNorm = toCand / Mathf.Sqrt(sqr);
                bool inCone = Vector3.Dot(toCandNorm, forwardFlat) >= _fovDotThreshold;
                bool inProximity = sqr <= _proximitySqr;
                if (!inCone && !inProximity) continue;

                // LOS raycast — eye to chest. Blocked by obstacles.
                Vector3 candEye = candPos + Vector3.up * _eyeY;
                Vector3 lineDir = candEye - eyePos;
                float lineDist = lineDir.magnitude;
                if (lineDist > 0.1f)
                {
                    lineDir /= lineDist;
                    if (Physics.Raycast(eyePos, lineDir, out RaycastHit los, lineDist + 0.1f))
                    {
                        var losDamageable = los.collider.GetComponentInParent<IDamageable>();
                        if (!ReferenceEquals(losDamageable, d)) continue; // wall in the way
                    }
                }

                if (sqr < bestSqr) { bestSqr = sqr; best = d; }
            }

            var ps = _perception?.PerceptionState;
            if (best != null)
            {
                ctx.Target = best;
                if (ps != null)
                {
                    Vector3 bp = best.Transform.position;
                    ps.LastKnownPos = bp;
                    Vector3 toBest = bp - selfPos; toBest.y = 0f;
                    if (toBest.sqrMagnitude > 1e-4f)
                        ps.LastKnownDir = toBest.normalized;
                    ps.LastSeenTime = Time.time;
                }
                return NodeStatus.Success;
            }

            // No visible target this tick. Drop ctx.Target (was true → false on
            // visibility loss). LastKnownPos stays — persistent memory.
            ctx.Target = null;
            return NodeStatus.Failure;
        }
    }
}
