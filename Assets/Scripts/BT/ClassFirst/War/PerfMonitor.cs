using UnityEngine;

namespace BehaviorTree.ClassFirst.War
{
    /// <summary>
    /// Minimal FPS / frame-time HUD for the War demo. Renders a small panel at
    /// the bottom-left with smoothed FPS and per-frame ms.
    /// </summary>
    public sealed class PerfMonitor : MonoBehaviour
    {
        [SerializeField] private float _smoothing = 0.9f;

        private float _smoothedDt = 1f / 60f;
        private float _minFps = float.PositiveInfinity;
        private float _maxFps = 0f;
        private float _resetTimer;
        private float _npcCountTimer;
        private int _cachedNpcCount;
        private const float RESET_EVERY = 5f;
        private const float NPC_COUNT_REFRESH = 0.5f;

        private void Update()
        {
            // Exponential smoothing.
            _smoothedDt = _smoothedDt * _smoothing + Time.unscaledDeltaTime * (1f - _smoothing);

            float fps = 1f / Time.unscaledDeltaTime;
            if (fps < _minFps) _minFps = fps;
            if (fps > _maxFps) _maxFps = fps;

            _resetTimer += Time.unscaledDeltaTime;
            if (_resetTimer >= RESET_EVERY)
            {
                _resetTimer = 0f;
                _minFps = float.PositiveInfinity;
                _maxFps = 0f;
            }

            // Refresh NPC count at most twice per second to keep OnGUI cheap.
            _npcCountTimer += Time.unscaledDeltaTime;
            if (_npcCountTimer >= NPC_COUNT_REFRESH)
            {
                _npcCountTimer = 0f;
                _cachedNpcCount = FindObjectsByType<BaseNPCRunner>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            }
        }

        private void OnGUI()
        {
            const int W = 230, H = 90, M = 10;
            var rect = new Rect(M, Screen.height - H - M, W, H);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;

            float avgFps = 1f / Mathf.Max(_smoothedDt, 1e-6f);
            float ms = _smoothedDt * 1000f;

            var s = new GUIStyle(GUI.skin.label) { richText = true };
            s.normal.textColor = Color.white;

            GUI.Label(new Rect(rect.x + 8, rect.y + 4, W - 16, 22),
                $"<b><size=16>FPS:</size></b> <size=18>{avgFps:F1}</size>", s);
            GUI.Label(new Rect(rect.x + 8, rect.y + 26, W - 16, 22),
                $"<size=13>Frame: {ms:F2} ms</size>", s);
            GUI.Label(new Rect(rect.x + 8, rect.y + 46, W - 16, 22),
                $"<size=13>Min/Max (5s): {(_minFps == float.PositiveInfinity ? 0 : _minFps):F0} / {_maxFps:F0}</size>", s);
            GUI.Label(new Rect(rect.x + 8, rect.y + 66, W - 16, 22),
                $"<size=11 color=#aaa>Alive NPCs: {_cachedNpcCount}</size>", s);
        }
    }
}
