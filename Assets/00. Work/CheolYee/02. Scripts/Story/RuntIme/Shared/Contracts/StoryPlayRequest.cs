using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts
{
    /// 스토리 재생 요청을 나타내는 구조체입니다.
    public readonly struct StoryPlayRequest
    {
        public StoryEpisodeSO Episode { get; }
        public StoryOpenMode OpenMode { get; }
        public bool UseResumePointIfExists { get; }
        public string CallerId { get; }

        public StoryPlayRequest(
            StoryEpisodeSO episode,
            StoryOpenMode openMode = StoryOpenMode.Overlay,
            bool useResumePointIfExists = true,
            string callerId = "")
        {
            Episode = episode;
            OpenMode = openMode;
            UseResumePointIfExists = useResumePointIfExists;
            CallerId = callerId;
        }
    }
}