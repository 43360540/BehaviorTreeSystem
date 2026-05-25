using UnityEngine;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Archer ranged: snapshot the target the moment we start aiming (so dodging
    /// won't fully cancel the shot), play the attack animation, then resolve the
    /// shot. Resolution = if a line-of-sight raycast still hits the original
    /// target, deal damage; otherwise the shot misses.
    /// </summary>
    public sealed class ArcherShoot : ActionBase<BTContext>
    {
        private IDamageable _aimedAt;
        private bool _animatorReady;

        public override void Start(BTContext ctx)
        {
            _animatorReady = ctx.Anim != null;
            _aimedAt = ctx.Target;
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

            // Keep aiming at original target while drawing the bow.
            if (_aimedAt != null && _aimedAt.IsAlive)
            {
                Vector3 dir = _aimedAt.Transform.position - ctx.Self.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 1e-6f)
                {
                    ctx.Self.rotation = Quaternion.RotateTowards(
                        ctx.Self.rotation,
                        Quaternion.LookRotation(dir.normalized),
                        720f * dt);
                }
            }

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

            if (stopStatus != NodeStatus.Success || _aimedAt == null || !_aimedAt.IsAlive)
                return;

            // Cheap line-of-sight: raycast from self toward original target.
            Vector3 origin = ctx.Self.position + Vector3.up * 1.2f;
            Vector3 to = _aimedAt.Transform.position + Vector3.up * 1.2f;
            Vector3 dir = (to - origin);
            float dist = dir.magnitude;
            if (dist < 0.01f) { _aimedAt.TakeDamage(ctx.AttackDamage, ctx.SelfAsDamageable); return; }
            dir /= dist;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist + 0.1f))
            {
                var hitDamageable = hit.collider.GetComponentInParent<IDamageable>();
                if (ReferenceEquals(hitDamageable, _aimedAt))
                    _aimedAt.TakeDamage(ctx.AttackDamage, ctx.SelfAsDamageable);
                // else: arrow blocked by obstacle — miss.
            }
            else
            {
                // Nothing in the way (rare with colliders everywhere): treat as hit.
                _aimedAt.TakeDamage(ctx.AttackDamage, ctx.SelfAsDamageable);
            }
        }

        public override void Reset() => _aimedAt = null;

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
