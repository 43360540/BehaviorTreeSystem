using BehaviorTree.ClassFirst.Actions;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Aggressive melee NPC.
    /// Senses every 0.3s. If an enemy is in attack range, face + charge.
    /// Otherwise chase. Otherwise idle (no patrol).
    /// </summary>
    public sealed class WarriorRunner : BaseNPCRunner
    {
        protected override INode<BTContext> CreateTree()
        {
            float chaseStop = System.Math.Max(0.5f, _attackRange - 0.3f);

            return BTBuilder<BTContext>.Build(root => root
                .Parallel(par => par
                    // Sensor branch — refresh target while we don't have one.
                    .Repeater(rep => rep
                        .Force(NodeStatus.Success, force => force
                            .Throttle(0.3f, thr => thr
                                .When((ctx, _) => !ctx.HasTarget, _ => _.Do(new Sense()))
                            )
                        )
                    )
                    // Decision branch
                    .Repeater(rep => rep
                        .Selector(sel => sel
                            // 1) In range -> face + charge + brief cooldown
                            .When((ctx, _) => ctx.HasTarget && ctx.TargetDistance <= ctx.AttackRange,
                                _ => _.Sequence(seq => seq
                                    .Do(new FaceTarget())
                                    .Do(new WarriorCharge())
                                    .Do(new WaitTimer(0.2f))
                                ))
                            // 2) Have target -> chase
                            .When((ctx, _) => ctx.HasTarget,
                                _ => _.Do(new MoveToTarget(stopDistance: chaseStop)))
                            // 3) March toward enemy main line (war demo default)
                            .Do(new MarchForward())
                        )
                    )
                )
            );
        }
    }
}
