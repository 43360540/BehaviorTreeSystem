using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace BehaviorTree.ClassFirst.War
{
    /// <summary>
    /// Companion to <see cref="PerfDump"/> — pulls a *deeper* set of profiler
    /// markers so we can locate where main-thread time actually goes (render
    /// loop, GPU wait, vsync stall, physics breakdown, animator updates).
    ///
    /// Writes &lt;ProjectRoot&gt;/perf-dump-detail.json once per second.
    ///
    /// Marker availability depends on Unity version + active render pipeline;
    /// any marker that fails to bind is reported as `null` (so the JSON makes
    /// the missing-marker case obvious instead of pretending the value is 0).
    /// </summary>
    public sealed class PerfDumpDetailed : MonoBehaviour
    {
        public const string DumpFileName = "perf-dump-detail.json";
        private const int SAMPLE_CAPACITY = 60;

        // --- Render path (where on the main thread render submission lives) ---
        private ProfilerRecorder _renderLoopMain;        // RenderPipelineManager.DoRenderLoop_Internal
        private ProfilerRecorder _renderForward;         // UniversalRenderer.Execute (URP-specific)
        private ProfilerRecorder _renderOpaque;          // Render.OpaqueGeometry / DrawOpaqueObjects
        private ProfilerRecorder _renderTransparent;     // Render.TransparentGeometry / DrawTransparentObjects
        private ProfilerRecorder _renderShadows;         // Shadows.RenderShadowMap

        // --- Main-thread wait / stall (idle time we're paying for) ---
        private ProfilerRecorder _waitForPresent;        // Gfx.WaitForPresentOnGfxThread  (waiting for GPU to present)
        private ProfilerRecorder _waitForRenderJob;      // WaitForRenderJob
        private ProfilerRecorder _waitForTargetFps;      // WaitForTargetFPS  (vsync / target framerate cap)
        private ProfilerRecorder _waitForGfxCmds;        // Gfx.WaitForGfxCommandsFromMainThread (render thread waiting)

        // --- Physics breakdown ---
        private ProfilerRecorder _physicsProcessing;     // Physics.Processing  (broadphase + narrowphase)
        private ProfilerRecorder _physicsUpdateBodies;   // Physics.UpdateBodies

        // --- Animator (relevant because every NPC has one) ---
        private ProfilerRecorder _animatorUpdate;        // Animators.Update
        private ProfilerRecorder _animatorWrite;         // Animators.WriteJob

        // --- Script entry points (already covered by PerfDump, repeated here
        //     for one-stop comparison) ---
        private ProfilerRecorder _behaviourUpdate;       // BehaviourUpdate (sum of MonoBehaviour.Update)
        private ProfilerRecorder _fixedBehaviourUpdate;  // FixedBehaviourUpdate

        private float _writeAccum;
        private string _outPath;

        private void OnEnable()
        {
            Profiler.enabled = true;

            // Force the Editor not to throttle when Unity loses OS focus.
            // PlayerSettings.runInBackground only affects built players; the
            // Editor decides throttle based on Application.runInBackground at
            // runtime + focus state, so we flip it explicitly.
            Application.runInBackground = true;

            // Render path
            _renderLoopMain     = StartIfPossible(ProfilerCategory.Render, "RenderPipelineManager.DoRenderLoop_Internal");
            _renderForward      = StartIfPossible(ProfilerCategory.Render, "UniversalRenderer.Execute");
            _renderOpaque       = StartIfPossible(ProfilerCategory.Render, "Render.OpaqueGeometry");
            _renderTransparent  = StartIfPossible(ProfilerCategory.Render, "Render.TransparentGeometry");
            _renderShadows      = StartIfPossible(ProfilerCategory.Render, "Shadows.RenderShadowMap");

            // Wait / stall
            _waitForPresent     = StartIfPossible(ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread");
            _waitForRenderJob   = StartIfPossible(ProfilerCategory.Render, "WaitForRenderJob");
            _waitForTargetFps   = StartIfPossible(ProfilerCategory.Internal, "WaitForTargetFPS");
            _waitForGfxCmds     = StartIfPossible(ProfilerCategory.Render, "Gfx.WaitForGfxCommandsFromMainThread");

            // Physics
            _physicsProcessing  = StartIfPossible(ProfilerCategory.Physics, "Physics.Processing");
            _physicsUpdateBodies= StartIfPossible(ProfilerCategory.Physics, "Physics.UpdateBodies");

            // Animator
            _animatorUpdate     = StartIfPossible(ProfilerCategory.Animation, "Animators.Update");
            _animatorWrite      = StartIfPossible(ProfilerCategory.Animation, "Animators.WriteJob");

            // Scripts
            _behaviourUpdate      = StartIfPossible(ProfilerCategory.Scripts, "BehaviourUpdate");
            _fixedBehaviourUpdate = StartIfPossible(ProfilerCategory.Scripts, "FixedBehaviourUpdate");

            _outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", DumpFileName));
        }

        private void OnDisable()
        {
            DisposeIfValid(ref _renderLoopMain);
            DisposeIfValid(ref _renderForward);
            DisposeIfValid(ref _renderOpaque);
            DisposeIfValid(ref _renderTransparent);
            DisposeIfValid(ref _renderShadows);
            DisposeIfValid(ref _waitForPresent);
            DisposeIfValid(ref _waitForRenderJob);
            DisposeIfValid(ref _waitForTargetFps);
            DisposeIfValid(ref _waitForGfxCmds);
            DisposeIfValid(ref _physicsProcessing);
            DisposeIfValid(ref _physicsUpdateBodies);
            DisposeIfValid(ref _animatorUpdate);
            DisposeIfValid(ref _animatorWrite);
            DisposeIfValid(ref _behaviourUpdate);
            DisposeIfValid(ref _fixedBehaviourUpdate);
        }

        private static ProfilerRecorder StartIfPossible(ProfilerCategory cat, string name)
        {
            // ProfilerRecorder.StartNew silently returns an invalid handle if the
            // marker isn't registered — that's fine, we check .Valid before reading.
            return ProfilerRecorder.StartNew(cat, name, SAMPLE_CAPACITY);
        }

        private static void DisposeIfValid(ref ProfilerRecorder r)
        {
            if (r.Valid) r.Dispose();
        }

        private void Update()
        {
            _writeAccum += Time.unscaledDeltaTime;
            if (_writeAccum < 1f) return;
            _writeAccum = 0f;

            var sb = new StringBuilder(2048);
            sb.Append("{\n");
            sb.AppendFormat("  \"timestamp\": \"{0}\",\n", System.DateTime.UtcNow.ToString("O"));
            sb.AppendFormat("  \"application_focused\": {0},\n", Application.isFocused ? "true" : "false");
            sb.AppendFormat("  \"frame_ms\": {0:F3},\n", Time.unscaledDeltaTime * 1000.0);

            sb.Append("  \"render\": {\n");
            AppendMs(sb, "render_loop_main", _renderLoopMain, comma: true);
            AppendMs(sb, "universal_renderer_execute", _renderForward, comma: true);
            AppendMs(sb, "opaque_geometry", _renderOpaque, comma: true);
            AppendMs(sb, "transparent_geometry", _renderTransparent, comma: true);
            AppendMs(sb, "shadows_render", _renderShadows, comma: false);
            sb.Append("  },\n");

            sb.Append("  \"wait\": {\n");
            AppendMs(sb, "wait_for_present_on_gfx", _waitForPresent, comma: true);
            AppendMs(sb, "wait_for_render_job", _waitForRenderJob, comma: true);
            AppendMs(sb, "wait_for_target_fps", _waitForTargetFps, comma: true);
            AppendMs(sb, "wait_for_gfx_cmds_from_main", _waitForGfxCmds, comma: false);
            sb.Append("  },\n");

            sb.Append("  \"physics_detail\": {\n");
            AppendMs(sb, "processing", _physicsProcessing, comma: true);
            AppendMs(sb, "update_bodies", _physicsUpdateBodies, comma: false);
            sb.Append("  },\n");

            sb.Append("  \"animator\": {\n");
            AppendMs(sb, "update", _animatorUpdate, comma: true);
            AppendMs(sb, "write_job", _animatorWrite, comma: false);
            sb.Append("  },\n");

            sb.Append("  \"scripts\": {\n");
            AppendMs(sb, "behaviour_update", _behaviourUpdate, comma: true);
            AppendMs(sb, "fixed_behaviour_update", _fixedBehaviourUpdate, comma: false);
            sb.Append("  }\n");

            sb.Append("}\n");

            try
            {
                File.WriteAllText(_outPath, sb.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PerfDumpDetailed] write failed: {e.Message}");
            }
        }

        private static void AppendMs(StringBuilder sb, string key, ProfilerRecorder r, bool comma)
        {
            // Null when marker isn't registered, so the missing case is explicit.
            if (!r.Valid)
            {
                sb.AppendFormat("    \"{0}\": null{1}\n", key, comma ? "," : "");
                return;
            }
            double avgNs = AvgNs(r);
            sb.AppendFormat("    \"{0}\": {1:F3}{2}\n", key, avgNs / 1_000_000.0, comma ? "," : "");
        }

        private static double AvgNs(ProfilerRecorder rec)
        {
            int n = rec.Count;
            if (n == 0) return 0.0;
            long sum = 0;
            for (int i = 0; i < n; i++)
                sum += rec.GetSample(i).Value;
            return (double)sum / n;
        }
    }
}
