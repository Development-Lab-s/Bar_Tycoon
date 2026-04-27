using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
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
            var sprite = state.background != null ? state.background.PreviewSprite : null;
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
            Vector2 scale = ResolveNonZeroScale(state.EffectiveScale);
            Vector2 offset = state.EffectiveOffset;
            Vector2 pivot = state.EffectivePivot;

            float width = camW * Mathf.Abs(scale.x);
            float height = camH * Mathf.Abs(scale.y);
            float anchorX = camW * 0.5f + offset.x * camW;
            float anchorY = camH * 0.5f - offset.y * camH;

            el.style.width = width;
            el.style.height = height;
            el.style.left = anchorX - width * Mathf.Clamp01(pivot.x);
            el.style.top = anchorY - height * Mathf.Clamp01(pivot.y);
        }

        private static Vector2 ResolveNonZeroScale(Vector2 scale) =>
            Mathf.Approximately(scale.x, 0f) && Mathf.Approximately(scale.y, 0f)
                ? Vector2.one
                : scale;
    }
}
