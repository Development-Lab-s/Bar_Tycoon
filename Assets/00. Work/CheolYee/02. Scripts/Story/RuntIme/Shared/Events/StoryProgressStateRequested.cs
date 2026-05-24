using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class StoryProgressStateRequested : GameEvent
    {
        public string CallerId { get; }

        public StoryProgressStateRequested(string callerId = "")
        {
            CallerId = callerId;
        }
    }
}
