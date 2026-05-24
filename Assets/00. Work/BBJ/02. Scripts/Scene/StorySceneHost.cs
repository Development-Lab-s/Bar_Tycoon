using BBJ.EventSystem;
using Gamelib.EventSystem;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using UnityEngine;

namespace BBJ.Scene
{
    public class StorySceneHost : MonoBehaviour, ISceneHost
    {
        [SerializeField] private EventChannelSO _sceneChannel;
        [SerializeField] private EventChannelSO _storyCommandChannel;
        [SerializeField] private EventChannelSO _storySignalChannel;

        public SceneType SceneType => SceneType.Story;

        private void Awake()
        {
            GameSceneManager.Instance.RegisterHost(this);
            _storySignalChannel.AddListener<StoryClosed>(OnStoryClosed);
        }

        private void OnDestroy()
        {
            _storySignalChannel.RemoveListener<StoryClosed>(OnStoryClosed);
        }

        public void OnForeground()
        {
            var episode = StoryTransitionContext.Instance?.PendingEpisode;
            if (episode == null) return;
            _storyCommandChannel.RaiseEvent(new PlayStoryRequested(episode: episode.Episode, callerId: episode.EpisodeId));
        }

        public void OnBackground()
        {
            StoryTransitionContext.Instance?.Clear();
            // TODO: 스토리 결과 전달 — StoryResultEvent(episodeId, unlockedItems)
        }

        private void OnStoryClosed(StoryClosed _)
        {
            _sceneChannel.RaiseEvent(new SceneTransitionRequestEvent(SceneType.Main));
        }
    }
}
