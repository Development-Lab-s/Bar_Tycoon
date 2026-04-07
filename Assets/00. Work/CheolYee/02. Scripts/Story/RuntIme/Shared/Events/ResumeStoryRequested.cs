using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class ResumeStoryRequested : GameEvent
    {
        public string EpisodeId { get; }
        public string CallerId { get; }

        public ResumeStoryRequested(string episodeId, string callerId = "")
        {
            EpisodeId = episodeId;
            CallerId = callerId;
        }
    }
}