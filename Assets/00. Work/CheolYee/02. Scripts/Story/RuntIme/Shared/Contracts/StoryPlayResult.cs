using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts
{
    public readonly struct StoryPlayResult
    {
        public string EpisodeId { get; }
        public StoryCloseReason CloseReason { get; }
        public bool WasSkipped { get; }

        public StoryPlayResult(string episodeId, StoryCloseReason closeReason, bool wasSkipped)
        {
            EpisodeId = episodeId;
            CloseReason = closeReason;
            WasSkipped = wasSkipped;
        }

        public bool IsCompleted => CloseReason == StoryCloseReason.Completed;
    }
}
