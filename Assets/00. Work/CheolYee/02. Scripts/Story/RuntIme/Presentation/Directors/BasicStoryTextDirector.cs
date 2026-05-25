using System;
using System.Text.RegularExpressions;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Leaderboards;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    public sealed class BasicStoryTextDirector : MonoBehaviour, ITextDirector
    {
        private const string PlayerPlaceholder = "{player}";

        [Header("UI")]
        [SerializeField] private GameObject nameRoot;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Image speakerColorImage;

        [Header("Typing")]
        [SerializeField] private float baseCharacterDelay = 0.03f;
        [SerializeField] private Vector2Int randomDelayMsRange = new Vector2Int(-5, 5);
        [SerializeField] private float punctuationDelay = 0.40f;
        [SerializeField] private float commaDelay = 0.20f;
        [SerializeField] private float lineBreakDelay = 0.50f;
        [SerializeField] private float spaceDelay = 0.05f;

        [Header("Typing Sound")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SfxSounds[] typingSounds = Array.Empty<SfxSounds>();
        [SerializeField] private float typingSoundMinInterval = 0.05f;

        private bool _completeRequested;
        private bool _playerNameLoaded;
        private string _cachedPlayerName = string.Empty;
        private float _lastTypingSoundTime;

        public bool IsTyping { get; private set; }

        private void Awake()
        {
            ResolveSpeakerColorImage();
        }

        public async UniTask PlayLineAsync(StoryLineSO line, CancellationToken ct)
        {
            if (line == null)
            {
                Clear();
                return;
            }

            string playerName = await ResolvePlayerNameIfNeededAsync(line, ct);

            SetupSpeaker(line, playerName);
            SetupDialogue(line, playerName);

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
                    PlayTypingSoundIfNeeded(currentChar);
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

            if (speakerColorImage != null)
                speakerColorImage.color = Color.white;

            if (nameRoot != null)
                nameRoot.SetActive(false);
        }

        private void SetupSpeaker(StoryLineSO line, string playerName)
        {
            bool isNarration = line.IsNarration();

            if (nameRoot != null)
                nameRoot.SetActive(!isNarration);

            if (!isNarration && nameText != null)
                nameText.text = ReplacePlayerPlaceholder(line.GetResolvedSpeakerName(), playerName);
            else if (nameText != null)
                nameText.text = string.Empty;

            if (speakerColorImage != null)
                speakerColorImage.color = !isNarration && line.Speaker != null
                    ? line.Speaker.PersonalColor
                    : Color.white;
        }

        private void SetupDialogue(StoryLineSO line, string playerName)
        {
            if (dialogueText == null)
                return;

            dialogueText.text = ReplacePlayerPlaceholder(line.DialogueText ?? string.Empty, playerName);
            dialogueText.maxVisibleCharacters = 0;
        }

        private async UniTask<string> ResolvePlayerNameIfNeededAsync(StoryLineSO line, CancellationToken ct)
        {
            bool needsPlayerName = ContainsPlayerPlaceholder(line?.GetResolvedSpeakerName())
                                   || ContainsPlayerPlaceholder(line?.DialogueText);

            if (!needsPlayerName)
                return string.Empty;

            if (_playerNameLoaded && !string.IsNullOrWhiteSpace(_cachedPlayerName))
                return _cachedPlayerName;

            if (!await EnsureUnityServicesReadyAsync())
                return string.Empty;

            try
            {
                string playerName = AuthenticationService.Instance.PlayerName;

                if (string.IsNullOrWhiteSpace(playerName))
                    playerName = await AuthenticationService.Instance.GetPlayerNameAsync(false);

                playerName = SanitizePlayerName(playerName);

                if (string.IsNullOrWhiteSpace(playerName))
                {
                    Debug.LogWarning("[BasicStoryTextDirector] 플레이어 이름이 비어 있습니다.", this);
                    return string.Empty;
                }

                _cachedPlayerName = playerName;
                _playerNameLoaded = true;

                return _cachedPlayerName;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BasicStoryTextDirector] 플레이어 이름 조회 실패: {ex.Message}", this);
                return string.Empty;
            }
        }

        private async UniTask<bool> EnsureUnityServicesReadyAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    Debug.LogWarning($"[BasicStoryTextDirector] Unity Services 초기화 실패 또는 진행 중: {UnityServices.State}", this);
                    return false;
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                return AuthenticationService.Instance.IsSignedIn;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BasicStoryTextDirector] Unity Services 준비 실패: {ex.Message}", this);
                return false;
            }
        }
        
        private void ResolveSpeakerColorImage()
        {
            if (speakerColorImage != null || nameRoot == null)
                return;

            Image[] images = nameRoot.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image != null)
                {
                    speakerColorImage = image;
                    break;
                }
            }
        }

        private static bool ContainsPlayerPlaceholder(string text) =>
            !string.IsNullOrEmpty(text) && text.Contains(PlayerPlaceholder, StringComparison.Ordinal);

        private static string ReplacePlayerPlaceholder(string text, string playerName)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(playerName))
                return text;

            return text.Replace(PlayerPlaceholder, playerName, StringComparison.Ordinal);
        }

        private static string SanitizePlayerName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return string.Empty;

            return Regex.IsMatch(playerName, @"#\d{4}$")
                ? playerName.Substring(0, playerName.Length - 5)
                : playerName;
        }

        private void PlayTypingSoundIfNeeded(char ch)
        {
            if (soundChannel == null || typingSounds == null || typingSounds.Length == 0)
                return;

            if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || char.IsSymbol(ch))
                return;

            float now = Time.unscaledTime;
            if (now - _lastTypingSoundTime < typingSoundMinInterval)
                return;

            _lastTypingSoundTime = now;
            SfxSounds chosen = typingSounds[UnityEngine.Random.Range(0, typingSounds.Length)];
            soundChannel.RaiseEvent(new PlaySoundEvent(chosen, Vector3.zero));
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
