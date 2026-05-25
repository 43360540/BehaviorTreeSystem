using UnityEngine;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Warrior melee: trigger AttackTrigger, lock rotation onto target, wait for
    /// the IsAttacking bool (driven by EnemyAnimationCtrl's AnimationEvents) to
    /// flip back to false. On Success, deal damage if target is still inside
    /// attack range.
    /// </summary>
    public sealed class WarriorCharge : ActionBase<BTContext>
    {
        private bool _animatorReady;

        public override void Start(BTContext ctx)
        {
            _animatorReady = ctx.Anim != null;
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
            if (!_animatorReady) return NodeStatus.Failure;

            // Face the target as we swing.
            if (ctx.HasTarget)
                ctx.Self.rotation = Quaternion.RotateTowards(
                    ctx.Self.rotation,
                    Quaternion.LookRotation(ctx.TargetDirectionFlat),
                    720f * dt);

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

            // Deal damage at the end of the swing if target is still close.
            if (stopStatus == NodeStatus.Success
                && ctx.HasTarget
                && ctx.TargetDistance <= ctx.AttackRange)
            {
                ctx.Target.TakeDamage(ctx.AttackDamage, ctx.SelfAsDamageable);
            }
        }

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
