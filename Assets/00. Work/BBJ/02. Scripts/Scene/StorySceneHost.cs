using BBJ.EventSystem;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Scene
{
    public class StorySceneHost : MonoBehaviour, ISceneHost
    {
        [SerializeField] private EventChannelSO _sceneChannel;

        public SceneType SceneType => SceneType.Story;

        private void Awake() => GameSceneManager.Instance.RegisterHost(this);

        public void OnForeground() { }

        public void OnBackground()
        {
            // 스토리 결과를 Main에 전달하는 곳 (나중에 구현)
            // _sceneChannel.RaiseEvent(new StoryResultEvent(episodeId, unlockedItems));
        }
    }
}
