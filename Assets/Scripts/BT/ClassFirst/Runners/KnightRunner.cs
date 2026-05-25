using BehaviorTree.ClassFirst.Actions;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Fast-charging melee. Same tree as WarriorRunner, but Inspector stats
    /// are tuned higher (more HP/speed/damage). Same WarriorCharge action.
    /// </summary>
    public sealed class KnightRunner : BaseNPCRunner
    {
        protected override INode<BTContext> CreateTree()
        {
            float chaseStop = System.Math.Max(0.5f, _attackRange - 0.3f);

            return BTBuilder<BTContext>.Build(root => root
                .Parallel(par => par
                    .Repeater(rep => rep
                        .Force(NodeStatus.Success, force => force
                            .Throttle(0.3f, thr => thr
                                .When((ctx, _) => !ctx.HasTarget, _ => _.Do(new Sense()))
                            )
                        )
                    )
                    .Repeater(rep => rep
                        .Selector(sel => sel
                            .When((ctx, _) => ctx.HasTarget && ctx.TargetDistance <= ctx.AttackRange,
                                _ => _.Sequence(seq => seq
                                    .Do(new FaceTarget())
                                    .Do(new WarriorCharge())
                                    .Do(new WaitTimer(0.15f))
                                ))
                            .When((ctx, _) => ctx.HasTarget,
                                _ => _.Do(new MoveToTarget(stopDistance: chaseStop)))
                            .Do(new MarchForward())
                        )
                    )
                )
            );
        }
    }
}
