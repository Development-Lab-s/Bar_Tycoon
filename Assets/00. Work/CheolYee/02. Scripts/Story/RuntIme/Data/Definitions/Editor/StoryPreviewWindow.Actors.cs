using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        // ── 선택/강조 시각 상수 ──────────────────────────
        private const float SelectionBorderWidth = 3f;
        private static readonly Color SelectionBorderColor = new Color(0.95f, 0.85f, 0.25f);

        // ── 액터 레이어 재빌드 ─────────────────────────

        private void RebuildActorLayer(bool refreshInspectorLists = true)
        {
            if (_actorLayer == null) return;

            RefreshFocusPreviewGuide();
            RefreshFocusPreviewGuide();
            RefreshBackgroundLayer();
            if (refreshInspectorLists)
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
                pickingMode = IsStageAuthoringMode ? PickingMode.Position : PickingMode.Ignore,
                style = { position = Position.Absolute, overflow = Overflow.Visible, borderTopLeftRadius = 3, borderTopRightRadius = 3 }
            };

            var sprite = StoryStageVisualSizing.ResolveActorSprite(data);
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
            if (IsStageAuthoringMode)
            {
                AddActorPivotMarker(el, data);
                if (data.focused)
                    AddFocusMarker(el);
                if (string.Equals(actorKey, ResolveCurrentPreviewCameraFocusTarget(), StringComparison.Ordinal))
                    AddCameraFocusTargetMarker(el);
                if (_selectionKind == StageSelectionKind.Actor && _selectedActorKey == actorKey)
                    AddCharacterDefinitionHandles(el, data);
            }

            if (IsStageAuthoringMode && _selectionKind == StageSelectionKind.Actor && _selectedActorKey == actorKey)
            {
                el.style.borderTopWidth = el.style.borderRightWidth =
                el.style.borderBottomWidth = el.style.borderLeftWidth = SelectionBorderWidth;
                AddActorScaleHandles(el, actorKey, data);
            }

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

        private static void AddCameraFocusTargetMarker(VisualElement el)
        {
            var marker = new Label("CAM")
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    top = 18,
                    right = 3,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 1,
                    paddingBottom = 1,
                    fontSize = 8,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(new Color(0.08f, 0.07f, 0.03f)),
                    backgroundColor = new StyleColor(new Color(1f, 0.74f, 0.32f, 0.92f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            el.Add(marker);
        }

        private static void AddActorPivotMarker(VisualElement el, StoryActorStateData data)
        {
            Vector2 pivot = data.EffectivePivot;
            float left = Mathf.Clamp01(pivot.x) * 100f;
            float top = (1f - Mathf.Clamp01(pivot.y)) * 100f;

            var horizontal = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = new StyleLength(new Length(left, LengthUnit.Percent)),
                    top = new StyleLength(new Length(top, LengthUnit.Percent)),
                    width = 10,
                    height = 1,
                    marginLeft = -5,
                    backgroundColor = new StyleColor(new Color(0.90f, 0.92f, 1f, 0.92f))
                }
            };
            var vertical = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = new StyleLength(new Length(left, LengthUnit.Percent)),
                    top = new StyleLength(new Length(top, LengthUnit.Percent)),
                    width = 1,
                    height = 10,
                    marginTop = -5,
                    backgroundColor = new StyleColor(new Color(0.90f, 0.92f, 1f, 0.92f))
                }
            };
            var center = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = new StyleLength(new Length(left, LengthUnit.Percent)),
                    top = new StyleLength(new Length(top, LengthUnit.Percent)),
                    width = 4,
                    height = 4,
                    marginLeft = -2,
                    marginTop = -2,
                    backgroundColor = new StyleColor(new Color(0.18f, 0.78f, 1f, 0.95f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            el.Add(horizontal);
            el.Add(vertical);
            el.Add(center);
        }

        private void AddCharacterDefinitionHandles(VisualElement el, StoryActorStateData data)
        {
            if (data?.actor == null)
                return;

            AddPivotEditHandle(el, data);
            AddCameraFocusEditHandle(el, data);
        }

        private void AddPivotEditHandle(VisualElement el, StoryActorStateData data)
        {
            Vector2 pivot = data.EffectivePivot;
            float left = Mathf.Clamp01(pivot.x) * 100f;
            float top = (1f - Mathf.Clamp01(pivot.y)) * 100f;

            var handle = new VisualElement
            {
                name = "PivotHandle",
                tooltip = "Edit pivot in Inspector",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = new StyleLength(new Length(left, LengthUnit.Percent)),
                    top = new StyleLength(new Length(top, LengthUnit.Percent)),
                    width = 12,
                    height = 12,
                    marginLeft = -6,
                    marginTop = -6,
                    backgroundColor = new StyleColor(new Color(0.16f, 0.86f, 1f, 0.96f)),
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(Color.black),
                    borderRightColor = new StyleColor(Color.black),
                    borderBottomColor = new StyleColor(Color.black),
                    borderLeftColor = new StyleColor(Color.black)
                }
            };
            el.Add(handle);
        }

        private void AddCameraFocusEditHandle(VisualElement el, StoryActorStateData data)
        {
            if (data.actor == null)
                return;

            Vector2 pivot = data.EffectivePivot;
            float camW = DefaultUnitPixels;
            float camH = DefaultUnitPixels / GetRenderAspect();
            float pivotX = Mathf.Clamp01(pivot.x) * Mathf.Max(1f, el.resolvedStyle.width);
            float pivotY = (1f - Mathf.Clamp01(pivot.y)) * Mathf.Max(1f, el.resolvedStyle.height);
            Vector2 focusOffset = data.actor.CameraFocusOffset;
            float left = pivotX + focusOffset.x * camW;
            float top = pivotY - focusOffset.y * camH;

            var handle = new VisualElement
            {
                name = "CameraFocusHandle",
                tooltip = "Edit camera focus offset in Inspector",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = left - 6f,
                    top = top - 6f,
                    width = 12,
                    height = 12,
                    backgroundColor = new StyleColor(new Color(1f, 0.72f, 0.30f, 0.96f)),
                    rotate = new StyleRotate(new Rotate(new Angle(45f, AngleUnit.Degree))),
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(Color.black),
                    borderRightColor = new StyleColor(Color.black),
                    borderBottomColor = new StyleColor(Color.black),
                    borderLeftColor = new StyleColor(Color.black)
                }
            };
            el.Add(handle);
        }

        private void RegisterCharacterHandleInteraction(
            VisualElement handle,
            VisualElement actorElement,
            CharacterDefinitionSO actor,
            string undoName,
            Action<Vector2> applyValue,
            Action<VisualElement, Vector2> updateHandleVisual)
        {
            if (handle == null || actorElement == null || actor == null || applyValue == null)
                return;

            bool dragging = false;
            int pointerId = -1;

            handle.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode || e.button != 0)
                    return;

                dragging = true;
                pointerId = e.pointerId;
                Undo.RecordObject(actor, undoName);
                handle.CapturePointer(pointerId);
                e.StopPropagation();
            });

            handle.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!dragging || e.pointerId != pointerId)
                    return;

                Vector2 local = actorElement.WorldToLocal(e.position);
                applyValue(local);
                updateHandleVisual?.Invoke(handle, local);
                EditorUtility.SetDirty(actor);
                UpdateActorLayerPositions();
                Repaint();
                e.StopPropagation();
            });

            handle.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!dragging || e.pointerId != pointerId)
                    return;

                dragging = false;
                handle.ReleasePointer(pointerId);
                pointerId = -1;
                RebuildActorLayer(refreshInspectorLists: false);
                RefreshActorInspector();
                e.StopPropagation();
            });
        }

        private string ResolveCurrentPreviewCameraFocusTarget() =>
            ResolveCurrentPreviewCameraState().targetActorInstanceKey;

        private static void ApplyActorFocusStyle(VisualElement el, StoryActorStateData data)
        {
            float focusBlend = StoryTransitionSampler.ResolveFocusBlend(data.EffectiveFocusAlpha);
            el.style.unityBackgroundImageTintColor = new StyleColor(Color.Lerp(new Color(0.5f, 0.5f, 0.5f), Color.white, focusBlend));
            el.style.opacity = data.EffectiveFocusAlpha;
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
            Rect rect = GetActorWorldRect(data, data.scale);
            el.style.width  = rect.width;
            el.style.height = rect.height;
            el.style.left   = rect.x;
            el.style.top    = rect.y;
        }

        private static float GetActorPreviewWidth(Vector2 effectiveScale, float cameraHeight)
        {
            float referenceCameraHeight = DefaultUnitPixels * FallbackRenderHeight / FallbackRenderWidth;
            float heightScale = Mathf.Clamp(cameraHeight / referenceCameraHeight, ActorMinHeightScale, ActorMaxHeightScale);
            return DefaultUnitPixels * ActorWidthFrac * heightScale * Mathf.Abs(effectiveScale.x);
        }

        private Rect GetActorWorldRect(StoryActorStateData data, Vector2 lineScale)
        {
            StoryActorStateData sample = data.ShallowClone();
            sample.scale = lineScale;
            if (ShouldApplyCameraFocusToRenderedPreview())
                sample.normalizedPosition -= ResolvePreviewCameraFocusOffset();
            float camH = DefaultUnitPixels / GetRenderAspect();
            return StoryStageVisualSizing.CalculateActorPreviewRect(
                sample,
                StoryStageVisualSizing.ResolveActorSprite(sample),
                new Vector2(DefaultUnitPixels, camH),
                ResolvePreviewCameraWorldWidth());
        }

        private Vector2 CalculateActorVisualSize(StoryActorStateData data, Vector2 lineScale)
        {
            StoryActorStateData sample = data.ShallowClone();
            sample.scale = lineScale;
            if (ShouldApplyCameraFocusToRenderedPreview())
                sample.normalizedPosition -= ResolvePreviewCameraFocusOffset();
            float camH = DefaultUnitPixels / GetRenderAspect();
            return StoryStageVisualSizing.CalculateActorPreviewSize(
                sample,
                StoryStageVisualSizing.ResolveActorSprite(sample),
                new Vector2(DefaultUnitPixels, camH),
                ResolvePreviewCameraWorldWidth());
        }

        private Vector2 ResolvePreviewCameraFocusOffset()
        {
            StoryCameraStateData cameraState = ResolveCurrentPreviewCameraState();
            string focusActorKey = cameraState.targetActorInstanceKey;
            if (string.IsNullOrWhiteSpace(focusActorKey))
                return Vector2.zero;

            float focusX = ResolvePreviewCameraTargetX(focusActorKey, cameraState.followMode, cameraState.snapshotNormalizedPosition);
            if (Mathf.Approximately(focusX, float.MinValue))
                return Vector2.zero;

            StoryCameraTrackData track = _isTransitionPreviewing ? _transitionCameraTrack : FindCurrentStageLayout()?.CameraTrackEditable;
            float time = _isTransitionPreviewing
                ? _transitionPreviewElapsed
                : _selectionKind == StageSelectionKind.Camera
                    ? _timelinePlayheadTime
                    : 0f;
            if (cameraState.moveMode == StoryCameraMoveMode.Smooth
                && TryGetPreviewCameraTargetKeys(track, time, out StoryActorKeyframeData previousKey, out StoryActorKeyframeData currentKey)
                && currentKey != null)
            {
                float keyTime = StoryTransitionSampler.GetKeyTime(currentKey);
                float local = Mathf.Clamp01((time - keyTime) / 0.45f);
                if (local < 1f)
                {
                    float previousX = previousKey != null
                        ? ResolvePreviewCameraTargetX(previousKey.cameraTargetActorKey, previousKey.cameraFollowMode, previousKey.cameraSnapshotNormalizedPosition)
                        : 0.5f;
                    focusX = Mathf.Lerp(previousX, focusX, local);
                }
            }

            focusX += cameraState.normalizedOffset.x;
            focusX -= 0.5f;
            return new Vector2(focusX, 0f) + ResolvePreviewCameraShakeOffset();
        }

        private static bool TryGetPreviewCameraTargetKeys(
            StoryCameraTrackData track,
            float time,
            out StoryActorKeyframeData previous,
            out StoryActorKeyframeData current)
        {
            previous = null;
            current = null;
            if (track == null || track.keyframes == null)
                return false;

            var keys = new List<StoryActorKeyframeData>();
            foreach (StoryActorKeyframeData keyframe in track.keyframes)
            {
                if (keyframe != null && keyframe.property == StoryActorKeyframeProperty.CameraTarget)
                    keys.Add(keyframe);
            }

            keys.Sort((a, b) => StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b)));
            if (keys.Count == 0)
                return false;

            for (int i = 0; i < keys.Count; i++)
            {
                float keyTime = StoryTransitionSampler.GetKeyTime(keys[i]);
                if (keyTime <= time)
                {
                    current = keys[i];
                    if (i > 0)
                        previous = keys[i - 1];
                }
                else
                {
                    break;
                }
            }

            return current != null;
        }

        private float ResolvePreviewCameraTargetX(string actorKey, StoryCameraFollowMode followMode, Vector2 snapshotPosition)
        {
            if (followMode == StoryCameraFollowMode.SnapshotPosition)
                return snapshotPosition.x;

            if (string.IsNullOrWhiteSpace(actorKey)
                || !_stageState.TryGetValue(actorKey, out StoryActorStateData focusState)
                || focusState == null
                || !focusState.visible)
                return float.MinValue;

            return focusState.normalizedPosition.x
                + focusState.EffectiveOffset.x
                + (focusState.actor != null ? focusState.actor.CameraFocusOffset.x : 0f);
        }

        private StoryCameraStateData ResolveCurrentPreviewCameraState()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StoryCameraTrackData track = _isTransitionPreviewing ? _transitionCameraTrack : layout?.CameraTrackEditable;
            string fallbackTarget = _isTransitionPreviewing ? _transitionCameraFocusTarget : layout?.CameraFocusTarget;
            float time = _isTransitionPreviewing
                ? _transitionPreviewElapsed
                : _selectionKind == StageSelectionKind.Camera
                    ? _timelinePlayheadTime
                    : 0f;
            return StoryTransitionSampler.SampleCameraTrackAtTime(track, fallbackTarget, time);
        }

        private float ResolvePreviewCameraWorldWidth()
        {
            StoryCameraStateData cameraState = ResolveCurrentPreviewCameraState();
            return StoryStageVisualSizing.DefaultCameraWorldWidth / Mathf.Max(0.01f, cameraState.zoomMultiplier);
        }

        private Vector2 ResolvePreviewCameraShakeOffset()
        {
            if (_previewCameraShakeDuration <= 0f)
                return Vector2.zero;

            float elapsed = (float)(EditorApplication.timeSinceStartup - _previewCameraShakeStartedAt);
            if (elapsed >= _previewCameraShakeDuration)
            {
                _previewCameraShakeDuration = 0f;
                return Vector2.zero;
            }

            float normalized = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, _previewCameraShakeDuration));
            float phase = elapsed * Mathf.Max(0f, _previewCameraShakeFrequency) * Mathf.PI * 2f;
            float amplitude = _previewCameraShakeStrength * 0.012f * normalized;
            return new Vector2(Mathf.Sin(phase) * amplitude, Mathf.Cos(phase) * amplitude * 0.35f);
        }

        private bool ShouldApplyCameraFocusToRenderedPreview() =>
            _isTransitionPreviewing || IsRuntimePreviewMode;

        private void SetEmptyStageVisible(bool visible)
        {
            if (_emptyStageLabel != null)
                _emptyStageLabel.style.display = visible && IsStageAuthoringMode ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Transition-frame fast path: updates only the style properties of existing actor
        /// VisualElements without destroying or recreating DOM nodes.
        /// Falls back to a full RebuildActorLayer when the visible actor set has changed
        /// (e.g., actor enters or exits stage).
        /// </summary>
        internal void UpdateActorLayerPositions()
        {
            // Background has only one element — cheap to recreate, no special fast path needed.
            RefreshBackgroundLayer();

            if (_actorLayer == null) return;

            // Detect whether the visible actor set matches _actorElements.
            // A mismatch (enter/exit) requires a full rebuild.
            bool needsRebuild = false;
            foreach (var kvp in _stageState)
            {
                bool shouldShow = kvp.Value != null && kvp.Value.visible && kvp.Value.actor != null;
                bool isShowing  = _actorElements.ContainsKey(kvp.Key);
                if (shouldShow != isShowing) { needsRebuild = true; break; }
            }

            if (needsRebuild)
            {
                RebuildActorLayer(refreshInspectorLists: false);
                return;
            }

            // Fast path: move existing elements in-place.
            foreach (var kvp in _actorElements)
            {
                if (_stageState.TryGetValue(kvp.Key, out var data) && data is { visible: true, actor: not null })
                {
                    kvp.Value.style.display = DisplayStyle.Flex;
                    Sprite sprite = StoryStageVisualSizing.ResolveActorSprite(data);
                    if (sprite != null)
                        kvp.Value.style.backgroundImage = new StyleBackground(sprite);
                    ApplyActorFocusStyle(kvp.Value, data);
                    PositionActorElement(kvp.Value, data);
                }
                else
                {
                    kvp.Value.style.display = DisplayStyle.None;
                }
            }

            SetEmptyStageVisible(_actorElements.Count == 0);
        }

        // ── 드래그 인터랙션 ───────────────────────────

        private void RegisterActorInteraction(VisualElement el, string actorKey, StoryActorStateData data)
        {
            if (!IsStageAuthoringMode)
                return;

            el.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode) return;
                if (e.button != 0) return;
                if (IsActorRecordSelectionLocked(actorKey))
                    return;
                if (BlocksActorManipulationBySelectedKey(actorKey, StoryActorKeyframeProperty.Position))
                    return;
                if (_stageState.TryGetValue(actorKey, out StoryActorStateData currentData))
                    data = currentData;
                SelectActor(actorKey);
                _draggingActorKey  = actorKey;
                _dragStartPanelPos = e.position;
                _dragStartNormPos  = data.normalizedPosition;
                _dragAxisLock = DragAxisLock.None;
                el.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            el.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!IsStageAuthoringMode) return;
                if (_draggingActorKey != actorKey) return;

                float camW = DefaultUnitPixels;
                float camH = DefaultUnitPixels / GetRenderAspect();

                // panel 공간 delta → world 공간 delta (zoom 보정)
                Vector2 panelDelta = (Vector2)e.position - _dragStartPanelPos;
                Vector2 worldDelta = panelDelta / _stageZoom;
                worldDelta = ApplyAxisLock(worldDelta, e.shiftKey, ref _dragAxisLock);

                data.normalizedPosition = new Vector2(
                    _dragStartNormPos.x + worldDelta.x / camW,
                    _dragStartNormPos.y - worldDelta.y / camH   // Y 반전
                );

                PositionActorElement(el, data);
                e.StopPropagation();
            });

            el.RegisterCallback<PointerUpEvent>(e =>
            {
                if (_draggingActorKey != actorKey) return;
                _draggingActorKey = null;
                _dragAxisLock = DragAxisLock.None;
                el.ReleasePointer(e.pointerId);
                if (!TryApplySelectedTimelineKeyFromState(actorKey, data, StoryActorKeyframeProperty.Position))
                {
                    SaveActorStateToCurrent(actorKey, entry => entry.normalizedPosition = data.normalizedPosition, saveNow: true);
                    RecordActorKeyframeFromState(actorKey, data, includePosition: true, includeScale: false);
                }
                RefreshActorInspector();
                e.StopPropagation();
            });
        }

        private void AddActorScaleHandles(VisualElement el, string actorKey, StoryActorStateData data)
        {
            AddActorScaleHandle(el, actorKey, data, ActorScaleHandle.TopLeft, -5f, -5f, null, null);
            AddActorScaleHandle(el, actorKey, data, ActorScaleHandle.TopRight, null, -5f, -5f, null);
            AddActorScaleHandle(el, actorKey, data, ActorScaleHandle.BottomLeft, -5f, null, null, -5f);
            AddActorScaleHandle(el, actorKey, data, ActorScaleHandle.BottomRight, null, null, -5f, -5f);
        }

        private void AddActorScaleHandle(
            VisualElement el,
            string actorKey,
            StoryActorStateData data,
            ActorScaleHandle handleType,
            float? left,
            float? top,
            float? right,
            float? bottom)
        {
            var handle = new VisualElement
            {
                name = $"ScaleHandle_{handleType}",
                pickingMode = PickingMode.Position,
                style =
                {
                    position = Position.Absolute,
                    width = 10,
                    height = 10,
                    backgroundColor = new StyleColor(new Color(1f, 0.76f, 0.22f, 0.96f)),
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.08f, 0.07f, 0.04f)),
                    borderRightColor = new StyleColor(new Color(0.08f, 0.07f, 0.04f)),
                    borderBottomColor = new StyleColor(new Color(0.08f, 0.07f, 0.04f)),
                    borderLeftColor = new StyleColor(new Color(0.08f, 0.07f, 0.04f))
                }
            };

            if (left.HasValue) handle.style.left = left.Value;
            if (top.HasValue) handle.style.top = top.Value;
            if (right.HasValue) handle.style.right = right.Value;
            if (bottom.HasValue) handle.style.bottom = bottom.Value;

            RegisterActorScaleHandleInteraction(handle, el, actorKey, data, handleType);
            el.Add(handle);
        }

        private void RegisterActorScaleHandleInteraction(
            VisualElement handle,
            VisualElement actorElement,
            string actorKey,
            StoryActorStateData data,
            ActorScaleHandle handleType)
        {
            handle.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode || e.button != 0) return;
                if (IsActorRecordSelectionLocked(actorKey))
                    return;
                if (BlocksActorManipulationBySelectedKey(actorKey, StoryActorKeyframeProperty.Scale))
                    return;
                if (_stageState.TryGetValue(actorKey, out StoryActorStateData currentData))
                    data = currentData;

                SelectActor(actorKey);
                _scalingActorKey = actorKey;
                _activeScaleHandle = handleType;
                _scaleStartPanelPos = e.position;
                _scaleStartNormPos = data.normalizedPosition;
                _scaleStartScale = data.scale;
                _scaleStartRect = GetActorWorldRect(data, data.scale);
                _scaleAxisLock = DragAxisLock.None;

                handle.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            handle.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!IsStageAuthoringMode
                    || _scalingActorKey != actorKey
                    || _activeScaleHandle != handleType)
                    return;

                Vector2 worldDelta = ((Vector2)e.position - _scaleStartPanelPos) / _stageZoom;
                worldDelta = ApplyAxisLock(worldDelta, e.shiftKey, ref _scaleAxisLock);
                ApplyActorScaleDrag(data, handleType, worldDelta, e.altKey && !e.ctrlKey, e.ctrlKey);
                PositionActorElement(actorElement, data);
                e.StopPropagation();
            });

            handle.RegisterCallback<PointerUpEvent>(e =>
            {
                if (_scalingActorKey != actorKey || _activeScaleHandle != handleType)
                    return;

                _scalingActorKey = null;
                _activeScaleHandle = ActorScaleHandle.None;
                _scaleAxisLock = DragAxisLock.None;
                handle.ReleasePointer(e.pointerId);
                if (!TryApplySelectedTimelineKeyFromState(actorKey, data, StoryActorKeyframeProperty.Scale))
                {
                    SaveActorStateToCurrent(actorKey, entry =>
                    {
                        entry.normalizedPosition = data.normalizedPosition;
                        entry.scale = data.scale;
                    }, saveNow: true);
                    RecordActorKeyframeFromState(actorKey, data, includePosition: false, includeScale: true);
                }
                RefreshActorInspector();
                e.StopPropagation();
            });
        }

        private void ApplyActorScaleDrag(
            StoryActorStateData data,
            ActorScaleHandle handleType,
            Vector2 worldDelta,
            bool scaleFromCenter,
            bool keepOppositeCornerFixed)
        {
            data.normalizedPosition = _scaleStartNormPos;
            data.scale = _scaleStartScale;

            float widthDelta = handleType is ActorScaleHandle.TopLeft or ActorScaleHandle.BottomLeft
                ? -worldDelta.x
                : worldDelta.x;
            float heightDelta = handleType is ActorScaleHandle.TopLeft or ActorScaleHandle.TopRight
                ? -worldDelta.y
                : worldDelta.y;

            float multiplier = scaleFromCenter ? 2f : 1f;
            Vector2 targetSize = new(
                Mathf.Max(12f, _scaleStartRect.width + widthDelta * multiplier),
                Mathf.Max(12f, _scaleStartRect.height + heightDelta * multiplier));

            if (_scaleAxisLock == DragAxisLock.X)
                targetSize.y = _scaleStartRect.height;
            else if (_scaleAxisLock == DragAxisLock.Y)
                targetSize.x = _scaleStartRect.width;

            Vector2 nextScale = ResolveLineScaleForVisualSize(data, targetSize);
            Rect targetRect = ResolveScaledRect(handleType, _scaleStartRect, targetSize, scaleFromCenter, keepOppositeCornerFixed);
            data.scale = nextScale;

            Vector2 actualSize = CalculateActorVisualSize(data, nextScale);
            Vector2 pivot = data.EffectivePivot;
            Vector2 offset = data.EffectiveOffset;
            float camW = DefaultUnitPixels;
            float camH = DefaultUnitPixels / GetRenderAspect();

            data.normalizedPosition = new Vector2(
                (targetRect.x + actualSize.x * Mathf.Clamp01(pivot.x)) / camW - offset.x,
                1f - (targetRect.y + actualSize.y * (1f - Mathf.Clamp01(pivot.y))) / camH - offset.y);
        }

        private Vector2 ResolveLineScaleForVisualSize(StoryActorStateData data, Vector2 targetSize)
        {
            StoryActorStateData baseSample = data.ShallowClone();
            baseSample.scale = Vector2.one;
            float camH = DefaultUnitPixels / GetRenderAspect();
            Vector2 baseSize = StoryStageVisualSizing.CalculateActorPreviewSize(
                baseSample,
                StoryStageVisualSizing.ResolveActorSprite(baseSample),
                new Vector2(DefaultUnitPixels, camH));
            float lineX = targetSize.x / Mathf.Max(0.0001f, baseSize.x);
            float lineY = targetSize.y / Mathf.Max(0.0001f, baseSize.y);
            return ResolveNonZeroScale(new Vector2(lineX, lineY));
        }

        private static Rect ResolveScaledRect(
            ActorScaleHandle handleType,
            Rect startRect,
            Vector2 targetSize,
            bool scaleFromCenter,
            bool keepOppositeCornerFixed)
        {
            if (scaleFromCenter)
            {
                Vector2 center = startRect.center;
                return new Rect(center.x - targetSize.x * 0.5f, center.y - targetSize.y * 0.5f, targetSize.x, targetSize.y);
            }

            // Ctrl explicitly pins the corner opposite to the dragged handle.
            // The non-center fallback uses the same stable behavior for now.
            return handleType switch
            {
                ActorScaleHandle.TopLeft => new Rect(startRect.xMax - targetSize.x, startRect.yMax - targetSize.y, targetSize.x, targetSize.y),
                ActorScaleHandle.TopRight => new Rect(startRect.xMin, startRect.yMax - targetSize.y, targetSize.x, targetSize.y),
                ActorScaleHandle.BottomLeft => new Rect(startRect.xMax - targetSize.x, startRect.yMin, targetSize.x, targetSize.y),
                _ => new Rect(startRect.xMin, startRect.yMin, targetSize.x, targetSize.y)
            };
        }

        private static Vector2 ApplyAxisLock(Vector2 delta, bool shiftKey, ref DragAxisLock axisLock)
        {
            if (!shiftKey)
            {
                axisLock = DragAxisLock.None;
                return delta;
            }

            if (axisLock == DragAxisLock.None)
                axisLock = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? DragAxisLock.X : DragAxisLock.Y;

            return axisLock == DragAxisLock.X
                ? new Vector2(delta.x, 0f)
                : new Vector2(0f, delta.y);
        }

        private void PositionSelectedActorElement()
        {
            if (_selectionKind != StageSelectionKind.Actor
                || string.IsNullOrWhiteSpace(_selectedActorKey)
                || !_stageState.TryGetValue(_selectedActorKey, out var data)
                || !_actorElements.TryGetValue(_selectedActorKey, out var el))
                return;

            PositionActorElement(el, data);
        }

        private void HighlightSelectedActor()
        {
            foreach (var kvp in _actorElements)
            {
                bool  sel = IsStageAuthoringMode && _selectionKind == StageSelectionKind.Actor && kvp.Key == _selectedActorKey;
                float bw  = sel ? SelectionBorderWidth : 0f;
                kvp.Value.style.borderTopWidth    = kvp.Value.style.borderRightWidth    =
                kvp.Value.style.borderBottomWidth = kvp.Value.style.borderLeftWidth = bw;
                kvp.Value.style.borderTopColor    = kvp.Value.style.borderRightColor    =
                kvp.Value.style.borderBottomColor = kvp.Value.style.borderLeftColor =
                    new StyleColor(SelectionBorderColor);
            }
        }
    }
}
