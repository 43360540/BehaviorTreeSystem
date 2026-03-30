using UnityEngine;
using BehaviorTree;

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
                .Selector(sel => sel
                    .Action(new ActionSample())
                    .Action(new QuickAction<ContextSample>((ctx, dt) =>
                    {
                        ctx.Attack();
                        return NodeStatus.Success;
                    }))
                    .Guard(new ConditionSample(), g => g
                        .Action(new ActionSample()))
                    .Sequence(seq => seq
                        .Action(new ActionSample())
                        .Condition(new ConditionSample()))
                        .Condition((ctx, dt) => Context.IsTrue)
                    .Parallel(par => par
                        .Action(new ActionSample())
                        .Action(new ActionSample()))));
        
        return root;
    }
}