using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using BBJ.EventSystem;
using BBJ.Scene;
using Gamelib.EventSystem;
using LitMotion.Animation.Components;
using System.Net.NetworkInformation;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core
{
    // story scene 진입 시 pendingEpisodeId 를 consume 하여 기존 PlayStoryRequested 흐름으로 연결한다.
    // Start() 사용 이유: story scene 내 StoryService 의 Awake 구독이 완료된 이후 발행 보장.
    public sealed class StorySceneBootstrap : MonoBehaviour, ISceneHost
    {
        [Header("Channels")]
        [SerializeField] private EventChannelSO storyCommandChannel;
        [SerializeField] private EventChannelSO sceneChenal;

        [Header("References")]
        [SerializeField] private StoryLaunchRequestStoreSO launchRequestStore;
        [SerializeField] private StoryEpisodeCatalogSO episodeCatalog;

        [SerializeField] private Camera sceenMainCamera;
        [SerializeField]private TutoBoolSO tutoBool;
        public SceneType SceneType => SceneType.Story;

        private void Awake()
        {
            GameSceneManager.Instance.RegisterHost(this);
            storyCommandChannel.AddListener<StoryClosed>(OnStoryClosed);
            sceenMainCamera?.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            storyCommandChannel.RemoveListener<StoryClosed>(OnStoryClosed);
        }
        private void OnStoryClosed(StoryClosed _)
        {
            sceneChenal.RaiseEvent(new SceneTransitionRequestEvent(SceneType.Main));
        }

        public void OnForeground() 
        {
            StartStory();
            sceenMainCamera?.gameObject.SetActive(true);
            Camera.SetupCurrent(sceenMainCamera);
        }
        public void OnBackground()
        {
            tutoBool.value = true;
            sceenMainCamera?.gameObject.SetActive(false);
        }

        private void StartStory()
        {
            if (launchRequestStore == null)
            {
                Debug.LogWarning("[StorySceneBootstrap] launchRequestStore가 없습니다.");
                return;
            }

            if (!launchRequestStore.TryConsume(out string episodeId))
            {
                Debug.LogWarning("[StorySceneBootstrap] pendingEpisodeId가 없습니다. story scene이 직접 로드된 것으로 보입니다.");
                return;
            }

            if (episodeCatalog == null)
            {
                Debug.LogWarning($"[StorySceneBootstrap] episodeCatalog가 없습니다. episode={episodeId}");
                return;
            }

            if (!episodeCatalog.TryGetEntry(episodeId, out StoryEpisodeCatalogEntry entry) || entry.Episode == null)
            {
                Debug.LogWarning($"[StorySceneBootstrap] catalog에 없거나 episode SO가 없는 episodeId — {episodeId}");
                return;
            }

            if (storyCommandChannel == null)
            {
                Debug.LogWarning("[StorySceneBootstrap] storyCommandChannel이 없습니다.");
                return;
            }

            storyCommandChannel.RaiseEvent(new PlayStoryRequested(episode: entry.Episode, callerId: episodeId));
        }

    }
}
