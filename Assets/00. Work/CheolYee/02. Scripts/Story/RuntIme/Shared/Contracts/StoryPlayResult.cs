using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts
{
    /// 스토리 재생 결과를 나타내는 구조체입니다.
    public readonly struct StoryPlayResult
    {
        public string EpisodeId { get; }
        public StoryCloseReason CloseReason { get; }
        public bool WasSkipped { get; }
        public bool HasResumePoint { get; }

        public StoryPlayResult(
            string episodeId,
            StoryCloseReason closeReason,
            bool wasSkipped,
            bool hasResumePoint)
        {
            EpisodeId = episodeId;
            CloseReason = closeReason;
            WasSkipped = wasSkipped;
            HasResumePoint = hasResumePoint;
        }

        public bool IsCompleted => CloseReason == StoryCloseReason.Completed;
    }
}