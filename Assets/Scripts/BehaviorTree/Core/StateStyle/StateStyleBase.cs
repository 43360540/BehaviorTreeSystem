using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace BehaviorTree.StateStyle
{
    public sealed record StateStyleMethodInfo
    {
        public MethodInfo Method { get; }
        public StateDefAttribute Attribute { get; }
        public string Name { get; }

        public StateStyleMethodInfo(MethodInfo method, StateDefAttribute attribute, string name)
        {
            Method = method;
            Attribute = attribute;
            Name = name;
        }
    }

    public abstract class StateStyleBase<TSelf, TStates> : MonoBTRunner<TSelf> 
    where TStates : struct, Enum where TSelf : StateStyleBase<TSelf, TStates>
    {
        private readonly TStates[] _states = (TStates[])Enum.GetValues(typeof(TStates));
        private readonly Dictionary<TStates, INode<TSelf>> _actionLeaves = new();


        protected override void Awake()
        {
            SetContext((TSelf)this);
            Scan();
            base.Awake();
        }
    
        protected INode<TSelf> GetState(TStates state) =>
            _actionLeaves[state];

        // 1. Scan every method within subclass TSelf and roughly exclude unneeded
        // 2. Collect needed and organize MethodInfos by States
        // 3. Build actions from given infos
        protected void Scan()
        {
            _actionLeaves.Clear();

            var roughInfos = typeof(TSelf)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | 
                            BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Select(method => new StateStyleMethodInfo(method, method.GetCustomAttribute<StateDefAttribute>(), method.Name))
                .Where(x => x.Attribute != null);

            var methodInfos = CollectMethods(roughInfos);

            BuildActions(methodInfos);
        }

        // Collect needed and organize MethodInfos by States
        private Dictionary<TStates, Dictionary<Phase, StateStyleMethodInfo>> CollectMethods(IEnumerable<StateStyleMethodInfo> infos)
        {
            var want = new Dictionary<TStates, Dictionary<Phase, StateStyleMethodInfo>>();

            for (int i = 0; i < _states.Length; i++)
            {
                Dictionary<Phase, StateStyleMethodInfo> lifecycles = new();
                TStates state = _states[i];
                
                var states = infos
                    .Where(method => method.Attribute.StateName == state.ToString());

                foreach (var x in states)
                    lifecycles.Add(x.Attribute.Lifecycle, x);

                want.Add(state, lifecycles);
            }
            return want;
        }

        // Build actions from given infos
        // Parameter: infos - States <-> MethodInfos
        private void BuildActions(Dictionary<TStates, Dictionary<Phase, StateStyleMethodInfo>> infos)
        {
            foreach (var x in infos)
            {
                if (x.Value.Count < 1)
                    continue;

                Action start = null;
                Action<NodeStatus> stop = null;
                Action abort = null;
                Action reset = null;
                Func<float, NodeStatus> tick = null;

                // Create delegates from each MethodInfo if it's not null
                if (x.Value.TryGetValue(Phase.Start, out var startInfo))
                    start = (Action)Delegate.CreateDelegate(typeof(Action), this, startInfo.Method);
                if (x.Value.TryGetValue(Phase.Stop, out var stopInfo))
                    stop = (Action<NodeStatus>)Delegate.CreateDelegate(typeof(Action<NodeStatus>), this, stopInfo.Method);
                if (x.Value.TryGetValue(Phase.Abort, out var abortInfo))
                    abort = (Action)Delegate.CreateDelegate(typeof(Action), this, abortInfo.Method);
                if (x.Value.TryGetValue(Phase.Reset, out var resetInfo))
                    reset = (Action)Delegate.CreateDelegate(typeof(Action), this, resetInfo.Method);
                if (x.Value.TryGetValue(Phase.Tick, out var tickInfo))
                    tick = (Func<float, NodeStatus>)Delegate.CreateDelegate(typeof(Func<float, NodeStatus>), this, tickInfo.Method);
                else // Null tick is not allowed
                    throw new InvalidOperationException();

                // Create QuickAction from delegates that created before
                var action = BTNodeFactory<TSelf>.Action( new QuickAction<TSelf>(
                                onStart: start, onTick: tick, onStop: stop, onAbort: abort, onReset: reset), x.Key.ToString());
                _actionLeaves.Add(x.Key, action);
            }
        }
    }
}