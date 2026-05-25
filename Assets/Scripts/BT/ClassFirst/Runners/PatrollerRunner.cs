using BehaviorTree.ClassFirst.Actions;
using UnityEngine;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Patrols a route. Will chase + attack briefly if an enemy enters sensor
    /// radius, but doesn't pursue beyond _giveUpRange — falls back to patrol.
    /// </summary>
    public sealed class PatrollerRunner : BaseNPCRunner
    {
        [Header("Patroller")]
        [SerializeField] private float _giveUpRange = 12f;
        [SerializeField] private float _patrolWaitAtPoint = 1.5f;

        protected override INode<BTContext> CreateTree()
        {
            float chaseStop = System.Math.Max(0.5f, _attackRange - 0.3f);

            return BTBuilder<BTContext>.Build(root => root
                .Parallel(par => par
                    // Sensor branch
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
                            // 1) In attack range -> melee
                            .When((ctx, _) => ctx.HasTarget && ctx.TargetDistance <= ctx.AttackRange,
                                _ => _.Sequence(seq => seq
                                    .Do(new FaceTarget())
                                    .Do(new WarriorCharge())
                                    .Do(new WaitTimer(0.25f))
                                ))
                            // 2) Have target & still worth chasing -> chase
                            .When((ctx, _) => ctx.HasTarget && ctx.TargetDistance <= _giveUpRange,
                                _ => _.Do(new MoveToTarget(stopDistance: chaseStop)))
                            // 3) Default: patrol the route
                            .Do(new PatrollerPatrol(waitAtPoint: _patrolWaitAtPoint))
                        )
                    )
                )
            );
        }
    }
}
