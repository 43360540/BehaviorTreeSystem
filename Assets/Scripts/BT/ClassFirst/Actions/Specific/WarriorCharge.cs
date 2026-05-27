using UnityEngine;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Melee swing action. Snapshots the target the moment the swing starts
    /// (so the BT can't accidentally cancel a committed hit), then applies
    /// damage at a fixed in-animation impact time based purely on spatial
    /// range — NO perception dependency.
    ///
    /// <para>This is the v3 redesign per Sean's feedback: a swing is a physical
    /// commitment. Once the blade is moving, whether the target is "in
    /// BTContext.Target" or "visible to ConeVisionSense" is irrelevant. What
    /// matters is whether the target is in range at the impact frame.</para>
    ///
    /// <para>Behavior:</para>
    /// <list type="bullet">
    /// <item><b>Start</b> — snapshot <c>_aimedAt</c> = ctx.Target; lock agent;
    ///       trigger attack animation. <c>_impactDelivered</c> = false.</item>
    /// <item><b>Tick</b> — face the snapshot target (ignore ctx.Target loss).
    ///       When elapsed crosses <c>_impactTime</c>, deliver damage IF the
    ///       snapshot target is alive AND within attackRange in XZ space.
    ///       Then keep ticking until animation completes.</item>
    /// <item><b>Stop</b> — clean up animator + agent. NO damage logic here.</item>
    /// </list>
    ///
    /// <para>Cancel semantics: BT preemption BEFORE impactTime → no damage
    /// (we got interrupted before the blade swung). BT preemption AFTER
    /// impactTime → damage already applied, nothing further. Mid-flight
    /// LOS loss is ignored — the swing connects on geometry, not vision.</para>
    /// </summary>
    public sealed class WarriorCharge : ActionBase<BTContext>
    {
        private readonly float _impactTime;
        private IDamageable _aimedAt;
        private bool _animatorReady;
        private bool _impactDelivered;
        private float _elapsed;

        /// <param name="impactTime">Seconds from Start when the blade is
        /// considered to connect. 0.3 by default — tuned to the existing
        /// EnemyAnimationCtrl swing.</param>
        public WarriorCharge(float impactTime = 0.3f)
        {
            _impactTime = Mathf.Max(0f, impactTime);
        }

        public override void Start(BTContext ctx)
        {
            _aimedAt = ctx.Target;          // commit to the target sighted at this moment
            _animatorReady = ctx.Anim != null;
            _impactDelivered = false;
            _elapsed = 0f;

            if (IsAgentReady(ctx))
            {
                ctx.Agent.updateRotation = false;
                ctx.Agent.isStopped = true;
            }
            if (_animatorReady)
            {
                ctx.Anim.SetTrigger(ctx.AttackTriggerHash);
                ctx.Anim.SetBool(ctx.IsAttackingHash, true);
            }
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            _elapsed += dt;

            // Face the snapshot target throughout the swing — even if it's
            // currently behind cover and ctx.Target was cleared this frame.
            if (_aimedAt != null && _aimedAt.IsAlive)
            {
                Vector3 dir = _aimedAt.Transform.position - ctx.Self.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 1e-6f)
                {
                    ctx.Self.rotation = Quaternion.RotateTowards(
                        ctx.Self.rotation,
                        Quaternion.LookRotation(dir.normalized),
                        720f * dt);
                }
            }

            // Impact frame — spatial range check, no perception involved.
            if (!_impactDelivered && _elapsed >= _impactTime)
            {
                _impactDelivered = true;
                TryDeliverDamage(ctx);
            }

            if (!_animatorReady) return NodeStatus.Success;
            return ctx.Anim.GetBool(ctx.IsAttackingHash)
                ? NodeStatus.Running
                : NodeStatus.Success;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (_animatorReady)
            {
                ctx.Anim.ResetTrigger(ctx.AttackTriggerHash);
                ctx.Anim.SetBool(ctx.IsAttackingHash, false);
            }
            if (IsAgentReady(ctx))
            {
                ctx.Agent.updateRotation = true;
                ctx.Agent.isStopped = false;
            }
            // Damage was applied (or not) at impactTime in Tick — Stop is
            // purely cleanup now.
        }

        public override void Reset()
        {
            _aimedAt = null;
            _impactDelivered = false;
            _elapsed = 0f;
        }

        private void TryDeliverDamage(BTContext ctx)
        {
            if (_aimedAt == null || !_aimedAt.IsAlive) return;

            // Spatial range, XZ only. attackRange is the swing reach.
            Vector3 to = _aimedAt.Transform.position - ctx.Self.position;
            to.y = 0f;
            float range = ctx.AttackRange;
            if (to.sqrMagnitude <= range * range)
            {
                _aimedAt.TakeDamage(ctx.AttackDamage, ctx.SelfAsDamageable);
            }
            // else: target stepped out of range during the wind-up — clean miss.
        }

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
