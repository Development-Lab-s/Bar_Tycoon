using System.Collections.Generic;
using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class StoryProgressStateProvided : GameEvent
    {
        public IReadOnlyList<string> UnlockedEpisodeIds { get; }
        public string CallerId { get; }

        public StoryProgressStateProvided(IReadOnlyList<string> unlockedEpisodeIds, string callerId = "")
        {
            UnlockedEpisodeIds = unlockedEpisodeIds;
            CallerId = callerId;
        }
    }
}
