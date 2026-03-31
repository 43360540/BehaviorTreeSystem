using BehaviorTree;
using UnityEngine;

public class IsTargetInRange : ICondition<ContextSample>
{
    private readonly float _distance;

    public IsTargetInRange(float distance) => _distance = distance;

    public bool Evaluate(ContextSample ctx, float dt)
    {
        throw new System.NotImplementedException();
    }
}