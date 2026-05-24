using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class StoryEpisodeLaunchRequested : GameEvent
    {
        public StoryEpisodeSO Episode { get; }
        public string EpisodeId { get; }

        public StoryEpisodeLaunchRequested(StoryEpisodeSO episode, string episodeId = "")
        {
            Episode = episode;
            EpisodeId = episodeId;
        }

        public StoryEpisodeLaunchRequested(string episodeId)
        {
            Episode = null;
            EpisodeId = episodeId;
        }
    }
}
