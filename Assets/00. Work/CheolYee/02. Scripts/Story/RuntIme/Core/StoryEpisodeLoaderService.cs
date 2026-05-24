using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core
{
    public sealed class StoryEpisodeLoaderService : MonoBehaviour
    {
        [Header("Channels")]
        [SerializeField] private EventChannelSO storyCommandChannel;

        [Header("References")]
        [SerializeField] private StoryProgressService progressService;
        [SerializeField] private StoryEpisodeCatalogSO episodeCatalog;
        [SerializeField] private StoryLaunchRequestStoreSO launchRequestStore;

        [Header("Scene")]
        [SerializeField] private string storySceneName;

        private void OnEnable()
        {
            if (storyCommandChannel == null) return;
            storyCommandChannel.AddListener<StoryEpisodeLaunchRequested>(HandleLaunchRequested);
        }

        private void OnDisable()
        {
            if (storyCommandChannel == null) return;
            storyCommandChannel.RemoveListener<StoryEpisodeLaunchRequested>(HandleLaunchRequested);
        }

        private void HandleLaunchRequested(StoryEpisodeLaunchRequested evt)
        {
            string episodeId = ResolveEpisodeId(evt.Episode, evt.EpisodeId);

            if (string.IsNullOrEmpty(episodeId))
            {
                Debug.LogWarning("[StoryEpisodeLoaderService] 비어있는 episodeId — 무시합니다.");
                return;
            }

            if (episodeCatalog == null || !episodeCatalog.TryGetEntry(episodeId, out _))
            {
                Debug.LogWarning($"[StoryEpisodeLoaderService] catalog에 없는 episodeId — {episodeId}. 무시합니다.");
                return;
            }

            if (progressService != null && !progressService.TrySetUnlocked(episodeId, true))
                return;

            if (launchRequestStore == null)
            {
                Debug.LogWarning("[StoryEpisodeLoaderService] launchRequestStore가 없습니다.");
                return;
            }

            launchRequestStore.SetPendingEpisode(episodeId);
            LoadStoryScene();
        }

        private void LoadStoryScene()
        {
            if (string.IsNullOrEmpty(storySceneName))
            {
                Debug.LogWarning("[StoryEpisodeLoaderService] storySceneName이 비어 있습니다. scene load를 중단합니다.");
                return;
            }
            SceneManager.LoadScene(storySceneName);
        }

        private static string ResolveEpisodeId(StoryEpisodeSO episode, string episodeId)
        {
            if (episode != null)
            {
                if (!string.IsNullOrEmpty(episodeId) && episodeId != episode.EpisodeId)
                    Debug.LogWarning($"[StoryEpisodeLoaderService] EpisodeId 불일치 — SO={episode.EpisodeId}, string={episodeId}. SO를 사용합니다.");
                return episode.EpisodeId;
            }
            return episodeId;
        }
    }
}
