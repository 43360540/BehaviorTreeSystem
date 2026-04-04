using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace BehaviorTree.StateStyle
{
    public sealed record StateStyleMethodInfo
    {
        public StateStyleMethodInfo(MethodInfo method, StateDefAttribute attribute, string name)
        {
            Method = method;
            Attribute = attribute;
            Name = name;
        }

        public MethodInfo Method { get; }
        public StateDefAttribute Attribute { get; }
        public string Name { get; }
    }

    public abstract class StateStyleBase<TSelf, TStates> : MonoBTRunner<TSelf> where TStates : struct, Enum where TSelf : StateStyleBase<TSelf, TStates>
    {
        private readonly TStates[] _states = (TStates[])Enum.GetValues(typeof(TStates));
        private readonly Dictionary<TStates, QuickAction<TSelf>> _actions = new();

        protected override void Awake()
        {
            Scan();
            base.Awake();
        }

        protected QuickAction<TSelf> Action(TStates state) =>
            _actions[state];

        protected void Scan()
        {
            _actions.Clear();

            var roughInfos = typeof(TSelf)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Select(method => new StateStyleMethodInfo(method, method.GetCustomAttribute<StateDefAttribute>(), method.Name))
                .Where(x => x.Attribute != null);

            var methodInfos = CollectMethods(roughInfos);

            BuildActions(methodInfos);
        }

        private Dictionary<TStates, Dictionary<Lifecycle, StateStyleMethodInfo>> CollectMethods(IEnumerable<StateStyleMethodInfo> infos)
        {
            var want = new Dictionary<TStates, Dictionary<Lifecycle, StateStyleMethodInfo>>();

            for (int i = 0; i < _states.Length; i++)
            {
                Dictionary<Lifecycle, StateStyleMethodInfo> lifecycles = new();
                TStates state = _states[i];
                var states = infos
                    .Where(method => RemoveSuffix(method.Name, method.Attribute.Lifecycle) == state.ToString());

                foreach (var x in states)
                {
                    lifecycles.Add(x.Attribute.Lifecycle, x);
                }
                want.Add(state, lifecycles);
            }
            return want;
        }

        private void BuildActions(Dictionary<TStates, Dictionary<Lifecycle, StateStyleMethodInfo>> infos)
        {
            foreach (var x in infos)
            {
                if (x.Value.Count < 1)
                    continue;

                Action start = null;
                Func<float, NodeStatus> tick = null;
                Action<NodeStatus> stop = null;

                if (x.Value.TryGetValue(Lifecycle.Start, out var startInfo))
                    start = (Action)Delegate.CreateDelegate(typeof(Action), this, startInfo.Method);
                if (x.Value.TryGetValue(Lifecycle.Stop, out var stopInfo))
                    stop = (Action<NodeStatus>)Delegate.CreateDelegate(typeof(Action<NodeStatus>), this, stopInfo.Method);
                if (x.Value.TryGetValue(Lifecycle.Tick, out var tickInfo))
                    tick = (Func<float, NodeStatus>)Delegate.CreateDelegate(typeof(Func<float, NodeStatus>), this, tickInfo.Method);
                else 
                    throw new InvalidOperationException();

                var action = new QuickAction<TSelf>(onStart: start, onTick: tick, onStop: stop);
                _actions.Add(x.Key, action);
            }
        }

        private string RemoveSuffix(string origin, Lifecycle lifecycle)
        {
            string suffix = lifecycle.ToString();

            if (!origin.EndsWith(suffix))
                throw new InvalidOperationException();

            return origin[..^suffix.Length];
        }
    }
}