using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class StoryStarted : GameEvent
    {
        public string EpisodeId { get; }
        public string EntryLineId { get; }

        public StoryStarted(string episodeId, string entryLineId)
        {
            EpisodeId = episodeId;
            EntryLineId = entryLineId;
        }
    }
}