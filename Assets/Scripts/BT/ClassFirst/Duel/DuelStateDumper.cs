using System.IO;
using System.Text;
using BehaviorTree.ClassFirst.Duel.Conditions;
using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// Periodic snapshot writer for the duel — both as a JSON Lines log
    /// (<c>duel-state.jsonl</c>) and as a sequence of game-view PNGs
    /// (<c>duel-snaps/snap_NNN.png</c>). Lets the operator scrub through a
    /// fight after the fact even when they weren't watching the Game view
    /// the whole time.
    ///
    /// <para>Reset on Awake (deletes previous capture set). Caps at
    /// <see cref="MaxSnapshots"/> entries so a long PlayMode session doesn't
    /// fill the disk; the cap also implicitly time-boxes a single duel
    /// observation window (default 0.5 s × 120 = 60 s of capture).</para>
    ///
    /// <para>Files land at &lt;ProjectRoot&gt;/duel-state.jsonl and
    /// &lt;ProjectRoot&gt;/duel-snaps/. Both paths are .gitignored
    /// alongside the perf-dump outputs.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DuelStateDumper : MonoBehaviour
    {
        [Tooltip("Seconds between snapshots.")]
        public float Interval = 0.5f;

        [Tooltip("Set false to skip PNG captures (JSON only). Useful for long runs " +
                 "where you only want the timeline data, not 600 screenshots.")]
        public bool CaptureScreenshots = true;

        [Tooltip("Editor only: auto-stop PlayMode when either NPC dies. " +
                 "Includes a small grace delay so the last 1-2 snapshots after " +
                 "death are still captured (the moment-of-death frame).")]
        public bool AutoStopOnDeath = true;

        [Tooltip("Seconds after first death detection before stopping PlayMode.")]
        public float DeathGraceSeconds = 1.0f;

        private float _deathDetectedAt = -1f;

        private const float DUELIST_LOW_HP   = 0.30f;
        private const float DUELIST_MID_HP   = 0.60f;
        private const float MARKSMAN_RETREAT = 8f;
        private const float MARKSMAN_LOW_AMMO_RATIO = 0.4f;
        private const float MARKSMAN_ENGAGE  = 22f;

        private DuelistRunner  _duelist;
        private MarksmanRunner _marksman;

        private string _jsonPath;
        private string _shotDir;
        private float _accum;
        private int _shotIdx;
        private bool _initialized;

        private void Awake()
        {
            _jsonPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "duel-state.jsonl"));
            _shotDir  = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "duel-snaps"));
            // Wipe previous capture set so each PlayMode entry starts clean.
            try
            {
                if (File.Exists(_jsonPath)) File.Delete(_jsonPath);
                if (Directory.Exists(_shotDir))
                {
                    foreach (var f in Directory.GetFiles(_shotDir, "*.png")) File.Delete(f);
                }
                else
                {
                    Directory.CreateDirectory(_shotDir);
                }
                _initialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[DuelStateDumper] init failed: {e.Message}");
            }
        }

        private void Update()
        {
            if (!_initialized) return;
            // No upper bound — dumper runs for the whole PlayMode session and
            // naturally stops when the user exits Play. Disable CaptureScreenshots
            // for long runs to avoid filling /duel-snaps/.

            if (_duelist == null)  _duelist  = Object.FindAnyObjectByType<DuelistRunner>();
            if (_marksman == null) _marksman = Object.FindAnyObjectByType<MarksmanRunner>();
            if (_duelist == null || _marksman == null) return;

            _accum += Time.unscaledDeltaTime;
            if (_accum < Interval) return;
            _accum = 0f;

            DumpJsonLine();
            if (CaptureScreenshots)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, $"snap_{_shotIdx:D3}.png"));
            }
            _shotIdx++;

            // Auto-stop on death — Editor-only, with a small grace so the
            // moment-of-death frame ends up captured in the dump.
#if UNITY_EDITOR
            if (AutoStopOnDeath)
            {
                bool anyDead = !_duelist.IsAlive || !_marksman.IsAlive;
                if (anyDead && _deathDetectedAt < 0f)
                {
                    _deathDetectedAt = Time.unscaledTime;
                }
                if (_deathDetectedAt > 0f && Time.unscaledTime - _deathDetectedAt >= DeathGraceSeconds)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
            }
