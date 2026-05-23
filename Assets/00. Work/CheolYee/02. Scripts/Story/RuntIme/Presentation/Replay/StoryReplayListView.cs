using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Replay
{
    public sealed class StoryReplayListView : MonoBehaviour
    {
        [Header("Channels")]
        [SerializeField] private EventChannelSO storyCommandChannel;

        [Header("References")]
        [SerializeField] private StoryEpisodeCatalogSO episodeCatalog;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private StoryReplayItemView itemPrefab;

        private readonly List<GameObject> _spawnedItems = new();
        private HashSet<string> _unlockedIds = new();
        private string _callerId;

        private void Awake()
        {
            _callerId = $"StoryReplayListView_{GetInstanceID()}";
        }

        private void OnEnable()
        {
            if (storyCommandChannel != null)
            {
                storyCommandChannel.AddListener<StoryProgressStateProvided>(HandleStateProvided);
                storyCommandChannel.AddListener<StoryProgressChanged>(HandleProgressChanged);
            }
            RequestState();
        }

        private void OnDisable()
        {
            if (storyCommandChannel != null)
            {
                storyCommandChannel.RemoveListener<StoryProgressStateProvided>(HandleStateProvided);
                storyCommandChannel.RemoveListener<StoryProgressChanged>(HandleProgressChanged);
            }
            ClearItems();
        }

        private void RequestState()
        {
            storyCommandChannel?.RaiseEvent(new StoryProgressStateRequested(_callerId));
        }

        private void HandleStateProvided(StoryProgressStateProvided evt)
        {
            if (evt.CallerId != _callerId) return;
            _unlockedIds = new HashSet<string>(evt.UnlockedEpisodeIds);
            Rebuild();
        }

        private void HandleProgressChanged(StoryProgressChanged evt)
        {
            RequestState();
        }

        private void Rebuild()
        {
            ClearItems();

            if (episodeCatalog == null || itemPrefab == null || contentRoot == null)
            {
                Debug.LogWarning("[StoryReplayListView] 필요한 참조가 없습니다.", this);
                return;
            }

            foreach (var entry in episodeCatalog.Entries)
            {
                if (entry == null || entry.Episode == null) continue;
                string id = entry.Episode.EpisodeId;
                if (string.IsNullOrEmpty(id)) continue;
                if (!_unlockedIds.Contains(id)) continue;

                StoryReplayItemView item = Instantiate(itemPrefab, contentRoot);
                StoryEpisodeCatalogEntry captured = entry;
                item.Bind(captured, () => OnItemReplayClicked(captured));
                _spawnedItems.Add(item.gameObject);
            }
        }

        private void ClearItems()
        {
            foreach (var go in _spawnedItems)
            {
                if (go != null)
                    Destroy(go);
            }
            _spawnedItems.Clear();
        }

        private void OnItemReplayClicked(StoryEpisodeCatalogEntry entry)
        {
            storyCommandChannel?.RaiseEvent(
                new StoryEpisodeLaunchRequested(entry.Episode, entry.Episode.EpisodeId));
        }
    }
}
