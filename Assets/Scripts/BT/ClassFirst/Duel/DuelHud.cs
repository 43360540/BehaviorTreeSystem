using BehaviorTree.ClassFirst.Duel.Conditions;
using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel
{
    /// <summary>
    /// On-screen HUD for the 1v1 duel demo. Shows HP bar + ammo + reload state
    /// + the BT branch the runner is most likely executing right now (inferred
    /// from the same predicates the BT's <c>.When</c> nodes use, so the HUD
    /// can't "lie" without the BT also being wrong).
    ///
    /// <para>Auto-finds both runners via FindObjectsByType on first OnGUI.
    /// Place on the demo root (DuelSceneSetup adds it).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DuelHud : MonoBehaviour
    {
        // Defaults must match MarksmanRunner / DuelistRunner SerializeField defaults
        // (we don't expose the tunables to avoid bloating Runner public surface).
        private const float DUELIST_LOW_HP   = 0.30f;
        private const float DUELIST_MID_HP   = 0.60f;
        private const float MARKSMAN_RETREAT = 8f;
        private const float MARKSMAN_LOW_AMMO_RATIO = 0.4f;
        private const float MARKSMAN_ENGAGE  = 22f;

        private DuelistRunner  _duelist;
        private MarksmanRunner _marksman;
        private GUIStyle _label;
        private GUIStyle _header;
        private Texture2D _whiteTex;

        private void OnGUI()
        {
            EnsureRefs();
            EnsureStyles();

            const float W = 320f;
            const float H = 170f;
            const float PAD = 12f;

            if (_duelist != null)
                DrawDuelist(new Rect(PAD, PAD, W, H));

            if (_marksman != null)
                DrawMarksman(new Rect(Screen.width - W - PAD, PAD, W, H));
        }

        private void DrawDuelist(Rect rect)
        {
            DrawPanel(rect, new Color(0.15f, 0.05f, 0.05f, 0.85f));
            float x = rect.x + 10f, y = rect.y + 8f;

            GUI.Label(new Rect(x, y, rect.width - 20f, 22f), "DUELIST (melee)", _header);
            y += 24f;

            var ctx = _duelist.Context;
            float hpRatio = ctx != null ? ctx.HpRatio : 0f;
            float hp = ctx != null ? ctx.Hp : 0f;
            float maxHp = ctx != null ? ctx.MaxHp : 1f;

            DrawHpBar(new Rect(x, y, rect.width - 20f, 14f), hpRatio,
                hpRatio > DUELIST_MID_HP ? new Color(0.4f, 0.85f, 0.3f)
                : hpRatio > DUELIST_LOW_HP ? new Color(0.95f, 0.75f, 0.2f)
                : new Color(0.95f, 0.25f, 0.25f));
            y += 16f;
            GUI.Label(new Rect(x, y, rect.width - 20f, 18f),
                $"HP {hp:F0} / {maxHp:F0}  ({hpRatio * 100f:F0}%)", _label);
            y += 22f;

            string mode = InferDuelistMode(ctx, _duelist);
            GUI.Label(new Rect(x, y, rect.width - 20f, 18f), $"Mode: {mode}", _label);
            y += 20f;

            string coverInfo = _duelist.CurrentCover != null
                ? $"Cover: {_duelist.CurrentCover.name}"
                : "Cover: -";
            GUI.Label(new Rect(x, y, rect.width - 20f, 18f), coverInfo, _label);
            y += 18f;

            string visInfo = DescribePerception(ctx, _duelist);
            GUI.Label(new Rect(x, y, rect.width - 20f, 18f), visInfo, _label);
        }

        private void DrawMarksman(Rect rect)
        {
            DrawPanel(rect, new Color(0.05f, 0.08f, 0.18f, 0.85f));
            float x = rect.x + 10f, y = rect.y + 8f;

            GUI.Label(new Rect(x, y, rect.width - 20f, 22f), "MARKSMAN (ranged)", _header);
            y += 24f;

            var ctx = _marksman.Context;
            float hpRatio = ctx != null ? ctx.HpRatio : 0f;
            float hp = ctx != null ? ctx.Hp : 0f;
            float maxHp = ctx != null ? ctx.MaxHp : 1f;

            DrawHpBar(new Rect(x, y, rect.width - 20f, 14f), hpRatio,
                hpRatio > 0.5f ? new Color(0.4f, 0.85f, 0.3f)
                : hpRatio > 0.25f ? new Color(0.95f, 0.75f, 0.2f)
                : new Color(0.95f, 0.25f, 0.25f));
            y += 16f;
            GUI.Label(new Rect(x, y, rect.width - 20f, 18f),
                $"HP {hp:F0} / {maxHp:F0}  ({hpRatio * 100f:F0}%)", _label);
            y += 22f;

            // Ammo bar
            float ammoRatio = _marksman.MaxAmmo > 0 ? (float)_marksman.Ammo / _marksman.MaxAmmo : 0f;
            DrawHpBar(new Rect(x, y, rect.width - 20f, 10f), ammoRatio, new Color(0.8f, 0.8f, 0.3f));
            y += 12f;
            string ammoLabel = _marksman.IsReloading
                ? $"Ammo {_marksman.Ammo}/{_marksman.MaxAmmo}  [RELOADING]"
                : $"Ammo {_marksman.Ammo}/{_marksman.MaxAmmo}";
            GUI.Label(new Rect(x, y, rect.width - 20f, 18f), ammoLabel, _label);
            y += 20f;

            string mode = InferMarksmanMode(ctx);
            GUI.Label(new Rect(x, y, rect.width - 20f, 18f), $"Mode: {mode}", _label);
            y += 20f;

            string coverInfo = _marksman.CurrentCover != null
                ? $"Cover: {_marksman.CurrentCover.name}"
                : "Cover: -";
            GUI.Label(new Rect(x, y, rect.width - 20f, 18f), coverInfo, _label);
            y += 18f;

            string visInfo = DescribePerception(ctx, _marksman);
            GUI.Label(new Rect(x, y, rect.width - 20f, 18f), visInfo, _label);
        }

        // "Target dist:" + perception state in one line, e.g.
        //   "VISIBLE @ 12.4m"  or  "no LOS, last known @ 8.2m"  or  "no LOS, direction only"
        private static string DescribePerception(BTContext ctx, IPerceptionHolder p)
        {
            if (ctx != null && ctx.HasTarget)
                return $"VISIBLE @ {ctx.TargetDistance:F1} m";
            var ps = p?.PerceptionState;
            if (ps != null && ps.HasContact)
            {
                float d = Vector3.Distance(p is MonoBehaviour mb ? mb.transform.position : Vector3.zero,
                                           ps.LastKnownPos.Value);
                return $"no LOS · last known @ {d:F1} m";
            }
            return "no LOS · direction only";
        }

        // -----------------------------------------------------------------
        // Mode inference — mirrors the .When predicates in each Runner's
        // CreateTree. Update both places together if branch logic changes.
        // -----------------------------------------------------------------

        private static string InferDuelistMode(BTContext ctx, IPerceptionHolder p)
        {
            bool visible = ctx != null && ctx.HasTarget;
            if (visible)
            {
                if (IsTargetReloading.Check(ctx)) return "OPPORTUNITY (rush)";
                if (ctx.HpRatio < DUELIST_LOW_HP) return "DEFENSIVE (cover + wait)";
                if (ctx.HpRatio < DUELIST_MID_HP) return "TACTICAL FLANK";
                if (ctx.TargetDistance <= ctx.AttackRange) return "CHARGE";
                return "ADVANCE (visible far)";
            }
            return p?.PerceptionState?.HasContact == true
                ? "INVESTIGATE (last known)"
                : "INVESTIGATE (direction only)";
        }

        private string InferMarksmanMode(BTContext ctx)
        {
            bool visible = ctx != null && ctx.HasTarget;
            if (visible)
            {
                if (!_marksman.IsReloading && _marksman.Ammo > 0 && IsTargetReloading.Check(ctx))
                    return "OPPORTUNITY (careful shot)";

                if (ctx.TargetDistance < MARKSMAN_RETREAT)
                {
                    if (!_marksman.IsReloading && _marksman.Ammo > 0)
                        return "DANGER + SNAP SHOT";
                    return "DANGER (kite)";
                }
                if (_marksman.Ammo <= 0 && !_marksman.IsReloading) return "FORCED RELOAD";
                if (_marksman.IsReloading) return "ONGOING RELOAD";
                bool ammoLow = (float)_marksman.Ammo / Mathf.Max(1, _marksman.MaxAmmo) <= MARKSMAN_LOW_AMMO_RATIO;
                if (ammoLow && ctx.TargetDistance >= MARKSMAN_RETREAT + 2f) return "TACTICAL RELOAD";
                if (_marksman.Ammo > 0 && ctx.TargetDistance <= MARKSMAN_ENGAGE) return "ENGAGE (careful shot)";
                return "ADVANCE (visible far)";
            }
            // Not visible
            if (_marksman.Ammo <= 0 && !_marksman.IsReloading) return "FORCED RELOAD";
            if (_marksman.IsReloading) return "ONGOING RELOAD";
            return _marksman.PerceptionState?.HasContact == true
                ? "INVESTIGATE (last known)"
                : "INVESTIGATE (direction only)";
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private void EnsureRefs()
        {
            if (_duelist == null)
                _duelist = Object.FindAnyObjectByType<DuelistRunner>();
            if (_marksman == null)
                _marksman = Object.FindAnyObjectByType<MarksmanRunner>();
        }

        private void EnsureStyles()
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(1, 1);
                _whiteTex.SetPixel(0, 0, Color.white);
                _whiteTex.Apply();
            }
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    normal = { textColor = Color.white },
                };
            }
            if (_header == null)
            {
                _header = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                };
            }
        }

        private void DrawPanel(Rect rect, Color bg)
        {
            var prev = GUI.color;
            GUI.color = bg;
            GUI.DrawTexture(rect, _whiteTex);
            GUI.color = prev;
        }

        private void DrawHpBar(Rect rect, float ratio01, Color fill)
        {
            ratio01 = Mathf.Clamp01(ratio01);
            var prev = GUI.color;

            // Background
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            GUI.DrawTexture(rect, _whiteTex);

            // Fill
            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * ratio01, rect.height), _whiteTex);

            GUI.color = prev;
        }
    }
}
