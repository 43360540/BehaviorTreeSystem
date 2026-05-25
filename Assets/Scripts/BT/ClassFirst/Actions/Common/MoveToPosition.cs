using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Walks to a fixed world-space position via NavMeshAgent.
    /// The position can be supplied at construction time, or via a delegate
    /// (so the same action instance can be reused for dynamic destinations).
    /// </summary>
    public sealed class MoveToPosition : ActionBase<BTContext>
    {
        private readonly System.Func<BTContext, Vector3> _resolver;

        public MoveToPosition(Vector3 fixedPosition)
            : this(_ => fixedPosition) { }

        public MoveToPosition(System.Func<BTContext, Vector3> resolver)
        {
            _resolver = resolver ?? throw new System.ArgumentNullException(nameof(resolver));
        }

        public override void Start(BTContext ctx)
        {
            ctx.Agent.speed = ctx.WalkSpeed;
            ctx.Agent.isStopped = false;
            ctx.Agent.SetDestination(_resolver(ctx));
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (ctx.Agent.pathPending) return NodeStatus.Running;
            if (ctx.Agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return NodeStatus.Failure;
            if (!ctx.Agent.hasPath && ctx.Agent.remainingDistance <= ctx.Agent.stoppingDistance)
                return NodeStatus.Success;
            if (ctx.Agent.remainingDistance <= ctx.Agent.stoppingDistance + 0.05f)
                return NodeStatus.Success;
            return NodeStatus.Running;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (ctx.Agent != null && ctx.Agent.isOnNavMesh)
                ctx.Agent.ResetPath();
        }
    }
}
