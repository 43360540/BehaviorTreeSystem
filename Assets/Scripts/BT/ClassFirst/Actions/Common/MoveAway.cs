using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Backs away from the current target until at least desiredDistance.
    /// Computes a flee goal opposite to the target and projects onto NavMesh.
    /// </summary>
    public sealed class MoveAway : ActionBase<BTContext>
    {
        private readonly float _desiredDistance;
        private readonly float _stepAhead;

        public MoveAway(float desiredDistance, float stepAhead = 1.5f)
        {
            _desiredDistance = desiredDistance;
            _stepAhead = stepAhead;
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

            float dist = ctx.TargetDistance;
            if (dist >= _desiredDistance) return NodeStatus.Success;

            Vector3 away = -ctx.TargetDirectionFlat;
            Vector3 goal = ctx.Self.position + away * (_desiredDistance - dist + _stepAhead);

            if (NavMesh.SamplePosition(goal, out NavMeshHit hit, 3f, ctx.Agent.areaMask))
            {
                ctx.Agent.SetDestination(hit.position);
                return NodeStatus.Running;
            }

            // Couldn't find a NavMesh point in the flee direction → perpendicular dodge.
            Vector3 dodge = Vector3.Cross(Vector3.up, ctx.TargetDirectionFlat).normalized;
            goal = ctx.Self.position + dodge * _stepAhead;
            if (NavMesh.SamplePosition(goal, out hit, 3f, ctx.Agent.areaMask))
            {
                ctx.Agent.SetDestination(hit.position);
                return NodeStatus.Running;
            }
            return NodeStatus.Failure;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (IsAgentReady(ctx)) ctx.Agent.ResetPath();
        }

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
