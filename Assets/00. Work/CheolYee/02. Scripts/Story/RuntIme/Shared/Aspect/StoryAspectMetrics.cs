using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect
{
    public readonly struct StoryAspectMetrics
    {
        public float PhysicalAspect { get; }
        public float VisibleWidthRatio { get; }
        public float StoryVisibleAspect { get; }
        public Rect VisibleNormalizedRect { get; }
        public Rect VisiblePixelRect { get; }

        private StoryAspectMetrics(
            float physicalAspect,
            float visibleWidthRatio,
            float storyVisibleAspect,
            Rect visibleNormalizedRect,
            Rect visiblePixelRect)
        {
            PhysicalAspect = physicalAspect;
            VisibleWidthRatio = visibleWidthRatio;
            StoryVisibleAspect = storyVisibleAspect;
            VisibleNormalizedRect = visibleNormalizedRect;
            VisiblePixelRect = visiblePixelRect;
        }

        public static StoryAspectMetrics Calculate(float screenWidth, float screenHeight, float visibleWidthRatio)
        {
            float safeRatio = Mathf.Clamp(visibleWidthRatio, 0.1f, 1.0f);
            float safeH = Mathf.Max(0.0001f, screenHeight);
            float physicalAspect = screenWidth / safeH;
            float storyVisibleAspect = physicalAspect * safeRatio;
            float visibleWidth = screenWidth * safeRatio;
            float letterboxHalf = (screenWidth - visibleWidth) * 0.5f;

            var normalizedRect = new Rect(
                letterboxHalf / Mathf.Max(0.0001f, screenWidth),
                0f,
                safeRatio,
                1f);

            var pixelRect = new Rect(letterboxHalf, 0f, visibleWidth, screenHeight);

            return new StoryAspectMetrics(
                physicalAspect, safeRatio, storyVisibleAspect, normalizedRect, pixelRect);
        }

        public static StoryAspectMetrics FromScreen(float visibleWidthRatio)
        {
            return Calculate(Screen.width, Screen.height, visibleWidthRatio);
        }
    }
}
