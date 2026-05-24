using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class PlayStoryRequested : GameEvent
    {
        public StoryEpisodeSO Episode { get; }
        public StoryOpenMode OpenMode { get; }
        public string CallerId { get; }

        public PlayStoryRequested(
            StoryEpisodeSO episode,
            StoryOpenMode openMode = StoryOpenMode.Overlay,
            string callerId = "")
        {
            Episode = episode;
            OpenMode = openMode;
            CallerId = callerId;
        }
    }
}
