using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class CloseStoryRequested : GameEvent
    {
        public StoryCloseReason Reason { get; }
        public bool SaveResumePoint { get; }

        public CloseStoryRequested(
            StoryCloseReason reason = StoryCloseReason.UserClosed,
            bool saveResumePoint = true)
        {
            Reason = reason;
            SaveResumePoint = saveResumePoint;
        }
    }
}