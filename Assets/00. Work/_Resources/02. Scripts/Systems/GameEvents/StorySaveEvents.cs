using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using Gamelib.EventSystem;

namespace _00._Work._Resources._02._Scripts.Systems.GameEvents
{
    public static class StorySaveEvents
    {
        public static readonly StorySave StorySave = new();
    }

    public class StorySave : GameEvent
    {
        public StoryEpisodeSO saveEpisode;
        public bool isLocked;

        public StorySave Init(StoryEpisodeSO saveEpisode, bool isLocked)
        {
            this.saveEpisode = saveEpisode;
            this.isLocked = isLocked;
            return this;
        }
    }
}