using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Duel.Actions
{
    /// <summary>
    /// Cover-to-cover advance toward a "search target" (visible target if any,
    /// otherwise LastKnownPos, otherwise LastKnownDir hint). Picks a CoverPoint
    /// that is (a) closer to the search target than where we are now, (b) within
    /// <paramref name="_maxStep"/> of the NPC's current position so we don't
    /// teleport across the map, and (c) ideally protects from the search
    /// direction.
    ///
    /// <para>Falls back to a straight step toward the search target if no
    /// suitable cover is found.</para>
    ///
    /// <para>Returns Success when arrived, Running while pathing, Failure if
    /// the agent is off-NavMesh or path invalid.</para>
    /// </summary>
    public sealed class AdvanceToNextCover : ActionBase<BTContext>
    {
        private readonly IPerceptionHolder _perception;
        private readonly ICoverHolder _coverHolder;
        private readonly float _maxStep;
        private readonly float _arriveTolerance;
        private readonly float _minStep;
        private Vector3 _destSnapshot;
        private bool _hasDest;

        public AdvanceToNextCover(
            IPerceptionHolder perception, ICoverHolder coverHolder,
            float maxStep = 10f, float arriveTolerance = 0.8f, float minStep = 1.5f)
        {
            _perception = perception;
            _coverHolder = coverHolder;
            _maxStep = maxStep;
            _arriveTolerance = arriveTolerance;
            _minStep = minStep;
        }

        public override void Start(BTContext ctx)
        {
            _hasDest = false;
            Vector3 selfPos = ctx.Self.position;
            Vector3 searchPos = ComputeSearchPos(ctx);

            CoverPoint best = PickNextCover(selfPos, searchPos);
            if (best != null)
            {
                _destSnapshot = best.StandPosition;
                if (_coverHolder != null) _coverHolder.CurrentCover = best;
            }
            else
            {
                // No suitable cover — step toward search target along ground plane.
                Vector3 dir = searchPos - selfPos; dir.y = 0f;
                if (dir.sqrMagnitude > 1e-4f)
                    _destSnapshot = selfPos + dir.normalized * _maxStep;
                else
                    _destSnapshot = selfPos;
                if (_coverHolder != null) _coverHolder.CurrentCover = null;
            }

            // Snap onto NavMesh so SetDestination doesn't fail because of a
            // slightly off-mesh point (e.g. cover sat 1cm above the plane).
            if (NavMesh.SamplePosition(_destSnapshot, out NavMeshHit nh, 2f, NavMesh.AllAreas))
                _destSnapshot = nh.position;

            if (IsAgentReady(ctx))
            {
                ctx.Agent.speed = ctx.WalkSpeed;
                ctx.Agent.isStopped = false;
                if (ctx.Agent.SetDestination(_destSnapshot)) _hasDest = true;
            }
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (!IsAgentReady(ctx)) return NodeStatus.Failure;
            if (!_hasDest)
            {
                if (!ctx.Agent.SetDestination(_destSnapshot)) return NodeStatus.Failure;
                _hasDest = true;
            }
            if (ctx.Agent.pathPending) return NodeStatus.Running;
            if (ctx.Agent.pathStatus == NavMeshPathStatus.PathInvalid) return NodeStatus.Failure;

            // XZ-only distance. CoverPoint GameObjects sit at y=0; NavMeshAgent
            // bodies sit at y = baseOffset (≈1m above ground). A 3D check would
            // permanently report ≥1m distance and Success would never fire,
            // freezing the sequence in this action — that was the V2 "stuck at
            // cover" bug found during the first play.
            Vector3 d = ctx.Self.position - _destSnapshot; d.y = 0f;
            return d.magnitude <= _arriveTolerance ? NodeStatus.Success : NodeStatus.Running;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (IsAgentReady(ctx)) ctx.Agent.ResetPath();
        }

        private Vector3 ComputeSearchPos(BTContext ctx)
        {
            // Priority: visible target > last-known position > last-known direction projection.
            if (ctx.HasTarget) return ctx.Target.Transform.position;
            var ps = _perception?.PerceptionState;
            if (ps != null && ps.LastKnownPos.HasValue) return ps.LastKnownPos.Value;
            if (ps != null) return ctx.Self.position + ps.LastKnownDir * 15f;
            return ctx.Self.position + ctx.Self.forward * 5f;
        }

        private CoverPoint PickNextCover(Vector3 selfPos, Vector3 searchPos)
        {
            float currentDistToSearch = Vector3.Distance(selfPos, searchPos);
            CoverPoint best = null;
            float bestScore = float.NegativeInfinity;

            var all = CoverRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var cp = all[i];
                if (cp == null) continue;

                Vector3 stand = cp.StandPosition;
                float coverToSearch = Vector3.Distance(stand, searchPos);
                // Must make forward progress (be closer to search than we are now).
                if (coverToSearch >= currentDistToSearch - 0.5f) continue;

                float coverToSelf = Vector3.Distance(stand, selfPos);
                if (coverToSelf > _maxStep) continue;   // too far to use as next step
                if (coverToSelf < _minStep) continue;   // we're basically here already

                // Higher score = better. Reward (a) closing distance to search,
                // (b) cover that protects from the search direction.
                float closure = currentDistToSearch - coverToSearch; // how much we close
                float protectionBonus = cp.ProtectsFrom(searchPos) ? 3f : 0f;
                float score = closure + protectionBonus - 0.1f * coverToSelf;

                if (score > bestScore) { bestScore = score; best = cp; }
            }
            return best;
        }

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
