namespace BehaviorTree.ClassFirst.Duel.Actions
{
    /// <summary>
    /// Calls <see cref="IRangedAmmoController.StartReload"/> on Start, then
    /// pumps <see cref="IRangedAmmoController.TickReload"/> until reloading
    /// completes. Locks the agent in place for the duration so the NPC commits
    /// to the reload (this is the vulnerability window that enables the
    /// opponent's "Rush while target reloads" branch).
    /// </summary>
    public sealed class Reload : ActionBase<BTContext>
    {
        private readonly IRangedAmmoController _ammo;

        public Reload(IRangedAmmoController ammo)
        {
            _ammo = ammo;
        }

        public override void Start(BTContext ctx)
        {
            // Only initiate the reload cycle if we're not already mid-reload.
            // Both branches of MarksmanRunner's BT (FORCED RELOAD and the
            // ONGOING RELOAD re-entry guard) construct their own Reload
            // instance — without this check, switching between them resets
            // the timer every time the selector flips branch.
            if (_ammo != null && !_ammo.IsReloading)
                _ammo.StartReload();
            if (IsAgentReady(ctx))
            {
                ctx.Agent.isStopped = true;
                ctx.Agent.ResetPath();
            }
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (_ammo == null) return NodeStatus.Failure;
            _ammo.TickReload(dt);
            return _ammo.IsReloading ? NodeStatus.Running : NodeStatus.Success;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (IsAgentReady(ctx)) ctx.Agent.isStopped = false;
        }

        private static bool IsAgentReady(BTContext ctx)
            => ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh;
    }
}
