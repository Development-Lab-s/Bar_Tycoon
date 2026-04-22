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
            var content = new VisualElement { style = { flexDirection = FlexDirection.Column, minHeight = 120 } };
            scroll.Add(content);

            _timelineRuler = new VisualElement { style = { flexDirection = FlexDirection.Row, height = 28, flexShrink = 0 } };
            _timelineRows = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            content.Add(_timelineRuler);
            content.Add(_timelineRows);
            _timelinePanel.Add(scroll);

            _timelinePanel.RegisterCallback<WheelEvent>(OnTimelineWheel);
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
                AddTimelineEmpty("No actor track. Use Add Property or Add Key.");
                return;
            }

            BuildActorTimelineRow("Position", track, StoryActorKeyframeProperty.Position);
            BuildActorTimelineRow("Scale", track, StoryActorKeyframeProperty.Scale);
            BuildActorTimelineRow("Easing", track, StoryActorKeyframeProperty.Easing);
        }

        private void BuildActorTimelineRow(string label, StoryActorTrackData track, StoryActorKeyframeProperty property)
        {
            var row = CreateTimelineRow(label);
            float duration = Mathf.Max(2f, GetTimelineDuration() + 0.5f, _timelinePlayheadTime + 0.5f);
            var lane = CreateTimelineLane(duration, TimelineRowHeight, property);

            for (int i = 0; i < track.keyframes.Count; i++)
            {
                StoryActorKeyframeData keyframe = track.keyframes[i];
                if (keyframe == null || keyframe.property != property) continue;
                AddActorKeyMarker(lane, i, keyframe, property);
            }

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
                _selectedTimelineProperty = property;
                _isTimelinePlayheadDragging = true;
                lane.CapturePointer(e.pointerId);
                SetTimelinePlayheadFromLocalX(e.localPosition.x);
                e.StopPropagation();
            });
            lane.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_isTimelinePlayheadDragging) return;
                SetTimelinePlayheadFromLocalX(e.localPosition.x);
                e.StopPropagation();
            });
            lane.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!_isTimelinePlayheadDragging) return;
                _isTimelinePlayheadDragging = false;
                lane.ReleasePointer(e.pointerId);
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
                if (e.button == 1)
                {
                    ShowEasingMenu(index, property);
                    e.StopPropagation();
                    return;
                }
                if (e.button != 0) return;
                _selectedTimelineKeyIndex = index;
                _selectedTimelineProperty = property;
                _isTimelineKeyDragging = true;
                _draggingTimelineActorKey = _selectedActorKey;
                _draggingTimelineKeyIndex = index;
                marker.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            marker.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_isTimelineKeyDragging || _draggingTimelineKeyIndex != index || _draggingTimelineActorKey != _selectedActorKey) return;
                float laneX = Mathf.Max(0f, e.localPosition.x + marker.resolvedStyle.left);
                float time = Mathf.Max(0f, laneX / Mathf.Max(1f, _timelinePixelsPerSecond));
                SetActorKeyTime(_selectedActorKey, index, time, refresh: false);
                marker.style.left = time * _timelinePixelsPerSecond - TimelineMarkerSize * 0.5f;
                _timelinePlayheadTime = time;
                ApplyTimelinePlayheadSample();
                e.StopPropagation();
            });

            marker.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!_isTimelineKeyDragging || _draggingTimelineKeyIndex != index) return;
                _isTimelineKeyDragging = false;
                _draggingTimelineActorKey = null;
                _draggingTimelineKeyIndex = -1;
                marker.ReleasePointer(e.pointerId);
                RefreshTimelinePanel();
                e.StopPropagation();
            });
            lane.Add(marker);
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

        private void AddTimelineEmpty(string message)
        {
            _timelineRows.Add(new Label(message)
            {
                style =
                {
                    paddingLeft = 8,
                    paddingTop = 8,
                    color = new StyleColor(new Color(0.58f, 0.60f, 0.66f)),
                    whiteSpace = WhiteSpace.Normal
                }
            });
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
                || !_stageState.TryGetValue(_selectedActorKey, out var baseState))
                return;
            StoryActorTrackData track = FindActorTrack(FindCurrentStageLayout(), _selectedActorKey);
            StoryActorStateData sample = StoryTransitionSampler.SampleActorTrackAtTime(baseState, track, _timelinePlayheadTime);
            if (sample == null) return;
            _stageState[_selectedActorKey] = sample;
            UpdateActorLayerPositions();
        }

        private void ShowAddPropertyMenu()
        {
            if (_selectionKind != StageSelectionKind.Actor || string.IsNullOrWhiteSpace(_selectedActorKey)) return;
            StoryActorTrackData track = FindActorTrack(FindCurrentStageLayout(), _selectedActorKey);
            var menu = new GenericMenu();
            AddPropertyMenuItem(menu, track, StoryActorKeyframeProperty.Position, "Position");
            AddPropertyMenuItem(menu, track, StoryActorKeyframeProperty.Scale, "Scale");
            AddPropertyMenuItem(menu, track, StoryActorKeyframeProperty.Easing, "Easing");
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
            AddOrUpdateKey(_selectedActorKey, actorState, _selectedTimelineProperty, _timelinePlayheadTime, createIfMissing: true);
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
            int count = track?.keyframes?.Count ?? 0;
            if (count == 0) return;
            if (offset == int.MinValue) _selectedTimelineKeyIndex = 0;
            else if (offset == int.MaxValue) _selectedTimelineKeyIndex = count - 1;
            else _selectedTimelineKeyIndex = Mathf.Clamp(_selectedTimelineKeyIndex + offset, 0, count - 1);
            _selectedTimelineProperty = track.keyframes[_selectedTimelineKeyIndex].property;
            _timelinePlayheadTime = StoryTransitionSampler.GetKeyTime(track.keyframes[_selectedTimelineKeyIndex]);
            ApplyTimelinePlayheadSample();
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
                    _selectedTimelineKeyIndex = track.keyframes.Count - 1;
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
            }, refresh: true);
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
            SaveActorTrackToCurrent(_selectedActorKey, track =>
            {
                StoryActorKeyframeData clone = _timelineClipboardKey.ShallowClone();
                clone.property = _timelineClipboardProperty;
                clone.timeSeconds = _timelinePlayheadTime;
                clone.normalizedTime = Mathf.Clamp01(_timelinePlayheadTime / Mathf.Max(1f, GetTimelineDuration()));
                track.keyframes.Add(clone);
                _selectedTimelineKeyIndex = track.keyframes.Count - 1;
                _selectedTimelineProperty = clone.property;
            }, refresh: true);
            RefreshTimelinePanel();
        }

        private bool HandleTimelineShortcut(Event e)
        {
            if (!IsStageAuthoringMode || e == null || e.type != EventType.KeyDown) return false;
            if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
            {
                if (_selectedTimelineKeyIndex >= 0)
                {
                    RemoveSelectedTimelineKey();
                    return true;
                }
                return false;
            }
            if (e.control && e.keyCode == KeyCode.C)
            {
                CopySelectedTimelineKey();
                return true;
            }
            if (e.control && e.keyCode == KeyCode.V)
            {
                PasteTimelineKeyAtPlayhead();
                return true;
            }
            return false;
        }
    }
}
