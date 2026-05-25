using BehaviorTree.ClassFirst.Actions;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Mid-range melee with longer attack reach than Warrior. Same tree as
    /// WarriorRunner; Inspector AttackRange should be ~4 to differentiate.
    /// </summary>
    public sealed class SpearmanRunner : BaseNPCRunner
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
                                    .Do(new WaitTimer(0.25f))
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
