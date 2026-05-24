using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.RuntimeModules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.ViewModels;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Motion;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    public sealed class BasicStoryLogController : MonoBehaviour, IStoryLogController
    {
        
        private const string PlayerPlaceholder = "{player}";

        
        [Header("UI")]
        [SerializeField] private GameObject logAreaRoot;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private GameObject logItemPrefab;

        [Header("Modal Blocking")]
        [SerializeField] private CanvasGroup logCanvasGroup;

        [Header("Motion (Optional)")]
        [SerializeField] private UIMotionPlayer motionPlayer;

        [Header("Auto Scroll")]
        [SerializeField] private ScrollRect scrollRect;

        private readonly List<StoryLogEntry> _entries = new();
        private readonly List<GameObject> _spawnedItems = new();
        private bool _playerNameLoaded;
        private string _cachedPlayerName = string.Empty;

        public bool IsOpen { get; private set; }
        
        private void Start()
        {
            CachePlayerNameAsync().Forget();
        }

        // ── IStoryLogController ────────────────

        public void AppendLine(StoryLogEntry entry)
        {
            if (entry == null)
                return;

            _entries.Add(entry);
            TryCreateItem(entry);
        }

        public void AppendChoiceResult(StoryLogEntry entry)
        {
            if (entry == null)
                return;

            _entries.Add(entry);
            TryCreateItem(entry);
        }

        public void Open()
        {
            if (IsOpen)
                return;

            IsOpen = true;

            if (logAreaRoot != null)
                logAreaRoot.SetActive(true);

            RebuildIfNeeded();
            ForceLayoutUpdate();
            ScrollToBottom();

            if (motionPlayer != null)
            {
                SetCanvasGroupInteractable(false);
                motionPlayer.Play("Show",
                    onComplete: () => SetCanvasGroupInteractable(true));
            }
            else
            {
                ApplyOpenedState();
            }
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            if (motionPlayer != null)
            {
                SetCanvasGroupInteractable(false);
                motionPlayer.Play("Hide",
                    onFinish: CloseImmediate);
            }
            else
            {
                CloseImmediate();
            }
        }

        public void Clear()
        {
            _entries.Clear();

            foreach (var item in _spawnedItems)
            {
                if (item != null)
                    Destroy(item);
            }

            _spawnedItems.Clear();
        }

        // ── State ─────────────────────────────

        private void CloseImmediate()
        {
            IsOpen = false;

            if (motionPlayer != null)
                motionPlayer.ApplyState("Hide");

            SetCanvasGroupInteractable(false);

            if (logAreaRoot != null)
                logAreaRoot.SetActive(false);
        }

        private void ApplyOpenedState()
        {
            if (logCanvasGroup != null)
            {
                logCanvasGroup.alpha = 1f;
                logCanvasGroup.interactable = true;
                logCanvasGroup.blocksRaycasts = true;
            }
        }

        private void SetCanvasGroupInteractable(bool interactable)
        {
            if (logCanvasGroup == null) return;
            logCanvasGroup.interactable = interactable;
            logCanvasGroup.blocksRaycasts = interactable;
        }

        // ── Scroll ────────────────────────────

        private void ScrollToBottom()
        {
            if (scrollRect == null)
                return;

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        // ── Build items ───────────────────────

        private void ForceLayoutUpdate()
        {
            Canvas.ForceUpdateCanvases();

            if (contentRoot is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            Canvas.ForceUpdateCanvases();
        }

        private void RebuildIfNeeded()
        {
            if (contentRoot == null || logItemPrefab == null)
                return;

            if (_spawnedItems.Count == _entries.Count)
                return;

            for (int i = _spawnedItems.Count; i < _entries.Count; i++)
            {
                TryCreateItem(_entries[i]);
            }
        }

        private void TryCreateItem(StoryLogEntry entry)
        {
            if (contentRoot == null || logItemPrefab == null || entry == null)
                return;

            GameObject item = Instantiate(logItemPrefab, contentRoot);
            _spawnedItems.Add(item);

            BasicStoryLogItemView view = item.GetComponent<BasicStoryLogItemView>();
            if (view != null)
            {
                ReplacePlayerPlaceholderInEntry(entry);
                view.Bind(entry);
            }

            if (item.transform is RectTransform itemRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(itemRect);
        }
        
        private async UniTaskVoid CachePlayerNameAsync()
        {
            if (_playerNameLoaded)
                return;

            if (!await EnsureUnityServicesReadyAsync())
                return;

            try
            {
                string playerName = AuthenticationService.Instance.PlayerName;

                if (string.IsNullOrWhiteSpace(playerName))
                    playerName = await AuthenticationService.Instance.GetPlayerNameAsync(false);

                playerName = SanitizePlayerName(playerName);

                if (string.IsNullOrWhiteSpace(playerName))
                {
                    Debug.LogWarning("[BasicStoryLogController] 플레이어 이름이 비어 있습니다.", this);
                    return;
                }

                _cachedPlayerName = playerName;
                _playerNameLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BasicStoryLogController] 플레이어 이름 조회 실패: {ex.Message}", this);
            }
        }
        
        private string ReplacePlayerPlaceholder(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (!_playerNameLoaded || string.IsNullOrWhiteSpace(_cachedPlayerName))
                return text;

            return text.Replace(PlayerPlaceholder, _cachedPlayerName, StringComparison.Ordinal);
        }

        private static string SanitizePlayerName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return string.Empty;

            return Regex.IsMatch(playerName, @"#\d{4}$")
                ? playerName.Substring(0, playerName.Length - 5)
                : playerName;
        }
        
        private void ReplacePlayerPlaceholderInEntry(StoryLogEntry entry)
        {
            if (entry == null)
                return;

            entry.displayName = ReplacePlayerPlaceholder(entry.displayName);
            entry.text = ReplacePlayerPlaceholder(entry.text);
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
    }
}