using System.Collections;
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

        [Header("Panel")]
        [Tooltip("다시보기 클릭 시 닫을 패널 루트 오브젝트. 스토리 캔버스 루트를 연결하세요.")]
        [SerializeField] private GameObject panelRoot;

        private readonly List<GameObject> _spawnedItems = new();
        private HashSet<string> _unlockedIds = new();
        private string _callerId;
        private bool _launching;

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
            _launching = false;
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
            if (_launching) return;
            _launching = true;
            StartCoroutine(SlideDownAndLaunch(entry));
        }

        private IEnumerator SlideDownAndLaunch(StoryEpisodeCatalogEntry entry)
        {
            if (panelRoot != null)
            {
                var rt = panelRoot.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float duration = 0.3f;
                    float elapsed = 0f;
                    float startY = rt.anchoredPosition.y;
                    float endY = -Screen.height * 2f;

                    while (elapsed < duration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = Mathf.Clamp01(elapsed / duration);
                        float ease = 1f - (1f - t) * (1f - t) * (1f - t); // OutCubic
                        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, Mathf.Lerp(startY, endY, ease));
                        yield return null;
                    }
                }
            }

            _launching = false;
            storyCommandChannel?.RaiseEvent(
                new StoryEpisodeLaunchRequested(entry.Episode, entry.Episode.EpisodeId));
        }
    }
}
