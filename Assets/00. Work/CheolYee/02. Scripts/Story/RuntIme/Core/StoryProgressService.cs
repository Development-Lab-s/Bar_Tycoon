using System.Collections.Generic;
using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Save;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using Alchemy.Inspector;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core
{
    public sealed class StoryProgressService : MonoBehaviour
    {
        [Header("Channels")]
        [SerializeField] private EventChannelSO storyCommandChannel;

        [Header("Catalog")]
        [SerializeField] private StoryEpisodeCatalogSO episodeCatalog;

        [Header("Debug")]
        [SerializeField] private StoryEpisodeSO debugTargetEpisode;
        [SerializeField] private string debugTargetEpisodeId;

        private const string SaveFileName = "StoryProgress.save";
        private const string SaveSubFolder = "Story";

        private readonly Dictionary<string, bool> _cache = new();

        private void Awake()
        {
            LoadIfNeeded();
        }

        private void OnEnable()
        {
            if (storyCommandChannel == null) return;
            storyCommandChannel.AddListener<StoryEpisodeUnlockRequested>(HandleUnlockRequested);
            storyCommandChannel.AddListener<StoryEpisodeLockRequested>(HandleLockRequested);
            storyCommandChannel.AddListener<StoryProgressStateRequested>(HandleStateRequested);
        }

        private void OnDisable()
        {
            if (storyCommandChannel == null) return;
            storyCommandChannel.RemoveListener<StoryEpisodeUnlockRequested>(HandleUnlockRequested);
            storyCommandChannel.RemoveListener<StoryEpisodeLockRequested>(HandleLockRequested);
            storyCommandChannel.RemoveListener<StoryProgressStateRequested>(HandleStateRequested);
        }

        private void LoadIfNeeded()
        {
            _cache.Clear();
            if (!SaveManager.IsSaveFile(SaveFileName, SaveSubFolder))
                return;

            var data = SaveManager.Load(typeof(StoryProgressSaveData), SaveFileName, SaveSubFolder) as StoryProgressSaveData;
            if (data?.episodes == null)
                return;

            foreach (var record in data.episodes)
            {
                if (string.IsNullOrEmpty(record.episodeId))
                    continue;
                if (!IsInCatalog(record.episodeId))
                {
                    Debug.LogWarning($"[StoryProgressService] Load: catalog에 없는 episodeId 무시 — {record.episodeId}");
                    continue;
                }
                _cache[record.episodeId] = record.unlocked;
            }
        }

        private void Save()
        {
            var data = new StoryProgressSaveData();
            if (episodeCatalog != null)
            {
                foreach (var entry in episodeCatalog.Entries)
                {
                    if (entry.Episode == null) continue;
                    string id = entry.Episode.EpisodeId;
                    if (_cache.TryGetValue(id, out bool unlocked))
                        data.episodes.Add(new StoryEpisodeUnlockRecord { episodeId = id, unlocked = unlocked });
                }
            }
            else
            {
                foreach (var kv in _cache)
                    data.episodes.Add(new StoryEpisodeUnlockRecord { episodeId = kv.Key, unlocked = kv.Value });
            }
            SaveManager.Save(data, SaveFileName, SaveSubFolder);
        }

        private bool IsInCatalog(string episodeId)
        {
            if (episodeCatalog == null) return true;
            return episodeCatalog.TryGetEntry(episodeId, out _);
        }

        private string ResolveEpisodeId(StoryEpisodeSO episode, string episodeId)
        {
            if (episode != null)
            {
                if (!string.IsNullOrEmpty(episodeId) && episodeId != episode.EpisodeId)
                    Debug.LogWarning($"[StoryProgressService] EpisodeId 불일치 — SO={episode.EpisodeId}, string={episodeId}. SO를 사용합니다.");
                return episode.EpisodeId;
            }
            return episodeId;
        }

        public bool TrySetUnlocked(string episodeId, bool unlocked) => SetUnlocked(episodeId, unlocked);

        private bool SetUnlocked(string episodeId, bool unlocked)
        {
            if (string.IsNullOrEmpty(episodeId))
            {
                Debug.LogWarning("[StoryProgressService] 비어있는 episodeId — 무시합니다.");
                return false;
            }
            if (!IsInCatalog(episodeId))
            {
                Debug.Log($"[StoryProgressService] catalog에 없는 episodeId — {episodeId}. 무시합니다.");
                return false;
            }
            _cache[episodeId] = unlocked;
            Save();
            storyCommandChannel?.RaiseEvent(new StoryProgressChanged(episodeId, unlocked));
            return true;
        }

        private List<string> BuildUnlockedList()
        {
            var result = new List<string>();
            if (episodeCatalog != null)
            {
                foreach (var entry in episodeCatalog.Entries)
                {
                    if (entry.Episode == null) continue;
                    string id = entry.Episode.EpisodeId;
                    if (_cache.TryGetValue(id, out bool unlocked) && unlocked)
                        result.Add(id);
                }
            }
            else
            {
                foreach (var kv in _cache)
                    if (kv.Value) result.Add(kv.Key);
            }
            return result;
        }

        private void HandleUnlockRequested(StoryEpisodeUnlockRequested evt)
        {
            SetUnlocked(ResolveEpisodeId(evt.Episode, evt.EpisodeId), true);
        }

        private void HandleLockRequested(StoryEpisodeLockRequested evt)
        {
            SetUnlocked(ResolveEpisodeId(evt.Episode, evt.EpisodeId), false);
        }

        private void HandleStateRequested(StoryProgressStateRequested evt)
        {
            storyCommandChannel?.RaiseEvent(new StoryProgressStateProvided(BuildUnlockedList(), evt.CallerId));
        }

        [Button]
        public void UnlockAllEpisodes()
        {
            if (episodeCatalog == null) { Debug.LogWarning("[StoryProgressService] catalog이 없습니다."); return; }
            foreach (var entry in episodeCatalog.Entries)
            {
                if (entry.Episode != null)
                    _cache[entry.Episode.EpisodeId] = true;
            }
            Save();
            storyCommandChannel?.RaiseEvent(new StoryProgressStateProvided(BuildUnlockedList(), ""));
        }

        [Button]
        public void LockAllEpisodes()
        {
            if (episodeCatalog == null) { Debug.LogWarning("[StoryProgressService] catalog이 없습니다."); return; }
            foreach (var entry in episodeCatalog.Entries)
            {
                if (entry.Episode != null)
                    _cache[entry.Episode.EpisodeId] = false;
            }
            Save();
            storyCommandChannel?.RaiseEvent(new StoryProgressStateProvided(BuildUnlockedList(), ""));
        }

        [Button]
        public void UnlockSelectedEpisode()
        {
            SetUnlocked(ResolveEpisodeId(debugTargetEpisode, debugTargetEpisodeId), true);
        }

        [Button]
        public void LockSelectedEpisode()
        {
            SetUnlocked(ResolveEpisodeId(debugTargetEpisode, debugTargetEpisodeId), false);
        }

        [Button]
        public void RemoveRecordsNotInCatalog()
        {
            if (episodeCatalog == null) { Debug.LogWarning("[StoryProgressService] catalog이 없습니다."); return; }
            var keysToRemove = new List<string>();
            foreach (var key in _cache.Keys)
            {
                if (!episodeCatalog.TryGetEntry(key, out _))
                    keysToRemove.Add(key);
            }
            if (keysToRemove.Count == 0) return;
            foreach (var key in keysToRemove)
                _cache.Remove(key);
            Save();
            storyCommandChannel?.RaiseEvent(new StoryProgressStateProvided(BuildUnlockedList(), ""));
        }

        [Button]
        public void DeleteStoryProgressSave()
        {
            SaveManager.DeleteSave(SaveFileName, SaveSubFolder);
            _cache.Clear();
            storyCommandChannel?.RaiseEvent(new StoryProgressStateProvided(new List<string>(), ""));
        }
    }
}
