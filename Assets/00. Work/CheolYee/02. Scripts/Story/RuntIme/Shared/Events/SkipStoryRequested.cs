using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class SkipStoryRequested : GameEvent
    {
        public bool ShowSummaryFirst { get; }
        public bool MarkAsSeen { get; }
        public string CallerId { get; }

        public SkipStoryRequested(
            bool showSummaryFirst = true,
            bool markAsSeen = true,
            string callerId = "")
        {
            ShowSummaryFirst = showSummaryFirst;
            MarkAsSeen = markAsSeen;
            CallerId = callerId;
        }
    }
}