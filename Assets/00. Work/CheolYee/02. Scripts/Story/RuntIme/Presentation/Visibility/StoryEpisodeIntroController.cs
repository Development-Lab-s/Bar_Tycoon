using System;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Visibility
{
    public sealed class StoryEpisodeIntroController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private TMP_Text episodeTitleText;
        [SerializeField] private StoryAspectSettingsSO aspectSettings;

        private string _defaultTitleText = string.Empty;

        private void Awake()
        {
            if (episodeTitleText != null)
                _defaultTitleText = episodeTitleText.text;

            ApplyHiddenImmediate();
        }

        public void ApplyHiddenImmediate()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public async UniTask PlayAsync(StoryEpisodeSO episode, CancellationToken ct)
        {
            if (panelCanvasGroup == null)
                return;

            BindEpisodeTitle(episode);

            if (panelRoot != null)
                panelRoot.SetActive(true);

            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.alpha = 0f;

            float fadeDuration = aspectSettings != null ? aspectSettings.FadeDuration : 0.35f;
            float holdDuration = aspectSettings != null ? aspectSettings.TitleHoldDuration : 1.2f;

            await FadeCanvasAsync(0f, 1f, fadeDuration, ct);
            if (holdDuration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, ct);
            await FadeCanvasAsync(1f, 0f, fadeDuration, ct);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void BindEpisodeTitle(StoryEpisodeSO episode)
        {
            if (episodeTitleText == null)
                return;

            if (episode != null && !string.IsNullOrWhiteSpace(episode.Title))
                episodeTitleText.text = episode.Title;
            else
                episodeTitleText.text = _defaultTitleText;
        }

        private async UniTask FadeCanvasAsync(float from, float to, float duration, CancellationToken ct)
        {
            panelCanvasGroup.alpha = from;
            if (duration <= 0f)
            {
                panelCanvasGroup.alpha = to;
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                panelCanvasGroup.alpha = Mathf.Lerp(from, to, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            panelCanvasGroup.alpha = to;
        }
    }
}
