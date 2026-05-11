using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        private void RefreshBackgroundLayer()
        {
            if (_backgroundLayer == null)
                return;

            _backgroundLayer.Clear();

            if (_bgState == null || !_bgState.HasBackground || !_bgState.visible)
                return;

            VisualElement bg = CreateBackgroundElement(_bgState);
            PositionBackgroundElement(bg, _bgState);
            _backgroundLayer.Add(bg);
        }

        private static VisualElement CreateBackgroundElement(StoryBackgroundStateData state)
        {
            var sprite = StoryStageVisualSizing.ResolveBackgroundSprite(state);
            if (sprite != null)
            {
                return new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position = Position.Absolute,
                        backgroundImage = new StyleBackground(sprite),
                        backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover)),
                        backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center)),
                        backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center)),
                        unityBackgroundImageTintColor = new StyleColor(state.EffectiveTint),
                        opacity = state.EffectiveOpacity
                    }
                };
            }

            var placeholder = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    backgroundColor = new StyleColor(new Color(0.12f, 0.13f, 0.15f, 0.65f)),
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.54f, 0.62f, 0.72f, 0.45f)),
                    borderRightColor = new StyleColor(new Color(0.54f, 0.62f, 0.72f, 0.45f)),
                    borderBottomColor = new StyleColor(new Color(0.54f, 0.62f, 0.72f, 0.45f)),
                    borderLeftColor = new StyleColor(new Color(0.54f, 0.62f, 0.72f, 0.45f)),
                    opacity = state.EffectiveOpacity
                }
            };

            placeholder.Add(new Label(state.ResolvedBackgroundKey)
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    right = 0,
                    bottom = 0,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    color = new StyleColor(new Color(0.8f, 0.8f, 0.8f, 0.65f))
                }
            });

            return placeholder;
        }

        private void PositionBackgroundElement(VisualElement el, StoryBackgroundStateData state)
        {
            float camW = DefaultUnitPixels;
            float camH = DefaultUnitPixels / GetRenderAspect();
            StoryBackgroundStateData sample = state.ShallowClone();
            sample.normalizedOffset += ResolvePreviewBackgroundParallaxOffset(state);
            Rect rect = StoryStageVisualSizing.CalculateBackgroundPreviewRect(
                sample,
                StoryStageVisualSizing.ResolveBackgroundSprite(sample),
                new Vector2(camW, camH),
                ResolvePreviewCameraWorldWidth());

            el.style.width = rect.width;
            el.style.height = rect.height;
            el.style.left = rect.x;
            el.style.top = rect.y;
        }

        private Vector2 ResolvePreviewBackgroundParallaxOffset(StoryBackgroundStateData state)
        {
            if (!ShouldApplyCameraFocusToRenderedPreview())
                return Vector2.zero;

            if (state?.background == null)
                return Vector2.zero;

            Vector2 cameraOffset = ResolvePreviewCameraFocusOffset();
            if (cameraOffset == Vector2.zero)
                return Vector2.zero;

            return -cameraOffset * state.background.ParallaxFactor;
        }

        private static Vector2 ResolveNonZeroScale(Vector2 scale) =>
            Mathf.Approximately(scale.x, 0f) && Mathf.Approximately(scale.y, 0f)
                ? Vector2.one
                : scale;
    }
}
