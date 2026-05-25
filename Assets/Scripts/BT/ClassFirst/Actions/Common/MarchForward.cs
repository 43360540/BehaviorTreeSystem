using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// "If you don't see an enemy yet, walk toward where the enemy IS."
    /// Drives the NavMeshAgent in ctx.EnemyDirection. The war demo bakes NavMesh
    /// into the scene at setup time, so agents are on the mesh from frame 1.
    /// </summary>
    public sealed class MarchForward : ActionBase<BTContext>
    {
        private readonly float _strideAhead;

        public MarchForward() : this(4f) { }
        public MarchForward(float strideAhead) => _strideAhead = strideAhead;

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
            if (ctx.EnemyDirection.sqrMagnitude < 1e-6f) return NodeStatus.Success;
            if (!IsAgentReady(ctx)) return NodeStatus.Failure;

            // Re-project a forward goal each ~tick so the NPC keeps advancing.
            Vector3 goal = ctx.Self.position + ctx.EnemyDirection * _strideAhead;
            if ((ctx.Agent.destination - goal).sqrMagnitude > 0.25f)
            {
                if (NavMesh.SamplePosition(goal, out NavMeshHit hit, 2f, ctx.Agent.areaMask))
                    ctx.Agent.SetDestination(hit.position);
            }
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
