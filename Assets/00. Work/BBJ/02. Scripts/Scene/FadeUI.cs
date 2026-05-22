using System.Collections;
using UnityEngine;

namespace BBJ.Scene
{
    public class FadeUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float       _duration = 0.3f;

        private void Awake()
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        public IEnumerator FadeOut()
        {
            _canvasGroup.blocksRaycasts = true;
            yield return Fade(0f, 1f);
        }

        public IEnumerator FadeIn()
        {
            yield return Fade(1f, 0f);
            _canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            _canvasGroup.alpha = from;
            while (elapsed < _duration)
            {
                elapsed           += Time.unscaledDeltaTime;
                _canvasGroup.alpha  = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / _duration));
                yield return null;
            }
            _canvasGroup.alpha = to;
        }
    }
}
