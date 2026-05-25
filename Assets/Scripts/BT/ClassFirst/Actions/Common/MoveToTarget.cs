using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Drives the NavMeshAgent toward the current Target.
    /// Returns Success when within stopDistance, Running otherwise, Failure if
    /// no target or no path / agent off-NavMesh.
    /// </summary>
    public sealed class MoveToTarget : ActionBase<BTContext>
    {
        private readonly float _stopDistance;
        private readonly float _repathThresholdSqr;

        public MoveToTarget(float stopDistance = 0.5f, float repathThreshold = 0.25f)
        {
            _stopDistance = stopDistance;
            _repathThresholdSqr = repathThreshold * repathThreshold;
        }

        public override void Start(BTContext ctx)
        {
            if (IsAgentReady(ctx))
            {
                ctx.Agent.speed = ctx.WalkSpeed;
                ctx.Agent.isStopped = false;
            }
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (!ctx.HasTarget) return NodeStatus.Failure;
            if (!IsAgentReady(ctx)) return NodeStatus.Failure;

            Vector3 want = ctx.Target.Transform.position;
            if ((ctx.Agent.destination - want).sqrMagnitude > _repathThresholdSqr)
                ctx.Agent.SetDestination(want);

            if (ctx.Agent.pathPending) return NodeStatus.Running;
            if (ctx.TargetDistance <= _stopDistance) return NodeStatus.Success;
            if (ctx.Agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return NodeStatus.Failure;
            return NodeStatus.Running;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (IsAgentReady(ctx)) ctx.Agent.ResetPath();
        }

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
