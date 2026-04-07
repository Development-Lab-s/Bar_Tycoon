using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class StoryFinished : GameEvent
    {
        public string EpisodeId { get; }
        public bool WasSkipped { get; }

        public StoryFinished(string episodeId, bool wasSkipped)
        {
            EpisodeId = episodeId;
            WasSkipped = wasSkipped;
        }
    }
}