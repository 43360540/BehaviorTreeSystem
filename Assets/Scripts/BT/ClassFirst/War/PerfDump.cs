using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace BehaviorTree.ClassFirst.War
{
    /// <summary>
    /// Pulls Unity Profiler markers via ProfilerRecorder and writes them to a
    /// JSON file once per second. Use it to inspect perf when OnGUI screenshots
    /// or unity-cli's profiler API don't give per-script breakdown.
    /// Output: &lt;ProjectRoot&gt;/perf-dump.json
    /// </summary>
    public sealed class PerfDump : MonoBehaviour
    {
        public const string DumpFileName = "perf-dump.json";
        private const int SAMPLE_CAPACITY = 60;

        // Sampler-style (time per frame, nanoseconds)
        private ProfilerRecorder _scriptsUpdate;
        private ProfilerRecorder _scriptsLateUpdate;
        private ProfilerRecorder _physicsSimulate;
        private ProfilerRecorder _renderRender;

        // Counter-style (per-frame integer values)
        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _triangles;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPass;
        private ProfilerRecorder _gcAllocBytes;
        private ProfilerRecorder _totalUsedMemory;
        private ProfilerRecorder _systemUsedMemory;

        private float _writeAccum;
        private float _fpsAccum;
        private int _fpsFrames;
        private string _outPath;

        private void OnEnable()
        {
            // Force the Profiler ON in PlayMode so ProfilerRecorder produces samples.
            // (Otherwise everything reads 0.)
            Profiler.enabled = true;

            // Common Unity profiler markers — names verified for Unity 6.
            _scriptsUpdate     = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "BehaviourUpdate", SAMPLE_CAPACITY);
            _scriptsLateUpdate = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "BehaviourLateUpdate", SAMPLE_CAPACITY);
            _physicsSimulate   = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Simulate", SAMPLE_CAPACITY);
            _renderRender      = ProfilerRecorder.StartNew(ProfilerCategory.Render,  "Camera.Render", SAMPLE_CAPACITY);

            _drawCalls         = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _triangles         = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _batches           = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPass           = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _gcAllocBytes      = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _totalUsedMemory   = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            _systemUsedMemory  = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");

            _outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", DumpFileName));
        }

        private void OnDisable()
        {
            DisposeIfValid(ref _scriptsUpdate);
            DisposeIfValid(ref _scriptsLateUpdate);
            DisposeIfValid(ref _physicsSimulate);
            DisposeIfValid(ref _renderRender);
            DisposeIfValid(ref _drawCalls);
            DisposeIfValid(ref _triangles);
            DisposeIfValid(ref _batches);
            DisposeIfValid(ref _setPass);
            DisposeIfValid(ref _gcAllocBytes);
            DisposeIfValid(ref _totalUsedMemory);
            DisposeIfValid(ref _systemUsedMemory);
        }

        private static void DisposeIfValid(ref ProfilerRecorder r)
        {
            if (r.Valid) r.Dispose();
        }

        private void Update()
        {
            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;
            _writeAccum += Time.unscaledDeltaTime;
            if (_writeAccum < 1f) return;

            float avgFps = _fpsFrames > 0 ? _fpsFrames / _fpsAccum : 0f;
            float avgFrameMs = avgFps > 0 ? 1000f / avgFps : 0f;
            _writeAccum = 0f;
            _fpsAccum = 0f;
            _fpsFrames = 0;

            int activeNpcs = FindObjectsByType<BaseNPCRunner>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            bool appFocused = Application.isFocused;

            var sb = new StringBuilder(800);
            sb.Append("{\n");
            sb.AppendFormat("  \"timestamp\": \"{0}\",\n", System.DateTime.UtcNow.ToString("O"));
            sb.AppendFormat("  \"application_focused\": {0},\n", appFocused ? "true" : "false");
            sb.AppendFormat("  \"profiler_enabled\": {0},\n", Profiler.enabled ? "true" : "false");
            sb.AppendFormat("  \"active_npcs\": {0},\n", activeNpcs);
            sb.AppendFormat("  \"fps_avg_last_1s\": {0:F2},\n", avgFps);
            sb.AppendFormat("  \"frame_ms_avg\": {0:F3},\n", avgFrameMs);
            sb.Append("  \"scripts\": {\n");
            sb.AppendFormat("    \"update_ms\": {0:F3},\n",      AvgNs(_scriptsUpdate) / 1_000_000.0);
            sb.AppendFormat("    \"late_update_ms\": {0:F3}\n",  AvgNs(_scriptsLateUpdate) / 1_000_000.0);
            sb.Append("  },\n");
            sb.Append("  \"physics\": {\n");
            sb.AppendFormat("    \"simulate_ms\": {0:F3}\n", AvgNs(_physicsSimulate) / 1_000_000.0);
            sb.Append("  },\n");
            sb.Append("  \"rendering\": {\n");
            sb.AppendFormat("    \"camera_render_ms\": {0:F3},\n", AvgNs(_renderRender) / 1_000_000.0);
            sb.AppendFormat("    \"draw_calls\": {0},\n", LastOrZero(_drawCalls));
            sb.AppendFormat("    \"triangles\": {0},\n", LastOrZero(_triangles));
            sb.AppendFormat("    \"batches\": {0},\n", LastOrZero(_batches));
            sb.AppendFormat("    \"set_pass_calls\": {0}\n", LastOrZero(_setPass));
            sb.Append("  },\n");
            sb.Append("  \"memory\": {\n");
            sb.AppendFormat("    \"gc_alloc_per_frame_bytes\": {0},\n", LastOrZero(_gcAllocBytes));
            sb.AppendFormat("    \"total_used_mb\": {0:F1},\n", LastOrZero(_totalUsedMemory) / (1024.0 * 1024.0));
            sb.AppendFormat("    \"system_used_mb\": {0:F1}\n", LastOrZero(_systemUsedMemory) / (1024.0 * 1024.0));
            sb.Append("  },\n");

            // ---- Init stats (one-shot at NPC Awake; doesn't update after) ----
            int initCount = BTInitStats.AwakeCount;
            double totalAwakeMs    = BTInitStats.TicksToMs(BTInitStats.TotalAwakeTicks);
            double totalCtxMs      = BTInitStats.TicksToMs(BTInitStats.TotalCtxBuildTicks);
            double totalBaseAwakeMs= BTInitStats.TicksToMs(BTInitStats.TotalBaseAwakeTicks);
            double avgAwakeMs      = initCount > 0 ? totalAwakeMs / initCount : 0;
            double avgCtxMs        = initCount > 0 ? totalCtxMs / initCount : 0;
            double avgBaseAwakeMs  = initCount > 0 ? totalBaseAwakeMs / initCount : 0;
            double maxAwakeMs      = BTInitStats.TicksToMs(BTInitStats.MaxAwakeTicks);
            double minAwakeMs      = BTInitStats.TicksToMs(BTInitStats.MinAwakeTicks);
            double wallclockSpanMs = (BTInitStats.LastAwakeRealtime - BTInitStats.FirstAwakeRealtime) * 1000.0;

            sb.Append("  \"init\": {\n");
            sb.AppendFormat("    \"npc_count\": {0},\n", initCount);
            sb.AppendFormat("    \"total_awake_ms\": {0:F2},\n", totalAwakeMs);
            sb.AppendFormat("    \"total_ctx_build_ms\": {0:F2},\n", totalCtxMs);
            sb.AppendFormat("    \"total_base_awake_ms\": {0:F2},\n", totalBaseAwakeMs);
            sb.AppendFormat("    \"avg_awake_ms\": {0:F4},\n", avgAwakeMs);
            sb.AppendFormat("    \"avg_ctx_build_ms\": {0:F4},\n", avgCtxMs);
            sb.AppendFormat("    \"avg_base_awake_ms\": {0:F4},\n", avgBaseAwakeMs);
            sb.AppendFormat("    \"max_awake_ms\": {0:F3},\n", maxAwakeMs);
            sb.AppendFormat("    \"min_awake_ms\": {0:F3},\n", minAwakeMs);
            sb.AppendFormat("    \"wallclock_span_ms\": {0:F1}\n", wallclockSpanMs);
            sb.Append("  }\n");
            sb.Append("}\n");

            try
            {
                File.WriteAllText(_outPath, sb.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PerfDump] write failed: {e.Message}");
            }
        }

        private static double AvgNs(ProfilerRecorder rec)
        {
            if (!rec.Valid) return 0.0;
            int n = rec.Count;
            if (n == 0) return 0.0;
            long sum = 0;
            for (int i = 0; i < n; i++)
                sum += rec.GetSample(i).Value;
            return (double)sum / n;
        }

        private static long LastOrZero(ProfilerRecorder rec)
            => rec.Valid ? rec.LastValue : 0L;
    }
}
