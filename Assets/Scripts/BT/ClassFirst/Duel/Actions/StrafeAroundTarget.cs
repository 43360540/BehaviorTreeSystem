using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Duel.Actions
{
    /// <summary>
    /// Orbit the current target at the agent's current distance, in a single
    /// direction (default counter-clockwise). Stays at attack range and keeps
    /// facing the target.
    ///
    /// <para>Returns Running for <paramref name="duration"/> seconds, then
    /// Success. Failure if no target / agent issue.</para>
    /// </summary>
    public sealed class StrafeAroundTarget : ActionBase<BTContext>
    {
        private readonly float _duration;
        private readonly float _directionSign;   // +1 = CCW, -1 = CW
        private readonly float _desiredDistance; // 0 = use current distance
        private float _elapsed;

        public StrafeAroundTarget(float duration = 1.2f, bool clockwise = false, float desiredDistance = 0f)
        {
            _duration = duration;
            _directionSign = clockwise ? -1f : 1f;
            _desiredDistance = desiredDistance;
        }

        public override void Start(BTContext ctx)
        {
            _elapsed = 0f;
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

            _elapsed += dt;

            Vector3 self = ctx.Self.position;
            Vector3 tgt = ctx.Target.Transform.position;
            Vector3 toSelf = self - tgt;
            toSelf.y = 0f;
            float currentDist = toSelf.magnitude;
            if (currentDist < 0.05f) return NodeStatus.Failure; // standing on target — bail

            float desired = _desiredDistance > 0f ? _desiredDistance : currentDist;

            // Tangent direction perpendicular to (target -> self).
            Vector3 radial = toSelf / currentDist;
            Vector3 tangent = new Vector3(-radial.z, 0f, radial.x) * _directionSign;

            // Step ahead along the tangent, then snap distance to desired.
            Vector3 stepped = self + tangent * (ctx.WalkSpeed * dt * 2f);
            Vector3 fromTgtToStepped = stepped - tgt;
            fromTgtToStepped.y = 0f;
            Vector3 destination = tgt + fromTgtToStepped.normalized * desired;

            // Snap onto NavMesh — strafe is short-hop, no path planning needed.
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                ctx.Agent.SetDestination(hit.position);

            // Face the target the whole time so we look "engaged".
            Vector3 face = -radial;
            ctx.Self.rotation = Quaternion.RotateTowards(
                ctx.Self.rotation, Quaternion.LookRotation(face), 360f * dt);

            return _elapsed >= _duration ? NodeStatus.Success : NodeStatus.Running;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (IsAgentReady(ctx)) ctx.Agent.ResetPath();
        }

        public override void Reset() => _elapsed = 0f;

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
