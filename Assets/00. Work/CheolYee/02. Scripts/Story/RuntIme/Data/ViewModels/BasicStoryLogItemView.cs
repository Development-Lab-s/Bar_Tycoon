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

            if (choiceBadgeRoot != null)
                choiceBadgeRoot.SetActive(isChoice);

            if (choiceBadgeText != null)
                choiceBadgeText.text = choiceLabel;

            if (iconRoot != null)
                iconRoot.SetActive(hasSpeaker && !isChoice && !isNarration);

            if (iconImage != null)
            {
                if (hasSpeaker && entry.speaker.LogIcon != null && !isChoice && !isNarration)
                {
                    iconImage.sprite = entry.speaker.LogIcon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }
            }

            if (nameRoot != null)
                nameRoot.SetActive(!isChoice);

            if (nameText != null)
            {
                if (isChoice)
                {
                    nameText.text = string.Empty;
                }
                else if (isNarration)
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
