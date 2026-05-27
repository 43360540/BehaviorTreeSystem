using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Duel.Actions
{
    /// <summary>
    /// Drive the NavMeshAgent to the host's CurrentCover stand position.
    /// Returns Success once within <see cref="_arriveTolerance"/>, Running while
    /// pathing, Failure if no cover is set / agent is off-NavMesh / path invalid.
    /// </summary>
    public sealed class MoveToCover : ActionBase<BTContext>
    {
        private readonly ICoverHolder _holder;
        private readonly float _arriveTolerance;
        private Vector3 _destSnapshot;
        private bool _hasDest;

        public MoveToCover(ICoverHolder holder, float arriveTolerance = 0.8f)
        {
            _holder = holder;
            _arriveTolerance = arriveTolerance;
        }

        public override void Start(BTContext ctx)
        {
            _hasDest = false;
            if (_holder == null || _holder.CurrentCover == null) return;
            if (!IsAgentReady(ctx)) return;

            _destSnapshot = _holder.CurrentCover.StandPosition;
            ctx.Agent.speed = ctx.WalkSpeed;
            ctx.Agent.isStopped = false;
            if (ctx.Agent.SetDestination(_destSnapshot))
                _hasDest = true;
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (_holder == null || _holder.CurrentCover == null) return NodeStatus.Failure;
            if (!IsAgentReady(ctx)) return NodeStatus.Failure;
            if (!_hasDest)
            {
                // Try again (path may have been computable after a frame).
                _destSnapshot = _holder.CurrentCover.StandPosition;
                if (!ctx.Agent.SetDestination(_destSnapshot)) return NodeStatus.Failure;
                _hasDest = true;
            }

            if (ctx.Agent.pathPending) return NodeStatus.Running;
            if (ctx.Agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return NodeStatus.Failure;

            // XZ-only — cover y=0 vs NavMeshAgent y≈1 would freeze 3D Distance
            // permanently above arriveTolerance. (Same bug class as
            // AdvanceToNextCover; both fixed together.)
            Vector3 d = ctx.Self.position - _destSnapshot; d.y = 0f;
            return d.magnitude <= _arriveTolerance ? NodeStatus.Success : NodeStatus.Running;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (IsAgentReady(ctx)) ctx.Agent.ResetPath();
        }

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
