using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Letterbox
{
    /// <summary>
    /// LetterboxOverlayRoot 아래의 좌우 패널에 앵커와 비주얼을 적용합니다.
    /// StoryAspectSettingsSO의 visibleWidthRatio 기반으로 패널 너비를 비율로 설정하며,
    /// 스프라이트가 없으면 letterboxColor 단색 Image로 동작합니다.
    /// UI 숨김 시에도 이 루트는 숨겨지지 않습니다(StoryUiVisibilityController 대상 외).
    /// </summary>
    public sealed class StoryLetterboxOverlayController : MonoBehaviour
    {
        [SerializeField] private StoryAspectSettingsSO settings;
        [SerializeField] private RectTransform leftPanel;
        [SerializeField] private RectTransform rightPanel;

        private void Awake() => ApplyLayout();

        private void ApplyLayout()
        {
            if (settings == null)
                return;

            float ratio = Mathf.Clamp(settings.VisibleWidthRatio, 0.1f, 1f);
            float letterboxRatio = (1f - ratio) * 0.5f;

            SetPanelAnchors(leftPanel,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(letterboxRatio, 1f));

            SetPanelAnchors(rightPanel,
                anchorMin: new Vector2(1f - letterboxRatio, 0f),
                anchorMax: new Vector2(1f, 1f));

            SetPanelVisuals(leftPanel, settings.LeftLetterboxSprite, settings.LetterboxColor);
            SetPanelVisuals(rightPanel, settings.RightLetterboxSprite, settings.LetterboxColor);
        }

        private static void SetPanelAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rt == null)
                return;

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetPanelVisuals(RectTransform rt, Sprite sprite, Color color)
        {
            if (rt == null)
                return;

            var image = rt.GetComponent<Image>();
            if (image == null)
                image = rt.gameObject.AddComponent<Image>();

            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = true;
        }
    }
}
