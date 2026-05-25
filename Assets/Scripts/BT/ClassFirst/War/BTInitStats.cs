using System.Diagnostics;
using UnityEngine;

namespace BehaviorTree.ClassFirst.War
{
    /// <summary>
    /// Static accumulator for BT initialization costs. BaseNPCRunner.Awake
    /// records its own elapsed time here; PerfDump dumps it into perf-dump.json.
    /// Reset automatically at PlayMode start via RuntimeInitializeOnLoadMethod.
    /// </summary>
    public static class BTInitStats
    {
        public static int AwakeCount;
        public static long TotalAwakeTicks;
        public static long TotalCtxBuildTicks;   // ctx allocation + SetContext
        public static long TotalBaseAwakeTicks;  // includes CreateTree + new BTRunner
        public static long MaxAwakeTicks;
        public static long MinAwakeTicks;
        public static double FirstAwakeRealtime; // Time.realtimeSinceStartupAsDouble of first Awake
        public static double LastAwakeRealtime;  // Time.realtimeSinceStartupAsDouble of last Awake

        public static void Record(long awakeTicks, long ctxBuildTicks, long baseAwakeTicks)
        {
            if (AwakeCount == 0)
            {
                FirstAwakeRealtime = Time.realtimeSinceStartupAsDouble;
                MinAwakeTicks = awakeTicks;
            }
            LastAwakeRealtime = Time.realtimeSinceStartupAsDouble;
            AwakeCount++;
            TotalAwakeTicks += awakeTicks;
            TotalCtxBuildTicks += ctxBuildTicks;
            TotalBaseAwakeTicks += baseAwakeTicks;
            if (awakeTicks > MaxAwakeTicks) MaxAwakeTicks = awakeTicks;
            if (awakeTicks < MinAwakeTicks) MinAwakeTicks = awakeTicks;
        }

        public static double TicksToMs(long ticks)
            => (double)ticks / Stopwatch.Frequency * 1000.0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            AwakeCount = 0;
            TotalAwakeTicks = 0;
            TotalCtxBuildTicks = 0;
            TotalBaseAwakeTicks = 0;
            MaxAwakeTicks = 0;
            MinAwakeTicks = 0;
            FirstAwakeRealtime = 0;
            LastAwakeRealtime = 0;
        }
    }
}
