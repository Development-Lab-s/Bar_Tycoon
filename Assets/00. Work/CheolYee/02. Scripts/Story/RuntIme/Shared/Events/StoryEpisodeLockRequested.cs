using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class StoryEpisodeLockRequested : GameEvent
    {
        public StoryEpisodeSO Episode { get; }
        public string EpisodeId { get; }

        public StoryEpisodeLockRequested(StoryEpisodeSO episode, string episodeId = "")
        {
            Episode = episode;
            EpisodeId = episodeId;
        }

        public StoryEpisodeLockRequested(string episodeId)
        {
            Episode = null;
            EpisodeId = episodeId;
        }
    }
}
