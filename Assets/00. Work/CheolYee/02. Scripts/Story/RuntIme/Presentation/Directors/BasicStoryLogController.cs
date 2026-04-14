using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.RuntimeModules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.ViewModels;
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

        private readonly List<StoryLogEntry> _entries = new();
        private readonly List<GameObject> _spawnedItems = new();

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
            Debug.Log("Open Story Log");

            if (logAreaRoot != null)
                logAreaRoot.SetActive(true);

            RebuildIfNeeded();

            Canvas.ForceUpdateCanvases();

            if (contentRoot is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            Canvas.ForceUpdateCanvases();
        }

        public void Close()
        {
            if (logAreaRoot != null)
                logAreaRoot.SetActive(false);
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