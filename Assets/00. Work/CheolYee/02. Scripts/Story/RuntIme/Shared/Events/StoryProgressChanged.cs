using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class StoryProgressChanged : GameEvent
    {
        public string EpisodeId { get; }
        public bool Unlocked { get; }

        public StoryProgressChanged(string episodeId, bool unlocked)
        {
            EpisodeId = episodeId;
            Unlocked = unlocked;
        }
    }
}
