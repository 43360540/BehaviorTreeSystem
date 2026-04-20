using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sean.Ui
{
    public static class UiTool
    {
        private static readonly Dictionary<CanvasGroup, CancellationTokenSource> _fadeTokens = new();

        public static async UniTask Fade(CanvasGroup group, float targetAlpha, float duration)
        {
            if (group == null) return;

            if (_fadeTokens.TryGetValue(group, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
                _fadeTokens.Remove(group);
            }

            if (duration <= 0)
            {
                group.alpha = targetAlpha;
                return;
            }

            var cts = new CancellationTokenSource();
            _fadeTokens[group] = cts;
            var token = cts.Token;

            float startAlpha = group.alpha;
            float elapsed = 0;

            try
            {
                while (elapsed < duration)
                {
                    if (token.IsCancellationRequested) return;

                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

                    await UniTask.WaitForEndOfFrame(token);
                }

                group.alpha = targetAlpha;
            }
            finally
            {
                if (_fadeTokens.TryGetValue(group, out var current) && current == cts)
                {
                    _fadeTokens.Remove(group);
                }

                cts.Dispose();
            }
        }

        public static void Set(CanvasGroup cg, bool enable, Vector2 position, float duration = 0f)
        {
            if (cg == null) return;
            if (!cg.TryGetComponent<RectTransform>(out var target))
                return;

            target.anchoredPosition = position;

            if (enable)
            {
                Fade(cg, 1, duration).Forget();

                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            else
            {
                Fade(cg, 0, duration).Forget();

                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        public static void Set(CanvasGroup cg, bool enable, float duration = 0f)
        {
            if (cg == null) return;
            if (!cg.TryGetComponent<RectTransform>(out var target))
                return;

            if (enable)
            {
                Fade(cg, 1, duration).Forget();

                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            else
            {
                Fade(cg, 0, duration).Forget();

                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }
    }
}