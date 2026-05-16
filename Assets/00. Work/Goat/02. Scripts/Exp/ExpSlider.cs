using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.Exp
{
    public class ExpSlider : MonoBehaviour
    {
        [SerializeField] private Image expSliderImage;
        [SerializeField] private float smoothTime = 0.25f;
        
        private Coroutine _fillCoroutine;

        public void SetFill(int amount, int max)
        {
            if (max <= 0)
            {
                expSliderImage.fillAmount = 1f;
                return;
            }

            expSliderImage.fillAmount = Mathf.Clamp01((float)amount / max);
        }

        public void SetSmoothFill(int levelUpCount, int amount, int max)
        {
            float targetFill = GetFillAmount(amount, max);

            if (_fillCoroutine != null)
            {
                StopCoroutine(_fillCoroutine);
            }

            if (levelUpCount > 0)
            {
                _fillCoroutine = StartCoroutine(LevelUpSmoothFill(levelUpCount, targetFill));
            }
            else
            {
                _fillCoroutine = StartCoroutine(SmoothFill(targetFill));
            }
        }
        
        private float GetFillAmount(int amount, int max)
        {
            if (max <= 0)
            {
                return 1f;
            }

            return Mathf.Clamp01((float)amount / max);
        }
        
        private IEnumerator SmoothFill(float targetFill)
        {
            yield return SmoothFillRoutine(targetFill);
            _fillCoroutine = null;
        }
        
        private IEnumerator LevelUpSmoothFill(int levelUpCount, float finalTargetFill)
        {
            for (int i = 0; i < levelUpCount; i++)
            {
                yield return SmoothFillRoutine(1f);
                expSliderImage.fillAmount = 0f;
            }

            yield return SmoothFillRoutine(finalTargetFill);

            _fillCoroutine = null;
        }
        
        private IEnumerator SmoothFillRoutine(float targetFill)
        {
            float startFill = expSliderImage.fillAmount;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, smoothTime);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);

                expSliderImage.fillAmount = Mathf.Lerp(startFill, targetFill, t);
                
                yield return null;
            }

            expSliderImage.fillAmount = targetFill;
        }
    }
}