using UnityEngine;
using BehaviorTree;
using Unity.VisualScripting;

public class BTSample : MonoBTRunner<ContextSample>
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
                .Selector(hunt => hunt
                    .Guard(new IsTargetInRange(5f), canAttack => canAttack
                        .Action(new Attack()))
                    .Guard(new IsTargetInRange(15f), track => track
                        .Sequence(tracking => tracking
                            .Action(new LookAround())
                            .Action(new TrackTarget())
                        )
                    )
                    .Guard(new IsTired(), canBreak => canBreak
                        .Parallel(brk => brk
                            .Action(new Pant())
                            .Action(new LookAround())
                            .Action(new Wander())
                        )
                    )
                    .Action(new Idle())
                )
            );

        return root;
    }
}