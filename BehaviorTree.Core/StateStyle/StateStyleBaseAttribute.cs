using System;

namespace BehaviorTree.StateStyle
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class StateDefAttribute : Attribute
    {
        public string StateName { get; }
        public Phase Lifecycle { get; }
        
        public StateDefAttribute(string stateName, Phase lifecycle)
        {
            StateName = stateName;
            Lifecycle = lifecycle;
        }
    }

    public enum Phase
    {
        Start,
        Tick,
        Stop,
        Abort,
        Reset,
    }
}
