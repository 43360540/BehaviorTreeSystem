using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel.Actions
{
    /// <summary>
    /// Stop, turn to face the search direction (visible target / LastKnown),
    /// and hold for <paramref name="_scanDuration"/> seconds. ConeVisionSense
    /// in the parallel sensor branch sees what's in front and updates
    /// <c>ctx.Target</c> — if a higher-priority engagement branch becomes
    /// valid as a result, the parent Selector will preempt this action on the
    /// next tick.
    ///
    /// <para>Always returns Success at the end. Failure if agent off-NavMesh.</para>
    /// </summary>
    public sealed class PeekAndScan : ActionBase<BTContext>
    {
        private readonly IPerceptionHolder _perception;
        private readonly float _scanDuration;
        private readonly float _rotateSpeedDegPerSec;
        private float _elapsed;

        public PeekAndScan(IPerceptionHolder perception, float scanDuration = 1.2f, float rotateSpeedDegPerSec = 180f)
        {
            _perception = perception;
            _scanDuration = Mathf.Max(0.05f, scanDuration);
            _rotateSpeedDegPerSec = rotateSpeedDegPerSec;
        }

        public override void Start(BTContext ctx)
        {
            _elapsed = 0f;
            if (IsAgentReady(ctx))
            {
                ctx.Agent.isStopped = true;
                ctx.Agent.ResetPath();
            }
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            _elapsed += dt;

            // Face the search direction so the cone vision can sweep it.
            Vector3 lookDir = ComputeLookDir(ctx);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 1e-4f)
            {
                ctx.Self.rotation = Quaternion.RotateTowards(
                    ctx.Self.rotation,
                    Quaternion.LookRotation(lookDir.normalized),
                    _rotateSpeedDegPerSec * dt);
            }

            return _elapsed >= _scanDuration ? NodeStatus.Success : NodeStatus.Running;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (IsAgentReady(ctx)) ctx.Agent.isStopped = false;
        }

        public override void Reset() => _elapsed = 0f;

        private Vector3 ComputeLookDir(BTContext ctx)
        {
            if (ctx.HasTarget) return ctx.Target.Transform.position - ctx.Self.position;
            var ps = _perception?.PerceptionState;
            if (ps != null && ps.LastKnownPos.HasValue) return ps.LastKnownPos.Value - ctx.Self.position;
            if (ps != null) return ps.LastKnownDir;
            return ctx.Self.forward;
        }

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
