using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts
{
    public readonly struct StoryPlayRequest
    {
        public StoryEpisodeSO Episode { get; }
        public StoryOpenMode OpenMode { get; }
        public string CallerId { get; }

        public StoryPlayRequest(
            StoryEpisodeSO episode,
            StoryOpenMode openMode = StoryOpenMode.Overlay,
            string callerId = "")
        {
            Episode = episode;
            OpenMode = openMode;
            CallerId = callerId;
        }
    }
}
