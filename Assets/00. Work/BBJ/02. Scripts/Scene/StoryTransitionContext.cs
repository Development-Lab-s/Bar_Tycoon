using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using BBJ.EventSystem;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Scene
{
    public class StoryTransitionContext : MonoBehaviour
    {
        public static StoryTransitionContext Instance { get; private set; }

        [SerializeField] private EventChannelSO _sceneChannel;

        public StoryEpisodeCatalogEntry PendingEpisode { get; private set; }

        private void Awake()
        {
            Instance = this;
            UtilDebugger.AssertAllAssigned(this);
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        public void RequestStory(StoryEpisodeCatalogEntry episode)
        {
            PendingEpisode = episode;
            _sceneChannel.RaiseEvent(new SceneTransitionRequestEvent(SceneType.Story));
        }

        public void Clear() => PendingEpisode = null;
    }
}
