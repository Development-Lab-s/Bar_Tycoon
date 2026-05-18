using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.CameraSystems
{
    public sealed class StoryAspectRatioController : MonoBehaviour, IStoryAspectProvider
    {
        [SerializeField] private StoryAspectSettingsSO settings;

        private StoryAspectMetrics _metrics;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        public float VisibleWidthRatio => settings != null ? settings.VisibleWidthRatio : 1f;
        public float StoryVisibleAspect => _metrics.StoryVisibleAspect;
        public StoryAspectMetrics CurrentMetrics => _metrics;

        private void Awake() => RecalculateMetrics();

        private void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                RecalculateMetrics();
        }

        public void RecalculateMetrics()
        {
            _lastScreenWidth  = Screen.width;
            _lastScreenHeight = Screen.height;
            _metrics = StoryAspectMetrics.Calculate(_lastScreenWidth, _lastScreenHeight, VisibleWidthRatio);
        }
    }
}
