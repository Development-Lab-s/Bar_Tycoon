using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.RuntimeModules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.ViewModels;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Motion;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    public sealed class BasicStoryLogController : MonoBehaviour, IStoryLogController
    {
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

        public bool IsOpen { get; private set; }

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
                    onFinish: () => CloseImmediate());
            }
            else
            {
                CloseImmediate();
            }
        }

        public void Clear()
        {
            _entries.Clear();

            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null)
                    Destroy(_spawnedItems[i]);
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
                view.Bind(entry);

            if (item.transform is RectTransform itemRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(itemRect);
        }
    }
}