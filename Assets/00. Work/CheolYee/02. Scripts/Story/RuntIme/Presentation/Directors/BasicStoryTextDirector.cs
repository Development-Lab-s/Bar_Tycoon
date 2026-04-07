using System;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    public sealed class BasicStoryTextDirector : MonoBehaviour, ITextDirector
    {
        [Header("UI")]
        [SerializeField] private GameObject nameRoot;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text dialogueText;

        [Header("Typing")]
        [SerializeField] private float baseCharacterDelay = 0.03f;
        [SerializeField] private Vector2Int randomDelayMsRange = new Vector2Int(-5, 5);
        [SerializeField] private float punctuationDelay = 0.40f;
        [SerializeField] private float commaDelay = 0.20f;
        [SerializeField] private float lineBreakDelay = 0.50f;
        [SerializeField] private float spaceDelay = 0.05f;

        private bool _completeRequested;

        public bool IsTyping { get; private set; }

        public async UniTask PlayLineAsync(StoryLineSO line, CancellationToken ct)
        {
            if (line == null)
            {
                Clear();
                return;
            }

            SetupSpeaker(line);
            SetupDialogue(line);

            _completeRequested = false;
            IsTyping = true;

            try
            {
                dialogueText.ForceMeshUpdate();
                int characterCount = dialogueText.textInfo.characterCount;

                if (characterCount <= 0)
                {
                    dialogueText.maxVisibleCharacters = 0;
                    return;
                }

                dialogueText.maxVisibleCharacters = 0;

                for (int i = 0; i < characterCount; i++)
                {
                    if (_completeRequested || !line.AllowTapToComplete && _completeRequested)
                        break;

                    dialogueText.maxVisibleCharacters = i + 1;

                    char currentChar = dialogueText.textInfo.characterInfo[i].character;
                    float delay = CalculateDelay(currentChar);

                    if (_completeRequested)
                        break;

                    if (delay > 0f)
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(delay),
                            DelayType.UnscaledDeltaTime,
                            PlayerLoopTiming.Update,
                            ct);
                    }
                    else
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    }
                }

                dialogueText.maxVisibleCharacters = int.MaxValue;
            }
            finally
            {
                IsTyping = false;
                _completeRequested = false;
            }
        }

        public void CompleteCurrentLine()
        {
            if (!IsTyping)
                return;

            _completeRequested = true;

            if (dialogueText != null)
                dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        public void Clear()
        {
            IsTyping = false;
            _completeRequested = false;

            if (nameText != null)
                nameText.text = string.Empty;

            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
                dialogueText.maxVisibleCharacters = 0;
            }

            if (nameRoot != null)
                nameRoot.SetActive(false);
        }

        private void SetupSpeaker(StoryLineSO line)
        {
            bool isNarration = line.IsNarration();

            if (nameRoot != null)
                nameRoot.SetActive(!isNarration);

            if (!isNarration && nameText != null)
                nameText.text = line.GetResolvedSpeakerName();
            else if (nameText != null)
                nameText.text = string.Empty;
        }

        private void SetupDialogue(StoryLineSO line)
        {
            if (dialogueText == null)
                return;

            dialogueText.text = line.DialogueText ?? string.Empty;
            dialogueText.maxVisibleCharacters = 0;
        }

        private float CalculateDelay(char ch)
        {
            int randomMs = UnityEngine.Random.Range(randomDelayMsRange.x, randomDelayMsRange.y + 1);
            float delay = baseCharacterDelay + randomMs / 1000f;

            switch (ch)
            {
                case '!':
                case '?':
                case '.':
                    delay += punctuationDelay;
                    break;

                case ',':
                    delay += commaDelay;
                    break;

                case '\n':
                    delay += lineBreakDelay;
                    break;

                case ' ':
                    delay += spaceDelay;
                    break;
            }

            if (delay < 0f)
                delay = 0f;

            return delay;
        }
    }
}