using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sean.Statistic
{
    public class StatFieldUi : MonoBehaviour
    {
        [Header("Stat Field Base")]
        [SerializeField] private Image _statImage;
        [SerializeField] private float _shadowAlpha = 0.5f;

        private Image StatImage
        {
            get => _statImage;
            set => _statImage = value;
        }

        private Image BarShadow { get; set; }

        private Coroutine _mainCo;
        private Coroutine _shadowCo;

        private void Awake()
        {
            InstantiateBarShadow();
        }

        public void UiUpdate(float current, float max)
        {
            var target = current / max;

            // Slowly increase
            if (target > _statImage.fillAmount)
            {
                StartSmoothFill(StatImage, current, max, _mainCo);
                StartSmoothFill(BarShadow, current, max, _shadowCo);
            }
            // Instant decrease with shadow slowly following
            else
            {
                StatImage.fillAmount = target;
                StartSmoothFill(BarShadow, current, max, _shadowCo);
            }
        }

        private void StartSmoothFill(Image bar, float current, float max, Coroutine co, float duration = 1f)
        {
            if (co != null)
                StopCoroutine(co);

            co = StartCoroutine(SmoothFill());

            IEnumerator SmoothFill()
            {
                var currentFill = bar.fillAmount;
                var endFill = current / max;

                for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
                {
                    currentFill = Mathf.Lerp(currentFill, endFill, Mathf.Clamp01(t / duration));
                    bar.fillAmount = currentFill;

                    yield return null;
                }

                bar.fillAmount = endFill;
            }
        }

        // Clone bar and set alpha 
        private void InstantiateBarShadow()
        {
            BarShadow = Instantiate(_statImage, _statImage.transform.parent);

            var targetColor = new Color(_statImage.color.r, _statImage.color.g, _statImage.color.b, _shadowAlpha);
            BarShadow.color = targetColor;
            BarShadow.transform.SetAsLastSibling();
        }
    }
}