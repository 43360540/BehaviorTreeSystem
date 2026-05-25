using UnityEngine;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Scans Physics colliders inside <see cref="BTContext.SensorRadius"/>, filters
    /// out self / same-faction / dead, and writes the nearest survivor to
    /// <see cref="BTContext.Target"/>.
    /// Returns Success when a target is acquired, Failure otherwise.
    /// </summary>
    public sealed class Sense : ActionBase<BTContext>
    {
        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            int hit = Physics.OverlapSphereNonAlloc(
                ctx.Self.position, ctx.SensorRadius, ctx.OverlapBuffer, ctx.SensorLayer);

            IDamageable best = null;
            float bestSqr = float.PositiveInfinity;
            Vector3 selfPos = ctx.Self.position;

            for (int i = 0; i < hit; i++)
            {
                var d = ctx.OverlapBuffer[i].GetComponentInParent<IDamageable>();
                if (d == null) continue;
                if (!d.IsAlive) continue;
                if (ReferenceEquals(d.Transform, ctx.Self)) continue;
                if (d.Faction == ctx.Faction) continue;

                float sqr = (d.Transform.position - selfPos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = d;
                }
            }

            if (best == null)
            {
                ctx.Target = null;
                return NodeStatus.Failure;
            }

            ctx.Target = best;
            return NodeStatus.Success;
        }
    }
}
