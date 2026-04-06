using UnityEngine;
using BehaviorTree;

public class BTSample2 : MonoBTRunner<ContextSample>
{
    protected override INode<ContextSample> CreateTree()
    {
        INode<ContextSample> breakBranch =
            BT<ContextSample>.Build(root => root
                .When(new IsTired(), _ => _
                    .Parallel(brk => brk
                        .Do(new Pant())
                        .Do(new LookAround())
                        .Do(new Wander())
                    )
                )
            );

        INode<ContextSample> attackBranch =
            BT<ContextSample>.Build(root => root
                .When(new IsTargetInRange(5f), _ => _
                    .Do(new Attack())
                )
            );

        INode<ContextSample> trackBranch =
            BT<ContextSample>.Build(root => root
                .When(new IsTargetInRange(15f), _ => _
                    .Sequence(track => track
                        .Do(new LookAround())
                        .Do(new TrackTarget())
                    )
                )
            );

        INode<ContextSample> root =
            BT<ContextSample>.Build(root => root
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