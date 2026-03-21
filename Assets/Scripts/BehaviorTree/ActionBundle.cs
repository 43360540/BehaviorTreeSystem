using System;

namespace BehaviorTree
{
    public sealed class ActionBundle
    {
        public Action<BlackBoardMono> OnStart { get; }
        public Func<BlackBoardMono, float, NodeStatus> OnTick { get; }
        public Action<BlackBoardMono> OnStop { get; }
        public Action<BlackBoardMono> OnAbort { get; }
        public Action OnReset { get; }

        public ActionBundle(Func<BlackBoardMono, float, NodeStatus> onTick, Action<BlackBoardMono> onStart = null,
                                Action<BlackBoardMono> onStop = null, Action<BlackBoardMono> onAbort = null, Action onReset = null)
        {
            OnStart = onStart;
            OnTick = onTick ?? throw new ArgumentNullException(nameof(onTick));
            OnStop = onStop;
            OnAbort = onAbort;
            OnReset = onReset;
        }
    }
}