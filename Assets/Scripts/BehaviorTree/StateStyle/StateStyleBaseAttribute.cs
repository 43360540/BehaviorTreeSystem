using System;

namespace BehaviorTree.StateStyle
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class StateDefAttribute : Attribute
    {
        public Lifecycle Lifecycle { get; }
        
        public StateDefAttribute(Lifecycle lifecycle)
        {
            Lifecycle = lifecycle;
        }
    }

    public enum Lifecycle
    {
        Start,
        Tick,
        Stop,
        Abort,
        Reset,
    }
}
