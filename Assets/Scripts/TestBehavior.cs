using BehaviorTree;

public class TestBehavior : ActionBase<AiContext>
{
    public override NodeStatus Tick(AiContext ctx, float dt)
    {
        throw new System.NotImplementedException();
    }
}

public class TestCondition : ICondition<AiContext>
{
    public bool Evaluate(AiContext ctx, float dt)
    {
        throw new System.NotImplementedException();
    }
}

public class AiContext{}