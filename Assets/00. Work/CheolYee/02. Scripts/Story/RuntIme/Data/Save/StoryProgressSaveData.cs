using System;
using System.Collections.Generic;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Save
{
    [Serializable]
    public sealed class StoryProgressSaveData
    {
        public int version = 1;
        public List<StoryEpisodeUnlockRecord> episodes = new();
    }

    [Serializable]
    public sealed class StoryEpisodeUnlockRecord
    {
        public string episodeId;
        public bool unlocked;
    }
}
