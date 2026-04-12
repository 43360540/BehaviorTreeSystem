using UnityEngine;
using BehaviorTree;

public class BTSample2 : MonoBTRunner<ContextSample>
{
    protected override INode<ContextSample> CreateTree()
    {
        var breakBranch = BTBuilder<ContextSample>.Build(root => root
            .When(new IsTired(), _ => _
                .Parallel(brk => brk
                    .Do(new Pant())
                    .Do(new LookAround())
                    .Do(new Wander())
                )
            )
        );

        var attackBranch = BTBuilder<ContextSample>.Build(root => root
            .When(new IsTargetInRange(5f), _ => _
                .Do(new Attack())
            )
        );

        var trackBranch = BTBuilder<ContextSample>.Build(root => root
            .When(new IsTargetInRange(15f), _ => _
                .Sequence(track => track
                    .Do(new LookAround())
                    .Do(new TrackTarget())
                )
            )
        );

        var root = BTBuilder<ContextSample>.Build(root => root
            .Selector(main => main
                .Add(breakBranch)
                .Add(attackBranch)
                .Add(trackBranch)
                .Do(new Idle()) // default
            )
        );

        return root;
    }
}