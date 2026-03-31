using UnityEngine;
using BehaviorTree;

public class BTSample1 : MonoBTRunner<ContextSample>
{
    [SerializeField] private ContextSample _context;

    protected override ContextSample CreateContext()
    {
        return _context;
    }

    protected override INode<ContextSample> CreateTree()
    {
        INode<ContextSample> root =
            BT<ContextSample>.Build(root => root
                // Start point
                .Selector(main => main
                    // Break branch
                    .When(new IsTired(), _ => _
                        .Parallel(brk => brk
                            .Do(new Pant())
                            .Do(new LookAround())
                            .Do(new Wander())
                        )
                    )
                    // Attack branch
                    .When(new IsTargetInRange(5f), _ => _
                        .Do(new Attack())
                    )
                    // Track bracnch
                    .When(new IsTargetInRange(15f), _ => _
                        .Sequence(track => track
                            .Do(new LookAround())
                            .Do(new TrackTarget())
                        )
                    )
                    // Default
                    .Do(new Idle())
                )
            );

        return root;
    }
}