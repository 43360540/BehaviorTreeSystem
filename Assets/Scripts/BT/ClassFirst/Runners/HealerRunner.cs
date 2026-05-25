using BehaviorTree.ClassFirst.Actions;
using UnityEngine;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Support NPC: find the most wounded ally inside sensor range and heal them.
    /// Will move to within ctx.AttackRange (= heal range) before casting.
    /// Runs from enemies if one gets close (uses MoveAway like the Archer).
    /// </summary>
    public sealed class HealerRunner : BaseNPCRunner
    {
        [Header("Healer")]
        [SerializeField] private float _retreatRange = 4f;
        [SerializeField] private float _healCooldown = 1.5f;
        [SerializeField] private float _allyMinHpRatio = 0.85f;

        protected override INode<BTContext> CreateTree()
        {
            float healRange = _attackRange; // AttackRange field doubles as heal range

            return BTBuilder<BTContext>.Build(root => root
                .Parallel(par => par
                    // Ally sensor — refresh wounded ally pick every 0.3s.
                    .Repeater(rep => rep
                        .Force(NodeStatus.Success, force => force
                            .Throttle(0.3f, thr => thr
                                .Do(new SenseAlly(_allyMinHpRatio))
                            )
                        )
                    )
                    // Enemy sensor — separate; we don't engage but want to flee.
                    .Repeater(rep => rep
                        .Force(NodeStatus.Success, force => force
                            .Throttle(0.4f, thr => thr
                                .When((ctx, _) => !ctx.HasTarget, _ => _.Do(new Sense()))
                            )
                        )
                    )
                    // Decision branch
                    .Repeater(rep => rep
                        .Selector(sel => sel
                            // 1) Enemy too close → flee
                            .When((ctx, _) => ctx.HasTarget && ctx.TargetDistance < _retreatRange,
                                _ => _.Do(new MoveAway(desiredDistance: _retreatRange + 1.5f)))
                            // 2) Wounded ally in heal range → heal + cooldown
                            .When((ctx, _) => ctx.HasAlly && ctx.AllyDistance <= healRange,
                                _ => _.Sequence(seq => seq
                                    .Do(new HealerHeal())
                                    .Do(new WaitTimer(_healCooldown))
                                ))
                            // 3) Have wounded ally → approach them
                            .When((ctx, _) => ctx.HasAlly,
                                _ => _.Do(new MoveToAlly(stopDistance: healRange - 0.5f)))
                            // 4) March toward enemy line (stay with formation)
                            .Do(new MarchForward())
                        )
                    )
                )
            );
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, _retreatRange);
        }
    }
}
