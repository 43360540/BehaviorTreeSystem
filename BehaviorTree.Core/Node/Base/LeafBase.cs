using System;
using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class LeafBase<TContext> : NodeBase<TContext>
    {
        public LeafBase(string? name = null) : base(name) {}

        protected override sealed void OnTimeElapse(float dt) { }
    }
}
