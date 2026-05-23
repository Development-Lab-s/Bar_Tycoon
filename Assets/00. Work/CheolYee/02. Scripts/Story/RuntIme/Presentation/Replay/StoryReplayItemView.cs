using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Replay
{
    public sealed class StoryReplayItemView : MonoBehaviour
    {
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button replayButton;

        private Action _onReplayClicked;

        public void Bind(StoryEpisodeCatalogEntry entry, Action onReplayClicked)
        {
            _onReplayClicked = onReplayClicked;

            if (thumbnailImage != null)
                thumbnailImage.sprite = entry.Thumbnail;

            if (titleText != null)
                titleText.text = entry.Episode != null ? entry.Episode.Title : string.Empty;

            if (replayButton != null)
            {
                replayButton.onClick.RemoveAllListeners();
                replayButton.onClick.AddListener(OnReplayButtonClicked);
            }
            else
            {
                Debug.LogWarning("[StoryReplayItemView] replayButton이 없습니다.", this);
            }
        }

        private void OnDestroy()
        {
            if (replayButton != null)
                replayButton.onClick.RemoveAllListeners();
        }

        private void OnReplayButtonClicked()
        {
            _onReplayClicked?.Invoke();
        }
    }
}
