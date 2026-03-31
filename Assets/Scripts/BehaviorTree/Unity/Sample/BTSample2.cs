using UnityEngine;
using BehaviorTree;

public class BTSample2 : MonoBTRunner<ContextSample>
{
    [SerializeField] private ContextSample _context;

    protected override ContextSample CreateContext()
    {
        return _context;
    }

    protected override INode<ContextSample> CreateTree()
    {
        INode<ContextSample> breakBranch =
            BT<ContextSample>.Build(_ => _
                .When(new IsTired(), canBreak => canBreak
                    .Parallel(brk => brk
                        .Do(new Pant())
                        .Do(new LookAround())
                        .Do(new Wander())
                    )
                )
            );

        INode<ContextSample> attackBranch =
            BT<ContextSample>.Build(_ => _
                .When(new IsTargetInRange(5f), canAttack => canAttack
                    .Do(new Attack())
                )
            );

        INode<ContextSample> trackBranch =
            BT<ContextSample>.Build(_ => _
                .When(new IsTargetInRange(15f), track => track
                    .Sequence(tracking => tracking
                        .Do(new LookAround())
                        .Do(new TrackTarget())
                    )
                )
            );

        INode<ContextSample> root =
            BT<ContextSample>.Build(root => root
                .Selector(_ => _
                    .AddChild(breakBranch)
                    .AddChild(attackBranch)
                    .AddChild(trackBranch)
                    .Do(new Idle())
                )
            );

        return root;
    }
}