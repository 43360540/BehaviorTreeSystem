using UnityEngine;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Rotates Self toward the current Target (flat / Y-only).
    /// Returns Success once angle to target is below <see cref="_epsilonDeg"/>,
    /// Running otherwise, Failure if no target.
    /// </summary>
    public sealed class FaceTarget : ActionBase<BTContext>
    {
        private readonly float _angularSpeedDegPerSec;
        private readonly float _epsilonDeg;

        public FaceTarget(float angularSpeedDegPerSec = 540f, float epsilonDeg = 5f)
        {
            _angularSpeedDegPerSec = angularSpeedDegPerSec;
            _epsilonDeg = epsilonDeg;
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (!ctx.HasTarget) return NodeStatus.Failure;

            Vector3 dir = ctx.TargetDirectionFlat;
            Quaternion want = Quaternion.LookRotation(dir);
            ctx.Self.rotation = Quaternion.RotateTowards(
                ctx.Self.rotation, want, _angularSpeedDegPerSec * dt);

            float ang = Quaternion.Angle(ctx.Self.rotation, want);
            return ang <= _epsilonDeg ? NodeStatus.Success : NodeStatus.Running;
        }
    }
}
