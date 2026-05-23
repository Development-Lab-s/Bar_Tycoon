using Gamelib.EventSystem;

namespace BBJ.EventSystem
{
    public enum SceneType { Title, Main, Story, Cocktail }

    public class SceneTransitionRequestEvent : GameEvent
    {
        public SceneType Target { get; }
        public SceneTransitionRequestEvent(SceneType target) => Target = target;
    }

    public class SceneReadyEvent : GameEvent { }

    public class SceneTypeChangedEvent : GameEvent
    {
        public SceneType Current { get; }
        public SceneTypeChangedEvent(SceneType current) => Current = current;
    }

    public class StoryResultEvent : GameEvent
    {
        public string   EpisodeId     { get; }
        public string[] UnlockedItems { get; }
        public StoryResultEvent(string episodeId, string[] unlockedItems)
        {
            EpisodeId     = episodeId;
            UnlockedItems = unlockedItems;
        }
    }
}
