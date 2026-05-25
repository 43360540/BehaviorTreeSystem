using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Move toward ctx.Ally via NavMeshAgent. Used by Healer to approach a wounded ally.
    /// </summary>
    public sealed class MoveToAlly : ActionBase<BTContext>
    {
        private readonly float _stopDistance;

        public MoveToAlly(float stopDistance = 4f) => _stopDistance = stopDistance;

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
            if (!ctx.HasAlly) return NodeStatus.Failure;
            if (!IsAgentReady(ctx)) return NodeStatus.Failure;

            Vector3 want = ctx.AllyTransform.position;
            if ((ctx.Agent.destination - want).sqrMagnitude > 0.0625f)
                ctx.Agent.SetDestination(want);
            if (ctx.Agent.pathPending) return NodeStatus.Running;
            if (ctx.AllyDistance <= _stopDistance) return NodeStatus.Success;
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
