using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        private const float TimelinePropertyWidth = 130f;
        private const float TimelineRowHeight = 24f;
        private const float TimelineMarkerSize = 10f;
        private const float TimelineKeyHitSeconds = 0.04f;

        private VisualElement BuildTimelineResizeHandle()
        {
            _timelineResizeHandle = new VisualElement
            {
                name = "StoryPreviewTimelineResizeHandle",
                style =
                {
                    height = 5,
                    flexShrink = 0,
                    backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f))
                }
            };

            _timelineResizeHandle.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode || e.button != 0) return;
                _isTimelineResizing = true;
                _timelineResizeStartY = e.position.y;
                _timelineResizeStartHeight = _timelineHeight;
                _timelineResizeHandle.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            _timelineResizeHandle.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_isTimelineResizing) return;
                float delta = _timelineResizeStartY - e.position.y;
                _timelineHeight = Mathf.Clamp(_timelineResizeStartHeight + delta, MinTimelineHeight, MaxTimelineHeight);
                if (_timelinePanel != null) _timelinePanel.style.height = _timelineHeight;
                e.StopPropagation();
            });

            _timelineResizeHandle.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!_isTimelineResizing) return;
                _isTimelineResizing = false;
                _timelineResizeHandle.ReleasePointer(e.pointerId);
                SavePreviewLayoutPrefs();
                e.StopPropagation();
            });

            return _timelineResizeHandle;
        }

        private VisualElement BuildTimelinePanel()
        {
            _timelinePanel = new VisualElement
            {
                name = "StoryPreviewKeyframeEditor",
                style =
                {
                    height = _timelineHeight,
                    flexShrink = 0,
                    flexDirection = FlexDirection.Column,
                    backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.135f)),
                    borderTopWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.22f, 0.22f, 0.24f))
                }
            };
            _timelinePanel.focusable = true;

            _timelineToolbar = new VisualElement
            {
                style =
                {
                    height = 30,
                    flexShrink = 0,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 6,
                    paddingRight = 6
                }
            };

            _timelineTitleLabel = new Label("Keyframe Editor")
            {
                style =
                {
                    width = 150,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(new Color(0.86f, 0.86f, 0.9f))
                }
            };
            _timelineToolbar.Add(_timelineTitleLabel);

            _timelinePlayBtn = MakeBtn("Play", new Color(0.20f, 0.35f, 0.25f), ToggleTimelinePlayback);
            _timelineRecordBtn = MakeBtn("Record", new Color(0.36f, 0.18f, 0.18f), ToggleTimelineRecord);
            _timelineToolbar.Add(_timelinePlayBtn);
            _timelineToolbar.Add(MakeBtn("|<", new Color(0.23f, 0.23f, 0.27f), SelectFirstTimelineKey));
            _timelineToolbar.Add(MakeBtn("<", new Color(0.23f, 0.23f, 0.27f), SelectPreviousTimelineKey));
            _timelineToolbar.Add(MakeBtn(">", new Color(0.23f, 0.23f, 0.27f), SelectNextTimelineKey));
            _timelineToolbar.Add(MakeBtn(">|", new Color(0.23f, 0.23f, 0.27f), SelectLastTimelineKey));
            _timelineToolbar.Add(_timelineRecordBtn);
            _timelineToolbar.Add(MakeBtn("Add Property", new Color(0.24f, 0.28f, 0.34f), ShowAddPropertyMenu));
            _timelineToolbar.Add(MakeBtn("Add Key", new Color(0.24f, 0.30f, 0.24f), AddTimelineKeyAtPlayhead));
            _timelineToolbar.Add(MakeBtn("Remove Key", new Color(0.34f, 0.20f, 0.20f), RemoveSelectedTimelineKey));

            var speedSlider = new Slider(0.25f, 4f)
            {
                value = _timelinePlaybackSpeed,
                style = { width = 90, marginLeft = 6, marginRight = 4 }
            };
            speedSlider.RegisterValueChangedCallback(e =>
            {
                _timelinePlaybackSpeed = Mathf.Max(0.1f, e.newValue);
                if (_timelineSpeedField != null) _timelineSpeedField.SetValueWithoutNotify(_timelinePlaybackSpeed);
                SavePreviewLayoutPrefs();
            });
            _timelineToolbar.Add(speedSlider);

            _timelineSpeedField = new FloatField("Speed")
            {
                value = _timelinePlaybackSpeed,
                style = { width = 88 }
            };
            _timelineSpeedField.RegisterValueChangedCallback(e =>
            {
                _timelinePlaybackSpeed = Mathf.Max(0.1f, e.newValue);
                speedSlider.SetValueWithoutNotify(Mathf.Clamp(_timelinePlaybackSpeed, 0.25f, 4f));
                SavePreviewLayoutPrefs();
            });
            _timelineToolbar.Add(_timelineSpeedField);

            _timelinePanel.Add(_timelineToolbar);

            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
            {
                style = { flexGrow = 1 }
            };
            var content = new VisualElement { style = { flexDirection = FlexDirection.Column, minHeight = 170 } };
            scroll.Add(content);

            _timelineRuler = new VisualElement { style = { flexDirection = FlexDirection.Row, height = 28, flexShrink = 0 } };
            _timelineRows = new VisualElement { style = { flexDirection = FlexDirection.Column, minHeight = 96 } };
            content.Add(_timelineRuler);
            content.Add(_timelineRows);
            _timelinePanel.Add(scroll);

            _timelinePanel.RegisterCallback<KeyDownEvent>(OnTimelineKeyDown);
            _timelinePanel.RegisterCallback<PointerDownEvent>(OnTimelinePanelPointerDown);
            _timelinePanel.RegisterCallback<WheelEvent>(OnTimelineWheel);
            _timelinePanel.RegisterCallback<PointerMoveEvent>(OnTimelinePointerMove);
            _timelinePanel.RegisterCallback<PointerUpEvent>(OnTimelinePointerUp);
            _timelinePanel.RegisterCallback<PointerCaptureOutEvent>(_ => CancelTimelinePointerDrag());
            RefreshTimelinePanel();
            return _timelinePanel;
        }

        private void RefreshTimelinePanel()
        {
            if (_timelinePanel == null || _timelineRows == null || _timelineRuler == null) return;

            bool visible = IsStageAuthoringMode;
            SetElementVisible(_timelinePanel, visible);
            SetElementVisible(_timelineResizeHandle, visible);
            if (!visible) return;

            _timelinePanel.style.height = _timelineHeight;
            _timelineRows.Clear();
            _timelineRuler.Clear();

            if (_timelineTitleLabel != null) _timelineTitleLabel.text = ResolveTimelineTitle();
            if (_timelineRecordBtn != null) _timelineRecordBtn.text = _timelineRecordEnabled ? "Record On" : "Record";
            if (_timelinePlayBtn != null) _timelinePlayBtn.text = _timelineIsPlaying ? "Stop" : "Play";
            if (_timelineSpeedField != null) _timelineSpeedField.SetValueWithoutNotify(_timelinePlaybackSpeed);

            BuildTimelineRuler();

            if (_currentLine == null)
            {
                AddTimelineEmpty("Select a story line.");
                return;
            }

            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
            {
                BuildActorTimelineRows();
                return;
            }

            if (_selectionKind == StageSelectionKind.Background)
            {
                BuildBackgroundTimelineRows();
                return;
            }

            AddTimelineEmpty("Select an actor or background in Stage Authoring.");
        }

        private string ResolveTimelineTitle()
        {
            if (_timelineRecordEnabled && !string.IsNullOrWhiteSpace(_timelineRecordActorKey))
                return $"REC: {_timelineRecordActorKey}";
            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
                return $"Actor Track: {_selectedActorKey}";
            return _selectionKind == StageSelectionKind.Background ? "Background Transition" : "Keyframe Editor";
        }

        private void BuildTimelineRuler()
        {
            _timelineRuler.Add(new Label("Property")
            {
                style =
                {
                    width = TimelinePropertyWidth,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    paddingLeft = 6,
                    color = new StyleColor(new Color(0.68f, 0.68f, 0.72f))
                }
            });

            float duration = Mathf.Max(2f, GetTimelineDuration() + 0.5f, _timelinePlayheadTime + 0.5f);
            var lane = CreateTimelineLane(duration, 28f, StoryActorKeyframeProperty.Position);
            float tick = ResolveTimelineTickSeconds();
            for (float t = 0f; t <= duration + 0.001f; t += tick) AddTimelineTick(lane, t, true);
            AddTimelinePlayhead(lane, duration, 28f);
            _timelineRuler.Add(lane);
        }

        private void BuildActorTimelineRows()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StoryActorTrackData track = FindActorTrack(layout, _selectedActorKey);

            if (track == null || track.keyframes.Count == 0)
            {
                AddTimelineEmpty("No actor track. Use Add Property.");
                return;
            }

            bool hasVisibleRow = false;
            if (HasProperty(track, StoryActorKeyframeProperty.Position))
            {
                BuildActorTimelineRow("Position", track, StoryActorKeyframeProperty.Position);
                hasVisibleRow = true;
            }
            if (HasProperty(track, StoryActorKeyframeProperty.Scale))
            {
                BuildActorTimelineRow("Scale", track, StoryActorKeyframeProperty.Scale);
                hasVisibleRow = true;
            }

            if (!hasVisibleRow)
                AddTimelineEmpty("No editable property row. Use Add Property.");
        }

        private void BuildActorTimelineRow(string label, StoryActorTrackData track, StoryActorKeyframeProperty property)
        {
            var row = CreateTimelineRow(label);
            float duration = Mathf.Max(2f, GetTimelineDuration() + 0.5f, _timelinePlayheadTime + 0.5f);
            var lane = CreateTimelineLane(duration, TimelineRowHeight, property);

            var indexes = GetOrderedKeyIndexes(track, property);
            for (int i = 0; i < indexes.Count - 1; i++)
                AddSegmentBar(lane, track, indexes[i], indexes[i + 1]);

            foreach (int keyIndex in indexes)
                AddActorKeyMarker(lane, keyIndex, track.keyframes[keyIndex], property);

            AddTimelinePlayhead(lane, duration, TimelineRowHeight);
            row.Add(lane);
            _timelineRows.Add(row);
        }

        private void BuildBackgroundTimelineRows()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StoryBackgroundStateData state = layout != null ? layout.Background : _bgState;
            if (state == null || !state.HasBackground)
            {
                AddTimelineEmpty("No background state on this line.");
                return;
            }

            var row = CreateTimelineRow("Transition");
            float duration = Mathf.Max(2f, state.transitionDuration + 0.5f);
            var lane = CreateTimelineLane(duration, TimelineRowHeight, StoryActorKeyframeProperty.Easing);
            AddBackgroundTransitionMarker(lane, state);
            AddTimelinePlayhead(lane, duration, TimelineRowHeight);
            row.Add(lane);
            _timelineRows.Add(row);
        }

        private VisualElement CreateTimelineRow(string label)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, height = TimelineRowHeight, flexShrink = 0 } };
            row.Add(new Label(label)
            {
                style =
                {
                    width = TimelinePropertyWidth,
                    paddingLeft = 6,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    color = new StyleColor(new Color(0.78f, 0.78f, 0.82f))
                }
            });
            return row;
        }

        private VisualElement CreateTimelineLane(float duration, float height, StoryActorKeyframeProperty property)
        {
            var lane = new VisualElement
            {
                style =
                {
                    position = Position.Relative,
                    width = Mathf.Max(360f, duration * _timelinePixelsPerSecond + 32f),
                    height = height,
                    backgroundColor = new StyleColor(new Color(0.095f, 0.095f, 0.105f))
                }
            };

            lane.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode || e.button != 0) return;
                _timelinePanel?.Focus();
                _selectedTimelineProperty = property;
                ClearTimelineSelection(refresh: false);
                _isTimelinePlayheadDragging = true;
                _activeTimelinePointerId = e.pointerId;
                _timelinePanel?.CapturePointer(e.pointerId);
                SetTimelinePlayheadFromLocalX(e.localPosition.x);
                RefreshActorInspector();
                RefreshTimelinePanel();
                e.StopPropagation();
            });

            return lane;
        }

        private void AddActorKeyMarker(VisualElement lane, int index, StoryActorKeyframeData keyframe, StoryActorKeyframeProperty property)
        {
            float x = StoryTransitionSampler.GetKeyTime(keyframe) * _timelinePixelsPerSecond;
            bool selected = index == _selectedTimelineKeyIndex && property == _selectedTimelineProperty;
            var marker = new VisualElement
            {
                name = $"ActorKey_{property}_{index}",
                tooltip = $"{property} {StoryTransitionSampler.GetKeyTime(keyframe):0.00}s",
                style =
                {
                    position = Position.Absolute,
                    left = x - TimelineMarkerSize * 0.5f,
                    top = (TimelineRowHeight - TimelineMarkerSize) * 0.5f,
                    width = TimelineMarkerSize,
                    height = TimelineMarkerSize,
                    backgroundColor = new StyleColor(selected ? new Color(1f, 0.82f, 0.22f) : ResolveKeyColor(property)),
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

            marker.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                _timelinePanel?.Focus();
                _selectedTimelineKeyIndex = index;
                _selectedTimelineProperty = property;
                _selectedTimelineSegmentKeyIndex = -1;
                _isTimelineKeyDragging = true;
                _draggingTimelineActorKey = _selectedActorKey;
                _draggingTimelineKeyIndex = index;
                _activeTimelinePointerId = e.pointerId;
                _timelinePanel?.CapturePointer(e.pointerId);
                _timelinePlayheadTime = StoryTransitionSampler.GetKeyTime(keyframe);
                ApplyTimelinePlayheadSample();
                RefreshActorInspector();
                RefreshTimelinePanel();
                e.StopPropagation();
            });
            lane.Add(marker);
        }

        private void AddSegmentBar(VisualElement lane, StoryActorTrackData track, int fromIndex, int toIndex)
        {
            StoryActorKeyframeData from = track.keyframes[fromIndex];
            StoryActorKeyframeData to = track.keyframes[toIndex];
            float fromX = StoryTransitionSampler.GetKeyTime(from) * _timelinePixelsPerSecond;
            float toX = StoryTransitionSampler.GetKeyTime(to) * _timelinePixelsPerSecond;
            bool selected = _selectedTimelineSegmentKeyIndex == fromIndex
                && _selectedTimelineSegmentProperty == from.property;
            var bar = new VisualElement
            {
                tooltip = $"{from.property} segment {from.easing}",
                style =
                {
                    position = Position.Absolute,
                    left = fromX,
                    top = TimelineRowHeight * 0.5f - (selected ? 2 : 1),
                    width = Mathf.Max(2f, toX - fromX),
                    height = selected ? 4 : 2,
                    backgroundColor = new StyleColor(selected
                        ? new Color(1f, 0.60f, 0.18f, 0.88f)
                        : new Color(0.74f, 0.74f, 0.78f, 0.55f))
                }
            };
            bar.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 && e.button != 1) return;
                SelectTimelineSegment(fromIndex, from.property);
                _timelinePlayheadTime = StoryTransitionSampler.GetKeyTime(from);
                ApplyTimelinePlayheadSample();
                if (e.button == 1)
                    ShowEasingMenu(fromIndex, from.property);
                else
                    RefreshTimelinePanel();
                RefreshActorInspector();
                e.StopPropagation();
            });
            lane.Add(bar);
        }

        private Color ResolveKeyColor(StoryActorKeyframeProperty property) =>
            property switch
            {
                StoryActorKeyframeProperty.Position => new Color(0.38f, 0.62f, 1f),
                StoryActorKeyframeProperty.Scale => new Color(0.60f, 0.82f, 0.35f),
                StoryActorKeyframeProperty.Easing => new Color(0.82f, 0.56f, 1f),
                _ => Color.white
            };

        private void AddBackgroundTransitionMarker(VisualElement lane, StoryBackgroundStateData state)
        {
            float x = state.transitionDuration * _timelinePixelsPerSecond;
            var marker = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = x - TimelineMarkerSize * 0.5f,
                    top = (TimelineRowHeight - TimelineMarkerSize) * 0.5f,
                    width = TimelineMarkerSize,
                    height = TimelineMarkerSize,
                    backgroundColor = new StyleColor(new Color(0.45f, 0.78f, 0.55f)),
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
            marker.RegisterCallback<PointerMoveEvent>(e =>
            {
                if ((e.pressedButtons & 1) == 0) return;
                float laneX = Mathf.Max(0f, e.localPosition.x + marker.resolvedStyle.left);
                float time = Mathf.Max(0.05f, laneX / Mathf.Max(1f, _timelinePixelsPerSecond));
                SaveBackgroundStateToCurrent(bg => bg.transitionDuration = time);
                marker.style.left = time * _timelinePixelsPerSecond - TimelineMarkerSize * 0.5f;
                e.StopPropagation();
            });
            lane.Add(marker);
        }

        private void AddTimelineTick(VisualElement lane, float time, bool withLabel)
        {
            float x = time * _timelinePixelsPerSecond;
            lane.Add(new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = x,
                    top = 0,
                    width = 1,
                    bottom = 0,
                    backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.24f))
                }
            });
            if (!withLabel) return;
            lane.Add(new Label($"{time:0.0}s")
            {
                style =
                {
                    position = Position.Absolute,
                    left = x + 3,
                    top = 3,
                    fontSize = 9,
                    color = new StyleColor(new Color(0.62f, 0.62f, 0.66f))
                }
            });
        }

        private void AddTimelinePlayhead(VisualElement lane, float duration, float height)
        {
            float x = Mathf.Clamp(_timelinePlayheadTime, 0f, duration) * _timelinePixelsPerSecond;
            lane.Add(new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = x,
                    top = 0,
                    width = 2,
                    height = height,
                    backgroundColor = new StyleColor(new Color(1f, 0.38f, 0.22f, 0.95f))
                }
            });
        }

        private static System.Collections.Generic.List<int> GetOrderedKeyIndexes(StoryActorTrackData track, StoryActorKeyframeProperty property)
        {
            var indexes = new System.Collections.Generic.List<int>();
            if (track == null || track.keyframes == null)
                return indexes;

            for (int i = 0; i < track.keyframes.Count; i++)
            {
                if (track.keyframes[i] != null && track.keyframes[i].property == property)
                    indexes.Add(i);
            }

            indexes.Sort((a, b) => StoryTransitionSampler.GetKeyTime(track.keyframes[a]).CompareTo(StoryTransitionSampler.GetKeyTime(track.keyframes[b])));
            return indexes;
        }

        private void AddTimelineEmpty(string message)
        {
            var empty = new Label(message)
            {
                style =
                {
                    paddingLeft = 8,
                    paddingTop = 8,
                    minHeight = 76,
                    color = new StyleColor(new Color(0.58f, 0.60f, 0.66f)),
                    whiteSpace = WhiteSpace.Normal
                }
            };
            empty.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                ClearTimelineSelection(refresh: true);
                e.StopPropagation();
            });
            _timelineRows.Add(empty);
        }

        private float ResolveTimelineTickSeconds()
        {
            if (_timelinePixelsPerSecond >= 220f) return 0.25f;
            if (_timelinePixelsPerSecond <= 70f) return 1f;
            return 0.5f;
        }

        private float GetTimelineDuration()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
                return Mathf.Max(1f, StoryTransitionSampler.GetActorTrackDuration(FindActorTrack(layout, _selectedActorKey)));
            if (_selectionKind == StageSelectionKind.Background)
                return Mathf.Max(1f, (layout?.Background ?? _bgState)?.transitionDuration ?? 1f);
            return 1f;
        }

        private void SetTimelinePlayheadFromLocalX(float localX)
        {
            _timelinePlayheadTime = Mathf.Max(0f, localX / Mathf.Max(1f, _timelinePixelsPerSecond));
            ApplyTimelinePlayheadSample();
            RefreshTimelinePanel();
        }

        private void OnTimelinePointerMove(PointerMoveEvent e)
        {
            if (e.pointerId != _activeTimelinePointerId)
                return;

            if (_isTimelinePlayheadDragging)
            {
                SetTimelinePlayheadFromLocalX(e.localPosition.x - TimelinePropertyWidth);
                e.StopPropagation();
                return;
            }

            if (_isTimelineKeyDragging && !string.IsNullOrWhiteSpace(_draggingTimelineActorKey))
            {
                float time = Mathf.Max(0f, (e.localPosition.x - TimelinePropertyWidth) / Mathf.Max(1f, _timelinePixelsPerSecond));
                SetActorKeyTime(_draggingTimelineActorKey, _draggingTimelineKeyIndex, time, refresh: false);
                _draggingTimelineKeyIndex = _selectedTimelineKeyIndex;
                _timelinePlayheadTime = time;
                ApplyTimelinePlayheadSample();
                RefreshTimelinePanel();
                e.StopPropagation();
            }
        }

        private void OnTimelinePointerUp(PointerUpEvent e)
        {
            if (e.pointerId != _activeTimelinePointerId)
                return;

            CancelTimelinePointerDrag();
            e.StopPropagation();
        }

        private void OnTimelineKeyDown(KeyDownEvent e)
        {
            if (IsFocusedOnTextInput(rootVisualElement))
                return;

            if (HandleTimelineShortcut(e.keyCode, e.ctrlKey || e.commandKey, e.shiftKey))
            {
                e.StopPropagation();
                e.PreventDefault();
            }
        }

        private void OnTimelinePanelPointerDown(PointerDownEvent e)
        {
            if (!IsStageAuthoringMode || e.button != 0 || _activeTimelinePointerId >= 0)
                return;

            if (!IsTimelineNeutralClickTarget(e.target as VisualElement))
                return;

            _timelinePanel?.Focus();
            ClearTimelineSelection(refresh: true);
            e.StopPropagation();
        }

        private bool IsTimelineNeutralClickTarget(VisualElement target)
        {
            if (target == null)
                return false;

            if (IsDescendantOf(target, _timelineToolbar))
                return false;

            for (VisualElement current = target; current != null; current = current.parent)
            {
                if (current is Button || current is BaseField<float> || current is BaseField<int> || current is BaseField<string>)
                    return false;
            }

            return target == _timelinePanel
                || target == _timelineRows
                || target == _timelineRuler
                || target is Label;
        }

        private static bool IsDescendantOf(VisualElement element, VisualElement ancestor)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current == ancestor)
                    return true;
            }

            return false;
        }

        private void CancelTimelinePointerDrag()
        {
            _isTimelinePlayheadDragging = false;
            _isTimelineKeyDragging = false;
            _draggingTimelineActorKey = null;
            _draggingTimelineKeyIndex = -1;

            if (_timelinePanel != null && _activeTimelinePointerId >= 0 && _timelinePanel.HasPointerCapture(_activeTimelinePointerId))
                _timelinePanel.ReleasePointer(_activeTimelinePointerId);

            _activeTimelinePointerId = -1;
        }

        private void ClearTimelineSelection(bool refresh)
        {
            _selectedTimelineKeyIndex = -1;
            _selectedTimelineSegmentKeyIndex = -1;
            if (!refresh)
                return;

            RefreshActorInspector();
            RefreshTimelinePanel();
        }

        private void SelectTimelineSegment(int keyIndex, StoryActorKeyframeProperty property)
        {
            _selectedTimelineKeyIndex = -1;
            _selectedTimelineSegmentKeyIndex = keyIndex;
            _selectedTimelineSegmentProperty = property;
            _selectedTimelineProperty = property;
        }

        private void OnTimelineWheel(WheelEvent e)
        {
            if (!e.ctrlKey) return;
            float delta = -e.delta.y * 8f;
            _timelinePixelsPerSecond = Mathf.Clamp(_timelinePixelsPerSecond + delta, MinTimelinePixelsPerSecond, MaxTimelinePixelsPerSecond);
            SavePreviewLayoutPrefs();
            RefreshTimelinePanel();
            e.StopPropagation();
        }

        private void ToggleTimelineRecord()
        {
            if (!_timelineRecordEnabled)
            {
                if (_selectionKind != StageSelectionKind.Actor || string.IsNullOrWhiteSpace(_selectedActorKey)) return;
                _timelineRecordActorKey = _selectedActorKey;
                _timelineRecordEnabled = true;
            }
            else
            {
                _timelineRecordEnabled = false;
                _timelineRecordActorKey = null;
            }
            RefreshTimelinePanel();
        }

        private void ToggleTimelinePlayback()
        {
            if (_timelineIsPlaying)
            {
                _timelineIsPlaying = false;
                RefreshTimelinePanel();
                return;
            }
            _timelinePlaybackStartTime = _timelinePlayheadTime;
            _timelinePlaybackStartedAt = EditorApplication.timeSinceStartup;
            _timelineIsPlaying = true;
            RefreshTimelinePanel();
        }

        private void UpdateTimelinePlayback()
        {
            if (!_timelineIsPlaying || !IsStageAuthoringMode) return;
            float elapsed = (float)(EditorApplication.timeSinceStartup - _timelinePlaybackStartedAt) * Mathf.Max(0.1f, _timelinePlaybackSpeed);
            _timelinePlayheadTime = _timelinePlaybackStartTime + elapsed;
            float duration = GetTimelineDuration();
            if (_timelinePlayheadTime >= duration)
            {
                _timelinePlayheadTime = duration;
                _timelineIsPlaying = false;
            }
            ApplyTimelinePlayheadSample();
            RefreshTimelinePanel();
        }

        private void ApplyTimelinePlayheadSample()
        {
            if (_selectionKind != StageSelectionKind.Actor
                || string.IsNullOrWhiteSpace(_selectedActorKey)
                || !TryResolveTimelineBaseActorState(_selectedActorKey, out var baseState))
                return;
            StoryActorTrackData track = FindActorTrack(FindCurrentStageLayout(), _selectedActorKey);
            StoryActorStateData sample = StoryTransitionSampler.SampleActorTrackAtTime(baseState, track, _timelinePlayheadTime);
            if (sample == null) return;
            _stageState[_selectedActorKey] = sample;
            UpdateActorLayerPositions();
        }

        private bool TryResolveTimelineBaseActorState(string actorKey, out StoryActorStateData baseState)
        {
            baseState = null;
            if (string.IsNullOrWhiteSpace(actorKey))
                return false;

            StoryActorStateData currentLineEntry = FindActorEntry(FindCurrentStageLayout(), actorKey);
            if (currentLineEntry != null)
            {
                baseState = currentLineEntry.ShallowClone();
                return true;
            }

            if (_stageState.TryGetValue(actorKey, out StoryActorStateData currentState))
            {
                baseState = currentState.ShallowClone();
                return true;
            }

            return false;
        }

        private void ShowAddPropertyMenu()
        {
            if (_selectionKind != StageSelectionKind.Actor || string.IsNullOrWhiteSpace(_selectedActorKey)) return;
            StoryActorTrackData track = FindActorTrack(FindCurrentStageLayout(), _selectedActorKey);
            var menu = new GenericMenu();
            AddPropertyMenuItem(menu, track, StoryActorKeyframeProperty.Position, "Position");
            AddPropertyMenuItem(menu, track, StoryActorKeyframeProperty.Scale, "Scale");
            menu.ShowAsContext();
        }

        private void AddPropertyMenuItem(GenericMenu menu, StoryActorTrackData track, StoryActorKeyframeProperty property, string label)
        {
            if (HasProperty(track, property))
            {
                menu.AddDisabledItem(new GUIContent(label));
                return;
            }
            menu.AddItem(new GUIContent(label), false, () => AddPropertyKey(property));
        }

        private void AddPropertyKey(StoryActorKeyframeProperty property)
        {
            _selectedTimelineProperty = property;
            AddTimelineKeyAtPlayhead();
        }

        private static bool HasProperty(StoryActorTrackData track, StoryActorKeyframeProperty property)
        {
            if (track == null) return false;
            foreach (StoryActorKeyframeData key in track.keyframes)
                if (key != null && key.property == property) return true;
            return false;
        }

        private void AddTimelineKeyAtPlayhead()
        {
            if (_selectionKind != StageSelectionKind.Actor
                || string.IsNullOrWhiteSpace(_selectedActorKey)
                || !_stageState.TryGetValue(_selectedActorKey, out var actorState))
                return;
            StoryActorKeyframeProperty property = _selectedTimelineProperty == StoryActorKeyframeProperty.Easing
                ? StoryActorKeyframeProperty.Position
                : _selectedTimelineProperty;
            AddOrUpdateKey(_selectedActorKey, actorState, property, _timelinePlayheadTime, createIfMissing: true);
            RefreshTimelinePanel();
        }

        private void RemoveSelectedTimelineKey()
        {
            if (_selectionKind != StageSelectionKind.Actor || string.IsNullOrWhiteSpace(_selectedActorKey) || _selectedTimelineKeyIndex < 0) return;
            int index = _selectedTimelineKeyIndex;
            SaveActorTrackToCurrent(_selectedActorKey, track =>
            {
                if (index >= 0 && index < track.keyframes.Count) track.keyframes.RemoveAt(index);
                _selectedTimelineKeyIndex = -1;
                _selectedTimelineSegmentKeyIndex = -1;
            }, refresh: true);
            RefreshTimelinePanel();
        }

        private void SelectFirstTimelineKey() => SelectTimelineKeyByOffset(int.MinValue);
        private void SelectPreviousTimelineKey() => SelectTimelineKeyByOffset(-1);
        private void SelectNextTimelineKey() => SelectTimelineKeyByOffset(1);
        private void SelectLastTimelineKey() => SelectTimelineKeyByOffset(int.MaxValue);

        private void SelectTimelineKeyByOffset(int offset)
        {
            StoryActorTrackData track = FindActorTrack(FindCurrentStageLayout(), _selectedActorKey);
            var visibleIndexes = new System.Collections.Generic.List<int>();
            if (track?.keyframes != null)
            {
                for (int i = 0; i < track.keyframes.Count; i++)
                {
                    StoryActorKeyframeData key = track.keyframes[i];
                    if (key != null && IsEditableTimelineProperty(key.property))
                        visibleIndexes.Add(i);
                }
            }

            visibleIndexes.Sort((a, b) => StoryTransitionSampler.GetKeyTime(track.keyframes[a]).CompareTo(StoryTransitionSampler.GetKeyTime(track.keyframes[b])));
            int count = visibleIndexes.Count;
            if (count == 0) return;
            int currentVisibleIndex = Mathf.Max(0, visibleIndexes.IndexOf(_selectedTimelineKeyIndex));
            if (offset == int.MinValue) currentVisibleIndex = 0;
            else if (offset == int.MaxValue) currentVisibleIndex = count - 1;
            else currentVisibleIndex = Mathf.Clamp(currentVisibleIndex + offset, 0, count - 1);
            _selectedTimelineKeyIndex = visibleIndexes[currentVisibleIndex];
            _selectedTimelineProperty = track.keyframes[_selectedTimelineKeyIndex].property;
            _selectedTimelineSegmentKeyIndex = -1;
            _timelinePlayheadTime = StoryTransitionSampler.GetKeyTime(track.keyframes[_selectedTimelineKeyIndex]);
            ApplyTimelinePlayheadSample();
            RefreshActorInspector();
            RefreshTimelinePanel();
        }

        private void SetActorKeyTime(string actorKey, int keyIndex, float timeSeconds, bool refresh)
        {
            if (string.IsNullOrWhiteSpace(actorKey) || keyIndex < 0) return;
            SaveActorTrackToCurrent(actorKey, track =>
            {
                if (keyIndex >= track.keyframes.Count) return;
                track.keyframes[keyIndex].timeSeconds = Mathf.Max(0f, timeSeconds);
                track.keyframes[keyIndex].normalizedTime = Mathf.Clamp01(timeSeconds / Mathf.Max(1f, GetTimelineDuration()));
            }, refresh);
        }

        private void RecordActorKeyframeFromState(string actorKey, StoryActorStateData state, bool includePosition, bool includeScale)
        {
            if (!_timelineRecordEnabled || !IsStageAuthoringMode || state == null || string.IsNullOrWhiteSpace(actorKey)) return;
            if (!string.Equals(actorKey, _timelineRecordActorKey, StringComparison.Ordinal)) return;
            if (includePosition) AddOrUpdateKey(actorKey, state, StoryActorKeyframeProperty.Position, _timelinePlayheadTime, createIfMissing: true);
            if (includeScale) AddOrUpdateKey(actorKey, state, StoryActorKeyframeProperty.Scale, _timelinePlayheadTime, createIfMissing: true);
            RefreshTimelinePanel();
        }

        private bool BlocksActorManipulationBySelectedKey(string actorKey, StoryActorKeyframeProperty property)
        {
            return _selectedTimelineKeyIndex >= 0
                && string.Equals(actorKey, _selectedActorKey, StringComparison.Ordinal)
                && _selectedTimelineProperty != property;
        }

        private bool TryApplySelectedTimelineKeyFromState(string actorKey, StoryActorStateData state, StoryActorKeyframeProperty property)
        {
            if (state == null
                || string.IsNullOrWhiteSpace(actorKey)
                || _selectedTimelineKeyIndex < 0
                || !string.Equals(actorKey, _selectedActorKey, StringComparison.Ordinal)
                || _selectedTimelineProperty != property)
                return false;

            int keyIndex = _selectedTimelineKeyIndex;
            SaveActorTrackToCurrent(actorKey, track =>
            {
                if (keyIndex < 0 || keyIndex >= track.keyframes.Count)
                    return;

                StoryActorKeyframeData key = track.keyframes[keyIndex];
                if (key == null || key.property != property)
                    return;

                if (property == StoryActorKeyframeProperty.Position)
                    key.normalizedPosition = state.normalizedPosition;
                else if (property == StoryActorKeyframeProperty.Scale)
                {
                    key.scale = ResolveNonZeroScale(state.scale);
                    key.scaleX = state.scaleX;
                }
            }, refresh: false);

            ApplyTimelinePlayheadSample();
            RefreshActorInspector();
            RefreshTimelinePanel();
            return true;
        }

        private void AddOrUpdateKey(string actorKey, StoryActorStateData state, StoryActorKeyframeProperty property, float time, bool createIfMissing)
        {
            SaveActorTrackToCurrent(actorKey, track =>
            {
                int index = FindKeyIndexNearTime(track, property, time);
                StoryActorKeyframeData key;
                if (index >= 0) key = track.keyframes[index];
                else
                {
                    if (!createIfMissing) return;
                    key = new StoryActorKeyframeData { property = property };
                    track.keyframes.Add(key);
                }
                key.property = property;
                key.timeSeconds = Mathf.Max(0f, time);
                key.normalizedTime = Mathf.Clamp01(time / Mathf.Max(1f, GetTimelineDuration()));
                key.easing = state.moveMotion;
                if (property == StoryActorKeyframeProperty.Position) key.normalizedPosition = state.normalizedPosition;
                if (property == StoryActorKeyframeProperty.Scale)
                {
                    key.scale = state.scale;
                    key.scaleX = state.scaleX;
                }
                _selectedTimelineProperty = property;
                _selectedTimelineSegmentKeyIndex = -1;
            }, refresh: true);

            StoryActorTrackData refreshed = FindActorTrack(FindCurrentStageLayout(), actorKey);
            _selectedTimelineKeyIndex = FindKeyIndexNearTime(refreshed, property, time);
        }

        private static int FindKeyIndexNearTime(StoryActorTrackData track, StoryActorKeyframeProperty property, float timeSeconds)
        {
            if (track == null || track.keyframes == null) return -1;
            for (int i = 0; i < track.keyframes.Count; i++)
            {
                StoryActorKeyframeData key = track.keyframes[i];
                if (key != null
                    && key.property == property
                    && Mathf.Abs(StoryTransitionSampler.GetKeyTime(key) - timeSeconds) <= TimelineKeyHitSeconds)
                    return i;
            }
            return -1;
        }

        private void ValidateTimelineSelection()
        {
            if (_selectionKind != StageSelectionKind.Actor || string.IsNullOrWhiteSpace(_selectedActorKey))
            {
                _selectedTimelineKeyIndex = -1;
                _selectedTimelineSegmentKeyIndex = -1;
                return;
            }

            StoryActorTrackData track = FindActorTrack(FindCurrentStageLayout(), _selectedActorKey);
            if (track == null || track.keyframes == null)
            {
                _selectedTimelineKeyIndex = -1;
                _selectedTimelineSegmentKeyIndex = -1;
                return;
            }

            if (_selectedTimelineKeyIndex >= 0
                && (_selectedTimelineKeyIndex >= track.keyframes.Count
                    || track.keyframes[_selectedTimelineKeyIndex] == null
                    || track.keyframes[_selectedTimelineKeyIndex].property != _selectedTimelineProperty
                    || !IsEditableTimelineProperty(track.keyframes[_selectedTimelineKeyIndex].property)))
            {
                _selectedTimelineKeyIndex = -1;
            }

            if (_selectedTimelineSegmentKeyIndex >= 0
                && (_selectedTimelineSegmentKeyIndex >= track.keyframes.Count
                    || track.keyframes[_selectedTimelineSegmentKeyIndex] == null
                    || track.keyframes[_selectedTimelineSegmentKeyIndex].property != _selectedTimelineSegmentProperty
                    || FindNextKeyIndex(track, _selectedTimelineSegmentKeyIndex, _selectedTimelineSegmentProperty) < 0))
            {
                _selectedTimelineSegmentKeyIndex = -1;
            }
        }

        private static bool IsEditableTimelineProperty(StoryActorKeyframeProperty property)
        {
            return property == StoryActorKeyframeProperty.Position
                || property == StoryActorKeyframeProperty.Scale;
        }

        private static int FindNextKeyIndex(StoryActorTrackData track, int fromIndex, StoryActorKeyframeProperty property)
        {
            if (track == null || track.keyframes == null || fromIndex < 0 || fromIndex >= track.keyframes.Count)
                return -1;

            float fromTime = StoryTransitionSampler.GetKeyTime(track.keyframes[fromIndex]);
            int nextIndex = -1;
            float nextTime = float.MaxValue;
            for (int i = 0; i < track.keyframes.Count; i++)
            {
                StoryActorKeyframeData key = track.keyframes[i];
                if (key == null || key.property != property)
                    continue;

                float time = StoryTransitionSampler.GetKeyTime(key);
                if (time > fromTime && time < nextTime)
                {
                    nextTime = time;
                    nextIndex = i;
                }
            }

            return nextIndex;
        }

        private void ShowEasingMenu(int keyIndex, StoryActorKeyframeProperty property)
        {
            StoryActorTrackData track = FindActorTrack(FindCurrentStageLayout(), _selectedActorKey);
            if (track == null || keyIndex < 0 || keyIndex >= track.keyframes.Count) return;
            var menu = new GenericMenu();
            foreach (StoryStageMoveMotionType easing in Enum.GetValues(typeof(StoryStageMoveMotionType)))
            {
                StoryStageMoveMotionType captured = easing;
                menu.AddItem(new GUIContent(captured.ToString()), track.keyframes[keyIndex].easing == captured, () =>
                {
                    SaveActorTrackToCurrent(_selectedActorKey, currentTrack =>
                    {
                        if (keyIndex >= 0 && keyIndex < currentTrack.keyframes.Count)
                            currentTrack.keyframes[keyIndex].easing = captured;
                    }, refresh: true);
                    RefreshActorInspector();
                    RefreshTimelinePanel();
                });
            }
            menu.ShowAsContext();
        }

        private void CopySelectedTimelineKey()
        {
            StoryActorTrackData track = FindActorTrack(FindCurrentStageLayout(), _selectedActorKey);
            if (track == null || _selectedTimelineKeyIndex < 0 || _selectedTimelineKeyIndex >= track.keyframes.Count) return;
            _timelineClipboardKey = track.keyframes[_selectedTimelineKeyIndex].ShallowClone();
            _timelineClipboardProperty = _timelineClipboardKey.property;
        }

        private void PasteTimelineKeyAtPlayhead()
        {
            if (_timelineClipboardKey == null || string.IsNullOrWhiteSpace(_selectedActorKey)) return;
            if (_timelineClipboardProperty != _selectedTimelineProperty)
                return;
            SaveActorTrackToCurrent(_selectedActorKey, track =>
            {
                StoryActorKeyframeData clone = _timelineClipboardKey.ShallowClone();
                clone.property = _timelineClipboardProperty;
                clone.timeSeconds = _timelinePlayheadTime;
                clone.normalizedTime = Mathf.Clamp01(_timelinePlayheadTime / Mathf.Max(1f, GetTimelineDuration()));
                track.keyframes.Add(clone);
                _selectedTimelineProperty = clone.property;
                _selectedTimelineSegmentKeyIndex = -1;
            }, refresh: true);
            StoryActorTrackData refreshed = FindActorTrack(FindCurrentStageLayout(), _selectedActorKey);
            _selectedTimelineKeyIndex = FindKeyIndexNearTime(refreshed, _timelineClipboardProperty, _timelinePlayheadTime);
            RefreshTimelinePanel();
        }

        private bool HandleTimelineShortcut(KeyCode keyCode, bool control, bool shift)
        {
            if (!IsStageAuthoringMode)
                return false;

            if (control && keyCode == KeyCode.Z)
            {
                if (shift)
                    Undo.PerformRedo();
                else
                    Undo.PerformUndo();
                return true;
            }

            if (control && keyCode == KeyCode.Y)
            {
                Undo.PerformRedo();
                return true;
            }

            if (keyCode == KeyCode.Delete || keyCode == KeyCode.Backspace)
            {
                if (_selectedTimelineKeyIndex >= 0)
                {
                    RemoveSelectedTimelineKey();
                    return true;
                }
                return false;
            }
            if (control && keyCode == KeyCode.C)
            {
                CopySelectedTimelineKey();
                return true;
            }
            if (control && keyCode == KeyCode.V)
            {
                PasteTimelineKeyAtPlayhead();
                return true;
            }
            return false;
        }
    }
}
