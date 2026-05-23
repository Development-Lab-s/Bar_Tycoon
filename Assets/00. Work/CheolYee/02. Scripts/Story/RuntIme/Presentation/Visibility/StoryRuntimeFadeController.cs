using System;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Visibility
{
    public sealed class StoryRuntimeFadeController : MonoBehaviour, IStoryRuntimeFadeController
    {
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private StoryAspectSettingsSO aspectSettings;

        public float CurrentAlpha => overlayCanvasGroup != null ? overlayCanvasGroup.alpha : 0f;

        private void Awake()
        {
            ApplyClearImmediate();
        }

        public void ApplyBlackImmediate()
        {
            if (overlayRoot != null)
                overlayRoot.SetActive(true);

            if (overlayCanvasGroup == null)
                return;

            overlayCanvasGroup.alpha = 1f;
            overlayCanvasGroup.interactable = true;
            overlayCanvasGroup.blocksRaycasts = true;
        }

        public void ApplyClearImmediate()
        {
            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 0f;
                overlayCanvasGroup.interactable = false;
                overlayCanvasGroup.blocksRaycasts = false;
            }

            if (overlayRoot != null)
                overlayRoot.SetActive(false);
        }

        public async UniTask PlayFadeAsync(StoryFadeDirection direction, CancellationToken ct)
        {
            float target = direction == StoryFadeDirection.FadeIn ? 1f : 0f;
            await FadeToAlphaAsync(target, ResolveFadeDuration(), ct);
        }

        private async UniTask FadeToAlphaAsync(float targetAlpha, float duration, CancellationToken ct)
        {
            if (overlayCanvasGroup == null)
                return;

            if (overlayRoot != null)
                overlayRoot.SetActive(true);

            float startAlpha = overlayCanvasGroup.alpha;
            SetBlocksRaycasts(true);

            if (duration <= 0f)
            {
                overlayCanvasGroup.alpha = targetAlpha;
                SetBlocksRaycasts(targetAlpha > 0.001f);
                if (targetAlpha <= 0.001f && overlayRoot != null)
                    overlayRoot.SetActive(false);
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                overlayCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            overlayCanvasGroup.alpha = targetAlpha;
            SetBlocksRaycasts(targetAlpha > 0.001f);
            if (targetAlpha <= 0.001f && overlayRoot != null)
                overlayRoot.SetActive(false);
        }

        private float ResolveFadeDuration()
        {
            return aspectSettings != null ? aspectSettings.FadeDuration : 0.35f;
        }

        private void SetBlocksRaycasts(bool value)
        {
            if (overlayCanvasGroup == null)
                return;

            overlayCanvasGroup.interactable = value;
            overlayCanvasGroup.blocksRaycasts = value;
        }
    }
}
