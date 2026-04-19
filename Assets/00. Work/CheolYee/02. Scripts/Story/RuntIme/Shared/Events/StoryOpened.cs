using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class StoryOpened : GameEvent
    {
        public string EpisodeId { get; }

        public StoryOpened(string episodeId)
        {
            EpisodeId = episodeId;
        }
    }
}