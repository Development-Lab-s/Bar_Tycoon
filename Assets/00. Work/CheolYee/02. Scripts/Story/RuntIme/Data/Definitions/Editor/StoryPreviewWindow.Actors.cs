using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        // ── 액터 레이어 재빌드 ─────────────────────────

        private void RebuildActorLayer()
        {
            if (_actorLayer == null) return;

            RefreshBackgroundLayer();
            RefreshActorList();

            _actorLayer.Clear();
            _actorElements.Clear();

            var ordered = new List<KeyValuePair<string, StoryActorStateData>>(_stageState);
            ordered.Sort((a, b) => a.Value.sortOrder.CompareTo(b.Value.sortOrder));

            bool any = false;
            foreach (var kvp in ordered)
            {
                string actorKey = kvp.Key;
                var data  = kvp.Value;
                var actor = data.actor;
                if (actor == null || data == null || !data.visible) continue;

                var el = CreateActorElement(actorKey, actor, data);
                _actorLayer.Add(el);
                _actorElements[actorKey] = el;
                PositionActorElement(el, data);
                any = true;
            }
            SetEmptyStageVisible(!any);
        }

        private VisualElement CreateActorElement(string actorKey, CharacterDefinitionSO actor, StoryActorStateData data)
        {
            var el = new VisualElement
            {
                name = actorKey,
                tooltip = $"{actor.DisplayName} ({actorKey})",
                style = { position = Position.Absolute, overflow = Overflow.Hidden, borderTopLeftRadius = 3, borderTopRightRadius = 3 }
            };

            var sprite = actor.PreviewSprite;
            if (sprite != null)
            {
                el.style.backgroundImage = new StyleBackground(sprite);
                el.style.backgroundSize  = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
                el.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
                el.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Bottom));
            }
            else
            {
                el.style.backgroundColor = new StyleColor(ActorPlaceholderColor(actor));
                el.Add(new Label(actor.DisplayName) { style = { fontSize = 9, color = new StyleColor(Color.white), unityTextAlign = TextAnchor.UpperCenter } });
            }

            ApplyActorFocusStyle(el, data);
            if (data.focused)
                AddFocusMarker(el);

            if (_selectionKind == StageSelectionKind.Actor && _selectedActorKey == actorKey)
                el.style.borderTopWidth = el.style.borderRightWidth =
                el.style.borderBottomWidth = el.style.borderLeftWidth = 2;

            RegisterActorInteraction(el, actorKey, data);
            return el;
        }

        private static void AddFocusMarker(VisualElement el)
        {
            var marker = new Label("FOCUS")
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    top = 3,
                    right = 3,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 1,
                    paddingBottom = 1,
                    fontSize = 8,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(new Color(0.08f, 0.07f, 0.03f)),
                    backgroundColor = new StyleColor(new Color(1f, 0.78f, 0.18f, 0.92f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            el.Add(marker);
        }

        private static void ApplyActorFocusStyle(VisualElement el, StoryActorStateData data)
        {
            el.style.unityBackgroundImageTintColor = new StyleColor(data.focused ? Color.white : new Color(0.5f, 0.5f, 0.5f));
            el.style.opacity = data.focused ? 1f : 0.65f;
        }

        // ── 액터 배치 (Stage World 좌표계) ────────────

        /// <summary>
        /// actor element 를 stage world 좌표에 배치한다.
        /// 1 world unit = DefaultUnitPixels px.
        /// normPos(0,0) = 카메라 프레임 좌측 하단, (1,1) = 우측 상단.
        /// 카메라 프레임 밖 배치(normPos 범위 초과)도 허용한다.
        /// </summary>
        private void PositionActorElement(VisualElement el, StoryActorStateData data)
        {
            float camW = DefaultUnitPixels;
            float camH = DefaultUnitPixels / GetRenderAspect();

            var     sprite         = data.actor?.PreviewSprite;
            Vector2 effectiveScale = data.EffectiveScale;
            float   aw             = GetActorPreviewWidth(effectiveScale, camH);
            float   aspect         = (sprite != null && sprite.rect.width > 0)
                                     ? sprite.rect.height / sprite.rect.width
                                     : DefaultActorAspect;
            float ah = aw * aspect * Mathf.Abs(effectiveScale.y);

            Vector2 normPos = data.normalizedPosition + data.EffectiveOffset;
            Vector2 pivot   = data.EffectivePivot;

            float x = normPos.x * camW - aw * Mathf.Clamp01(pivot.x);
            float y = (1f - normPos.y) * camH - ah * (1f - Mathf.Clamp01(pivot.y));

            el.style.width  = aw;
            el.style.height = ah;
            el.style.left   = x;
            el.style.top    = y;
        }

        private static float GetActorPreviewWidth(Vector2 effectiveScale, float cameraHeight)
        {
            float referenceCameraHeight = DefaultUnitPixels * FallbackRenderHeight / FallbackRenderWidth;
            float heightScale = Mathf.Clamp(cameraHeight / referenceCameraHeight, ActorMinHeightScale, ActorMaxHeightScale);
            return DefaultUnitPixels * ActorWidthFrac * heightScale * Mathf.Abs(effectiveScale.x);
        }

        private void SetEmptyStageVisible(bool visible)
        {
            if (_emptyStageLabel != null)
                _emptyStageLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ── 드래그 인터랙션 ───────────────────────────

        private void RegisterActorInteraction(VisualElement el, string actorKey, StoryActorStateData data)
        {
            el.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                SelectActor(actorKey);
                _draggingActorKey  = actorKey;
                _dragStartPanelPos = e.position;
                _dragStartNormPos  = data.normalizedPosition;
                el.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            el.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (_draggingActorKey != actorKey) return;

                float camW = DefaultUnitPixels;
                float camH = DefaultUnitPixels / GetRenderAspect();

                // panel 공간 delta → world 공간 delta (zoom 보정)
                Vector2 panelDelta = (Vector2)e.position - _dragStartPanelPos;
                Vector2 worldDelta = panelDelta / _stageZoom;

                data.normalizedPosition = new Vector2(
                    _dragStartNormPos.x + worldDelta.x / camW,
                    _dragStartNormPos.y - worldDelta.y / camH   // Y 반전
                );

                PositionActorElement(el, data);
                RefreshActorInspector();
                e.StopPropagation();
            });

            el.RegisterCallback<PointerUpEvent>(e =>
            {
                if (_draggingActorKey != actorKey) return;
                _draggingActorKey = null;
                el.ReleasePointer(e.pointerId);
                SaveActorStateToCurrent(actorKey, entry => entry.normalizedPosition = data.normalizedPosition);
                e.StopPropagation();
            });
        }

        private void HighlightSelectedActor()
        {
            foreach (var kvp in _actorElements)
            {
                bool  sel = _selectionKind == StageSelectionKind.Actor && kvp.Key == _selectedActorKey;
                float bw  = sel ? 2f : 0f;
                kvp.Value.style.borderTopWidth    = kvp.Value.style.borderRightWidth    =
                kvp.Value.style.borderBottomWidth = kvp.Value.style.borderLeftWidth = bw;
                kvp.Value.style.borderTopColor    = kvp.Value.style.borderRightColor    =
                kvp.Value.style.borderBottomColor = kvp.Value.style.borderLeftColor =
                    new StyleColor(new Color(0.9f, 0.7f, 0.2f));
            }
        }
    }
}
