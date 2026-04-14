using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using Alchemy.Inspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Skip
{
    public sealed class StorySkipSummaryPanelController : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private CanvasGroup overlayCanvasGroup;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text episodeTitleText;
        [SerializeField] private TMP_Text summaryText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button confirmSkipButton;

        [Header("Fallback Text")]
        [SerializeField] private string defaultTitle = "이 장면을 건너뛸까요?";
        [SerializeField] private string emptyEpisodeTitle = "에피소드";
        [SerializeField] private string emptySummaryText = "요약 정보가 아직 없습니다.";

        public bool IsOpen { get; private set; }

        public event Action CloseRequested;
        public event Action ConfirmSkipRequested;
        
        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(HandleClosePressed);

            if (confirmSkipButton != null)
                confirmSkipButton.onClick.AddListener(HandleConfirmSkipPressed);

            ApplyClosedStateImmediate();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleClosePressed);

            if (confirmSkipButton != null)
                confirmSkipButton.onClick.RemoveListener(HandleConfirmSkipPressed);
        }

        public void Open(StoryEpisodeSO episode)
        {
            BindEpisode(episode);

            IsOpen = true;

            if (overlayRoot != null)
                overlayRoot.SetActive(true);

            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 1f;
                overlayCanvasGroup.interactable = true;
                overlayCanvasGroup.blocksRaycasts = true;
            }
        }

        public void CloseImmediate()
        {
            IsOpen = false;
            ApplyClosedStateImmediate();
        }

        private void BindEpisode(StoryEpisodeSO episode)
        {
            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(defaultTitle) ? "이 스토리를 건너뛸까요?" : defaultTitle;

            if (episodeTitleText != null)
            {
                episodeTitleText.text =
                    episode != null && !string.IsNullOrWhiteSpace(episode.Title)
                        ? episode.Title
                        : emptyEpisodeTitle;
            }

            if (summaryText != null)
            {
                summaryText.text =
                    episode != null && !string.IsNullOrWhiteSpace(episode.SkipSummary)
                        ? episode.SkipSummary
                        : emptySummaryText;
            }
        }

        private void ApplyClosedStateImmediate()
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

        private void HandleClosePressed()
        {
            CloseRequested?.Invoke();
        }

        private void HandleConfirmSkipPressed()
        {
            ConfirmSkipRequested?.Invoke();
        }
    }
}