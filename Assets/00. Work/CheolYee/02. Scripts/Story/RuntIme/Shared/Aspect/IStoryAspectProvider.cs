namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect
{
    public interface IStoryAspectProvider
    {
        float VisibleWidthRatio { get; }
        float StoryVisibleAspect { get; }
        StoryAspectMetrics CurrentMetrics { get; }
        void RecalculateMetrics();
    }
}
