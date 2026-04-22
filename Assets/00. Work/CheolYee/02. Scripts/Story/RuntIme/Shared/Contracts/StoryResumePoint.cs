namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts
{
    /// 스토리 재개 지점을 나타내는 구조체입니다.
    public readonly struct StoryResumePoint
    {
        public string EpisodeId { get; }
        public string LineId { get; }
        public bool AutoMode { get; }

        public StoryResumePoint(string episodeId, string lineId, bool autoMode)
        {
            EpisodeId = episodeId;
            LineId = lineId;
            AutoMode = autoMode;
        }
    }
}