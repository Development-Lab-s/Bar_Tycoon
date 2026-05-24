using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.RuntimeModules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.ViewModels
{
    public sealed class BasicStoryLogItemView : MonoBehaviour
    {
        [Header("Optional Roots")]
        [SerializeField] private GameObject iconRoot;
        [SerializeField] private GameObject nameRoot;
        [SerializeField] private GameObject choiceBadgeRoot;

        [Header("UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text choiceBadgeText;
        [SerializeField] private TMP_Text sequenceText;

        [Header("Fallback")]
        [SerializeField] private string narrationLabel = "나레이션";
        [SerializeField] private string choiceLabel = "선택";

        public void Bind(StoryLogEntry entry)
        {
            if (entry == null)
                return;

            if (bodyText != null) 
                bodyText.text = entry.text ?? string.Empty;

            if (sequenceText != null)
                sequenceText.text = entry.sequence.ToString();

            bool isChoice = entry.entryType == StoryLogEntryType.ChoiceResult;
            bool isNarration = entry.entryType == StoryLogEntryType.Narration;
            bool hasSpeaker = entry.speaker != null;

            // ── 선택지 뱃지 ────────────────────
            if (choiceBadgeRoot != null)
                choiceBadgeRoot.SetActive(isChoice);

            if (choiceBadgeText != null)
                choiceBadgeText.text = choiceLabel;

            // ── 아이콘 ─────────────────────────
            bool hasIcon = hasSpeaker && entry.speaker.LogIcon != null && !isChoice && !isNarration;

            if (iconRoot != null)
                iconRoot.SetActive(hasIcon);

            if (iconImage != null)
            {
                iconImage.sprite = hasIcon ? entry.speaker.LogIcon : null;
                iconImage.enabled = hasIcon;
            }

            // ── 이름 영역 ─────────────────────
            if (nameRoot != null)
                nameRoot.SetActive(true);

            if (nameText != null)
            {
                if (isChoice)
                {
                    nameText.gameObject.SetActive(false);
                }
                else
                {
                    nameText.gameObject.SetActive(true);

                    if (isNarration)
                    {
                        nameText.text = narrationLabel;
                    }
                    else
                    {
                        nameText.text = string.IsNullOrWhiteSpace(entry.displayName)
                            ? (hasSpeaker ? entry.speaker.DisplayName : string.Empty)
                            : entry.displayName;
                    }
                }
            }
        }
    }
}