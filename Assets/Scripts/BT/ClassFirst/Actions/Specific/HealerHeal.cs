using UnityEngine;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Heal the current ctx.Ally. Plays the attack animation as a stand-in for
    /// a cast animation (no dedicated heal anim in this demo), waits for it to
    /// finish, then applies ctx.AttackDamage as positive heal.
    /// </summary>
    public sealed class HealerHeal : ActionBase<BTContext>
    {
        private bool _animatorReady;
        private IHealable _aimedAt;
        private float _fallbackTimer;

        public override void Start(BTContext ctx)
        {
            _animatorReady = ctx.Anim != null;
            _aimedAt = ctx.Ally;
            _fallbackTimer = 0f;
            if (!_animatorReady) return;
            if (IsAgentReady(ctx))
            {
                ctx.Agent.updateRotation = false;
                ctx.Agent.isStopped = true;
            }
            ctx.Anim.SetTrigger(ctx.AttackTriggerHash);
            ctx.Anim.SetBool(ctx.IsAttackingHash, true);
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            // Face the ally during cast.
            if (ctx.AllyTransform != null)
            {
                Vector3 dir = ctx.AllyTransform.position - ctx.Self.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 1e-6f)
                {
                    ctx.Self.rotation = Quaternion.RotateTowards(
                        ctx.Self.rotation, Quaternion.LookRotation(dir.normalized), 360f * dt);
                }
            }

            if (!_animatorReady) return NodeStatus.Failure;

            // Watchdog: if Animator never reports IsAttacking=true (e.g. wrong
            // controller wired), don't spin forever — bail after 2s.
            _fallbackTimer += dt;
            if (_fallbackTimer > 2f) return NodeStatus.Success;

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

            // Apply heal at the end of the cast — re-use AttackDamage field as heal amount.
            if (stopStatus == NodeStatus.Success && _aimedAt != null)
            {
                if (_aimedAt is IDamageable d && d.IsAlive)
                    _aimedAt.Heal(ctx.AttackDamage, ctx.SelfAsDamageable);
            }
        }

        public override void Reset() => _aimedAt = null;

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
