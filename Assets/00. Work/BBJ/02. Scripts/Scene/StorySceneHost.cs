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
            // 카메라는 자기 자신들이 생성되면서 Bridge를 통해 자동으로 주입되었으므로 신경 쓰지 않음
            var episode = StoryTransitionContext.Instance?.PendingEpisode;
            if (episode == null) return;
            _storyCommandChannel.RaiseEvent(new PlayStoryRequested(episode: episode.Episode));
        }

        public void OnBackground()
        {
            StoryTransitionContext.Instance?.Clear();
        }

        private void OnStoryClosed(StoryClosed _)
        {
            _sceneChannel.RaiseEvent(new SceneTransitionRequestEvent(SceneType.Main));
        }
    }
}