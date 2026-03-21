using System;

namespace BehaviorTree
{
    public abstract class ActionDefinition
    {
        public virtual void Start(BlackBoardMono bb) { }

        public abstract NodeStatus Tick(BlackBoardMono bb, float dt);

        public virtual void Stop(BlackBoardMono bb) { }

        public virtual void Abort(BlackBoardMono bb) { }

        public virtual void Reset() { }
    }
}