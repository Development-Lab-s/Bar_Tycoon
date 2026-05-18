using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.CameraSystems;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Letterbox
{
    /// <summary>
    /// StoryVisibleFrameRoot RectTransform의 앵커를 visibleWidthRatio 기준으로 설정합니다.
    /// 정규화 앵커 방식이므로 Screen 크기 변화는 UGUI가 자동으로 반영합니다.
    /// </summary>
    public sealed class StoryVisibleFrameController : MonoBehaviour
    {
        [SerializeField] private StoryAspectRatioController aspectProvider;
        [SerializeField] private RectTransform visibleFrameRoot;

        private void Awake() => ApplyLayout();

        private void ApplyLayout()
        {
            if (visibleFrameRoot == null || aspectProvider == null)
                return;

            float ratio = Mathf.Clamp(aspectProvider.VisibleWidthRatio, 0.1f, 1f);
            float margin = (1f - ratio) * 0.5f;

            visibleFrameRoot.anchorMin = new Vector2(margin, 0f);
            visibleFrameRoot.anchorMax = new Vector2(1f - margin, 1f);
            visibleFrameRoot.offsetMin = Vector2.zero;
            visibleFrameRoot.offsetMax = Vector2.zero;
        }
    }
}
