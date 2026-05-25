using UnityEngine;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Scans for SAME-faction, alive, wounded NPCs inside SensorRadius.
    /// Picks the one with the lowest HpRatio (rough proxy for "needs healing most").
    /// Sets ctx.Ally + ctx.AllyTransform.
    /// </summary>
    public sealed class SenseAlly : ActionBase<BTContext>
    {
        private readonly float _wantedHpRatio;

        /// <param name="wantedHpRatio">
        /// Only consider allies whose HP ratio is below this. 1.0 = anyone alive.
        /// Default 0.85 = ignore near-full HP allies (don't waste casts).
        /// </param>
        public SenseAlly(float wantedHpRatio = 0.85f)
        {
            _wantedHpRatio = wantedHpRatio;
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            int hit = Physics.OverlapSphereNonAlloc(
                ctx.Self.position, ctx.SensorRadius, ctx.OverlapBuffer, ctx.SensorLayer);

            IHealable best = null;
            Transform bestT = null;
            float worstRatio = float.PositiveInfinity;

            for (int i = 0; i < hit; i++)
            {
                var dmg = ctx.OverlapBuffer[i].GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive) continue;
                if (ReferenceEquals(dmg.Transform, ctx.Self)) continue;
                if (dmg.Faction != ctx.Faction) continue;

                var heal = ctx.OverlapBuffer[i].GetComponentInParent<IHealable>();
                if (heal == null) continue;

                // We need an HP ratio — only BaseNPCRunner-likes expose it directly.
                // Use ctx-style lookup: BaseNPCRunner implements both; cast safely.
                if (dmg is BehaviorTree.ClassFirst.BaseNPCRunner runner)
                {
                    float ratio = runner.HpRatio;
                    if (ratio >= _wantedHpRatio) continue; // healthy, skip
                    if (ratio < worstRatio)
                    {
                        worstRatio = ratio;
                        best = heal;
                        bestT = dmg.Transform;
                    }
                }
            }

            if (best == null)
            {
                ctx.Ally = null;
                ctx.AllyTransform = null;
                return NodeStatus.Failure;
            }
            ctx.Ally = best;
            ctx.AllyTransform = bestT;
            return NodeStatus.Success;
        }
    }
}
