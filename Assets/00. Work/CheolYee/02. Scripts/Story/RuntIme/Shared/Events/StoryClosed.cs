using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class StoryClosed : GameEvent
    {
        public string EpisodeId { get; }
        public StoryCloseReason Reason { get; }
        public bool HasResumePoint { get; }

        public StoryClosed(string episodeId, StoryCloseReason reason, bool hasResumePoint)
        {
            EpisodeId = episodeId;
            Reason = reason;
            HasResumePoint = hasResumePoint;
        }
    }

}