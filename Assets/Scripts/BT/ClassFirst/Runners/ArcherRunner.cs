using BehaviorTree.ClassFirst.Actions;
using UnityEngine;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Kiting ranged NPC.
    /// If enemy is too close (under _retreatRange) -> back away.
    /// If enemy is in shoot range (under AttackRange) -> face + shoot + cooldown.
    /// If have target but out of range -> close to shoot range.
    /// Otherwise idle.
    /// </summary>
    public sealed class ArcherRunner : BaseNPCRunner
    {
        [Header("Archer")]
        [SerializeField] private float _retreatRange = 4f;
        [SerializeField] private float _shootCooldown = 1.2f;

        protected override INode<BTContext> CreateTree()
        {
            float closeShoot = System.Math.Max(_retreatRange + 0.5f, _attackRange - 1f);

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
                            // 1) Too close -> retreat to outside _retreatRange
                            .When((ctx, _) => ctx.HasTarget && ctx.TargetDistance < _retreatRange,
                                _ => _.Do(new MoveAway(desiredDistance: _retreatRange + 1.5f)))
                            // 2) In shoot range -> face + shoot + cooldown
                            .When((ctx, _) => ctx.HasTarget && ctx.TargetDistance <= ctx.AttackRange,
                                _ => _.Sequence(seq => seq
                                    .Do(new FaceTarget(angularSpeedDegPerSec: 360f, epsilonDeg: 8f))
                                    .Do(new ArcherShoot())
                                    .Do(new WaitTimer(_shootCooldown))
                                ))
                            // 3) Out of range -> close in to just inside shoot range
                            .When((ctx, _) => ctx.HasTarget,
                                _ => _.Do(new MoveToTarget(stopDistance: closeShoot)))
                            // 4) March toward enemy
                            .Do(new MarchForward())
                        )
                    )
                )
            );
        }
    }
}
