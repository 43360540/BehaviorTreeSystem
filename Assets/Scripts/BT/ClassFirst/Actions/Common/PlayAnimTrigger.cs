using UnityEngine;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Sets an Animator trigger and returns Success.
    /// Trigger name is hashed once on first use.
    /// </summary>
    public sealed class PlayAnimTrigger : ActionBase<BTContext>
    {
        private readonly string _triggerName;
        private int _hash;
        private bool _hashed;

        public PlayAnimTrigger(string triggerName)
        {
            _triggerName = triggerName ?? throw new System.ArgumentNullException(nameof(triggerName));
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (ctx.Anim == null) return NodeStatus.Failure;
            if (!_hashed)
            {
                _hash = Animator.StringToHash(_triggerName);
                _hashed = true;
            }
            ctx.Anim.SetTrigger(_hash);
            return NodeStatus.Success;
        }
    }
}