#endif
        }

        private void DumpJsonLine()
        {
            var sb = new StringBuilder(512);
            sb.Append('{');
            sb.AppendFormat("\"idx\":{0}", _shotIdx);
            sb.AppendFormat(",\"t\":{0:F2}", Time.timeSinceLevelLoad);

            // ----- Duelist -----
            var dCtx = _duelist.Context;
            sb.Append(",\"duelist\":{");
            AppendPos(sb, _duelist.transform.position);
            sb.AppendFormat(",\"hp\":{0:F1},\"hp_ratio\":{1:F3}",
                dCtx != null ? dCtx.Hp : 0f, dCtx != null ? dCtx.HpRatio : 0f);
            sb.AppendFormat(",\"alive\":{0}", _duelist.IsAlive ? "true" : "false");
            sb.AppendFormat(",\"visible\":{0}", dCtx != null && dCtx.HasTarget ? "true" : "false");
            sb.AppendFormat(",\"target_dist\":{0:F2}",
                dCtx != null && dCtx.HasTarget ? dCtx.TargetDistance : -1f);
            AppendPerception(sb, _duelist.transform.position, _duelist.PerceptionState);
            sb.AppendFormat(",\"mode\":\"{0}\"", InferDuelistMode(dCtx, _duelist));
            sb.AppendFormat(",\"cover\":\"{0}\"",
                _duelist.CurrentCover != null ? _duelist.CurrentCover.name : "");
            sb.Append('}');

            // ----- Marksman -----
            var mCtx = _marksman.Context;
            sb.Append(",\"marksman\":{");
            AppendPos(sb, _marksman.transform.position);
            sb.AppendFormat(",\"hp\":{0:F1},\"hp_ratio\":{1:F3}",
                mCtx != null ? mCtx.Hp : 0f, mCtx != null ? mCtx.HpRatio : 0f);
            sb.AppendFormat(",\"alive\":{0}", _marksman.IsAlive ? "true" : "false");
            sb.AppendFormat(",\"visible\":{0}", mCtx != null && mCtx.HasTarget ? "true" : "false");
            sb.AppendFormat(",\"ammo\":{0},\"max_ammo\":{1},\"reloading\":{2}",
                _marksman.Ammo, _marksman.MaxAmmo, _marksman.IsReloading ? "true" : "false");
            sb.AppendFormat(",\"target_dist\":{0:F2}",
                mCtx != null && mCtx.HasTarget ? mCtx.TargetDistance : -1f);
            AppendPerception(sb, _marksman.transform.position, _marksman.PerceptionState);
            sb.AppendFormat(",\"mode\":\"{0}\"", InferMarksmanMode(mCtx));
            sb.AppendFormat(",\"cover\":\"{0}\"",
                _marksman.CurrentCover != null ? _marksman.CurrentCover.name : "");
            sb.Append('}');

            sb.Append("}\n");

            try
            {
                File.AppendAllText(_jsonPath, sb.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[DuelStateDumper] write failed: {e.Message}");
            }
        }

        private static void AppendPos(StringBuilder sb, Vector3 p)
        {
            sb.AppendFormat("\"pos\":[{0:F2},{1:F2},{2:F2}]", p.x, p.y, p.z);
        }

        private static void AppendPerception(StringBuilder sb, Vector3 selfPos, PerceptionState ps)
        {
            sb.AppendFormat(",\"has_contact\":{0}", ps != null && ps.HasContact ? "true" : "false");
            if (ps != null && ps.HasContact)
            {
                float d = Vector3.Distance(selfPos, ps.LastKnownPos.Value);
                sb.AppendFormat(",\"last_known_dist\":{0:F2}", d);
            }
            else
            {
                sb.Append(",\"last_known_dist\":-1");
            }
        }

        // -----------------------------------------------------------------
        // Mode inference — mirrors the .When predicates in each Runner.
        // Kept in sync with DuelHud's copy; if you change Runner branch
        // logic, update BOTH this and DuelHud.InferXxxMode.
        // -----------------------------------------------------------------

        private static string InferDuelistMode(BTContext ctx, IPerceptionHolder p)
        {
            bool visible = ctx != null && ctx.HasTarget;
            if (visible)
            {
                if (IsTargetReloading.Check(ctx)) return "OPPORTUNITY";
                if (ctx.HpRatio < DUELIST_LOW_HP) return "DEFENSIVE";
                if (ctx.HpRatio < DUELIST_MID_HP) return "TACTICAL_FLANK";
                if (ctx.TargetDistance <= ctx.AttackRange) return "CHARGE";
                return "ADVANCE_VISIBLE";
            }
            return p?.PerceptionState?.HasContact == true ? "INVESTIGATE_LAST_KNOWN" : "INVESTIGATE_DIRECTION";
        }

        private string InferMarksmanMode(BTContext ctx)
        {
            bool visible = ctx != null && ctx.HasTarget;
            if (visible)
            {
                if (!_marksman.IsReloading && _marksman.Ammo > 0 && IsTargetReloading.Check(ctx))
                    return "OPPORTUNITY";

                if (ctx.TargetDistance < MARKSMAN_RETREAT)
                {
                    if (!_marksman.IsReloading && _marksman.Ammo > 0) return "DANGER_SNAP_SHOT";
                    return "DANGER_KITE";
                }
                if (_marksman.Ammo <= 0 && !_marksman.IsReloading) return "FORCED_RELOAD";
                if (_marksman.IsReloading) return "ONGOING_RELOAD";
                bool ammoLow = (float)_marksman.Ammo / Mathf.Max(1, _marksman.MaxAmmo) <= MARKSMAN_LOW_AMMO_RATIO;
                if (ammoLow && ctx.TargetDistance >= MARKSMAN_RETREAT + 2f) return "TACTICAL_RELOAD";
                if (_marksman.Ammo > 0 && ctx.TargetDistance <= MARKSMAN_ENGAGE) return "ENGAGE";
                return "ADVANCE_VISIBLE";
            }
            if (_marksman.Ammo <= 0 && !_marksman.IsReloading) return "FORCED_RELOAD";
            if (_marksman.IsReloading) return "ONGOING_RELOAD";
            return _marksman.PerceptionState?.HasContact == true ? "INVESTIGATE_LAST_KNOWN" : "INVESTIGATE_DIRECTION";
        }
    }
}
