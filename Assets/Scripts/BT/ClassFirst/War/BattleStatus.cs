using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.ClassFirst.War
{
    /// <summary>
    /// Scans every BaseNPCRunner in the scene once per second, groups by Faction
    /// (= Team) and per-class (Warrior/Knight/Archer/Spearman/Healer), and
    /// renders the count via OnGUI. Logs the winner once one side is fully wiped.
    /// </summary>
    public sealed class BattleStatus : MonoBehaviour
    {
        [SerializeField] private float _refreshInterval = 0.5f;

        private float _accum;
        private readonly Dictionary<Faction, Dictionary<System.Type, int>> _counts = new();
        private bool _gameOver;
        private Faction _winner;
        private float _elapsed;

        private void Update()
        {
            _elapsed += Time.deltaTime;
            _accum += Time.deltaTime;
            if (_accum < _refreshInterval) return;
            _accum = 0f;
            Recount();
        }

        private void Recount()
        {
            _counts.Clear();
            var all = FindObjectsByType<BaseNPCRunner>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            int teamATotal = 0, teamBTotal = 0;
            foreach (var n in all)
            {
                if (!n.IsAlive) continue;
                if (!_counts.TryGetValue(n.Faction, out var byType))
                {
                    byType = new Dictionary<System.Type, int>();
                    _counts[n.Faction] = byType;
                }
                var t = n.GetType();
                byType.TryGetValue(t, out int c);
                byType[t] = c + 1;

                if (n.Faction == Faction.TeamA) teamATotal++;
                else if (n.Faction == Faction.TeamB) teamBTotal++;
            }

            if (!_gameOver)
            {
                if (teamATotal == 0 && teamBTotal > 0)
                {
                    _gameOver = true;
                    _winner = Faction.TeamB;
                    Debug.Log($"[BattleStatus] TeamB WINS in {_elapsed:F1}s — {teamBTotal} survivors.");
                }
                else if (teamBTotal == 0 && teamATotal > 0)
                {
                    _gameOver = true;
                    _winner = Faction.TeamA;
                    Debug.Log($"[BattleStatus] TeamA WINS in {_elapsed:F1}s — {teamATotal} survivors.");
                }
                else if (teamATotal == 0 && teamBTotal == 0)
                {
                    _gameOver = true;
                    Debug.Log($"[BattleStatus] Mutual annihilation in {_elapsed:F1}s.");
                }
            }
        }

        private void OnGUI()
        {
            const int W = 240, H = 200, M = 10;
            // TeamA box (top-left)
            DrawTeamBox(new Rect(M, M, W, H), Faction.TeamA, "Team A", new Color(0.85f, 0.15f, 0.15f, 0.85f));
            // TeamB box (top-right)
            DrawTeamBox(new Rect(Screen.width - W - M, M, W, H), Faction.TeamB, "Team B", new Color(0.2f, 0.4f, 0.9f, 0.85f));

            // Status line
            string status = _gameOver
                ? $"<b><color=yellow>GAME OVER — {_winner} WINS @ {_elapsed:F1}s</color></b>"
                : $"<b>Elapsed:</b> {_elapsed:F1}s";
            GUI.Label(new Rect(Screen.width / 2 - 200, M, 400, 30),
                $"<size=18>{status}</size>", LabelStyle());
        }

        private void DrawTeamBox(Rect rect, Faction faction, string title, Color tint)
        {
            GUI.color = tint;
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;

            var inner = new Rect(rect.x + 8, rect.y + 6, rect.width - 16, rect.height - 12);
            GUI.Label(inner, $"<b><size=18>{title}</size></b>", LabelStyle());

            int y = 28;
            int total = 0;
            if (_counts.TryGetValue(faction, out var byType))
            {
                foreach (var kv in byType)
                {
                    string shortName = kv.Key.Name.Replace("Runner", "");
                    GUI.Label(new Rect(inner.x + 10, inner.y + y, inner.width - 20, 22),
                        $"<size=14>{shortName,-10} × {kv.Value}</size>", LabelStyle());
                    total += kv.Value;
                    y += 22;
                }
            }
            GUI.Label(new Rect(inner.x + 10, inner.y + inner.height - 28, inner.width - 20, 24),
                $"<b><size=16>Total: {total}</size></b>", LabelStyle());
        }

        private static GUIStyle _labelStyle;
        private static GUIStyle LabelStyle()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label) { richText = true };
                _labelStyle.normal.textColor = Color.white;
            }
            return _labelStyle;
        }
    }
}
