using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Duel.Actions
{
    /// <summary>
    /// "Advanced" tactic: route the NavMeshAgent to a position offset to one
    /// side of the target so we can attack the flank instead of charging
    /// head-on. The offset uses the target's <c>transform.forward</c> as
    /// reference, swung 120° to the chosen side.
    ///
    /// <para>Picks the side (left vs right) opportunistically based on whether
    /// the candidate position is on the NavMesh; falls back to the other side
    /// otherwise. Returns Success once close enough to the flank point, Running
    /// while pathing, Failure if neither side is reachable.</para>
    /// </summary>
    public sealed class MoveToFlankPosition : ActionBase<BTContext>
    {
        private readonly float _flankAngleDeg;
        private readonly float _flankRadius;
        private readonly float _arriveTolerance;
        private Vector3 _flankPos;
        private bool _flankValid;

        public MoveToFlankPosition(float flankAngleDeg = 120f, float flankRadius = 4f, float arriveTolerance = 1.2f)
        {
            _flankAngleDeg = flankAngleDeg;
            _flankRadius = flankRadius;
            _arriveTolerance = arriveTolerance;
        }

        public override void Start(BTContext ctx)
        {
            _flankValid = TryPickFlank(ctx, out _flankPos);
            if (!IsAgentReady(ctx)) return;
            if (_flankValid)
            {
                ctx.Agent.speed = ctx.WalkSpeed;
                ctx.Agent.isStopped = false;
                ctx.Agent.SetDestination(_flankPos);
            }
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (!ctx.HasTarget) return NodeStatus.Failure;
            if (!IsAgentReady(ctx)) return NodeStatus.Failure;
            if (!_flankValid) return NodeStatus.Failure;

            if (ctx.Agent.pathPending) return NodeStatus.Running;
            if (ctx.Agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return NodeStatus.Failure;

            // XZ-only — flank position is computed at target's y (≈1m for an
            // NPC) but the snap to NavMesh might land at ground (y=0).
            // 3D distance would falsely report 1m+ even when arrived.
            Vector3 d = ctx.Self.position - _flankPos; d.y = 0f;
            return d.magnitude <= _arriveTolerance ? NodeStatus.Success : NodeStatus.Running;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (IsAgentReady(ctx)) ctx.Agent.ResetPath();
        }

        private bool TryPickFlank(BTContext ctx, out Vector3 pos)
        {
            pos = ctx.Self.position;
            if (!ctx.HasTarget) return false;

            Vector3 tgt = ctx.Target.Transform.position;
            // Target facing — fall back to (target->self) if forward is zero-ish.
            Vector3 fwd = ctx.Target.Transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f)
            {
                fwd = (ctx.Self.position - tgt);
                fwd.y = 0f;
            }
            if (fwd.sqrMagnitude < 1e-4f) return false;
            fwd.Normalize();

            // Try the side closer to where we already are first — feels less
            // teleport-y and usually has a shorter path.
            Vector3 selfFromTgt = ctx.Self.position - tgt;
            selfFromTgt.y = 0f;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            bool tryRightFirst = Vector3.Dot(selfFromTgt, right) > 0f;

            if (TryFlankSide(ctx, tgt, fwd, tryRightFirst ? +1f : -1f, out pos)) return true;
            if (TryFlankSide(ctx, tgt, fwd, tryRightFirst ? -1f : +1f, out pos)) return true;
            return false;
        }

        private bool TryFlankSide(BTContext ctx, Vector3 tgt, Vector3 fwd, float sideSign, out Vector3 pos)
        {
            float angle = _flankAngleDeg * sideSign;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * fwd;
            Vector3 candidate = tgt + dir * _flankRadius;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                pos = hit.position;
                return true;
            }
            pos = candidate;
            return false;
        }

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
