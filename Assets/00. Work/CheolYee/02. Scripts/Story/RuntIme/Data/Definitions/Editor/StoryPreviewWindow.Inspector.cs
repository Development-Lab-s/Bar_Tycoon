using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        // ?? ?곗륫 ?몄뒪?숉꽣 ?⑤꼸 鍮뚮뱶 ??????????????????

        private VisualElement BuildActorInspector()
        {
            var panel = new VisualElement
            {
                style =
                {
                    width = _previewInspectorExpandedWidth, flexDirection = FlexDirection.Column,
                    borderLeftWidth = 1, borderLeftColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f)),
                    paddingLeft = 8, paddingRight = 8, paddingTop = 8, paddingBottom = 8
                }
            };
            _inspectorPanel = panel;

            _inspectorScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            panel.Add(_inspectorScrollView);

            _inspectorScrollView.Add(MakeBoldLabel("Aspect Settings"));
            _aspectSettingsField = new ObjectField("Aspect SO")
            {
                objectType = typeof(StoryAspectSettingsSO),
                allowSceneObjects = false,
                value = _previewAspectSettings,
                style = { marginBottom = 4 }
            };
            _aspectSettingsField.RegisterValueChangedCallback(e =>
            {
                _previewAspectSettings = e.newValue as StoryAspectSettingsSO;
                SaveAspectSettingsGuid();
                UpdateCameraFrameGeometry();
                float ww = _stageWrapper?.resolvedStyle.width ?? 0f;
                float wh = _stageWrapper?.resolvedStyle.height ?? 0f;
                if (ww > 0 && wh > 0) InitWorldView(ww, wh);
                RebuildActorLayer();
                RefreshAspectMetricsDisplay();
                Repaint();
            });
            _inspectorScrollView.Add(_aspectSettingsField);

            _aspectMetricsInfoLabel = new Label("")
            {
                style =
                {
                    fontSize = 9,
                    whiteSpace = WhiteSpace.Normal,
                    color = new StyleColor(new Color(0.58f, 0.64f, 0.72f)),
                    marginBottom = 4
                }
            };
            _inspectorScrollView.Add(_aspectMetricsInfoLabel);
            _inspectorScrollView.Add(MakeSeparator());

            _inspectorScrollView.Add(BuildAuthoringTools());
            _inspectorScrollView.Add(MakeSeparator());

            _inspectorScrollView.Add(MakeBoldLabel("Stage Actors"));

            _actorListRoot = new VisualElement { style = { flexDirection = FlexDirection.Column, marginBottom = 4 } };
            _inspectorScrollView.Add(_actorListRoot);
            _inspectorScrollView.Add(MakeSeparator());

            _inspectorRoot = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1 } };
            _inspectorScrollView.Add(_inspectorRoot);

            RefreshAspectMetricsDisplay();
            return panel;
        }

        private void RefreshAspectMetricsDisplay()
        {
            if (_aspectMetricsInfoLabel == null) return;

            float physical = GetPhysicalAspect();
            float visible  = GetStoryVisibleAspect();
            float ratio    = _previewAspectSettings?.VisibleWidthRatio ?? 1f;

            int renderW  = _renderResolution.x > 0 ? _renderResolution.x : FallbackRenderWidth;
            int renderH  = _renderResolution.y > 0 ? _renderResolution.y : FallbackRenderHeight;
            int visW     = Mathf.RoundToInt(renderW * ratio);
            int letterW  = Mathf.RoundToInt((renderW - visW) * 0.5f);

            _aspectMetricsInfoLabel.text =
                $"Physical: {physical:F3}  Visible: {visible:F3}\n" +
                $"View: {visW}×{renderH}  Letterbox: ±{letterW}px";
        }

        private VisualElement BuildAuthoringTools()
        {
            _authoringToolsRoot = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 6
                }
            };

            _authoringToolsRoot.Add(MakeBoldLabel("Stage Authoring"));

            _importPreviousStageBtn = new Button(OnImportPreviousStageClicked)
            {
                text = "Import Previous Stage",
                style = { height = 22, marginBottom = 4 }
            };
            _authoringToolsRoot.Add(_importPreviousStageBtn);

            _previewTransitionBtn = new Button(OnPreviewTransitionClicked)
            {
                text = "Preview Line Motion",
                style = { height = 22, marginBottom = 8 }
            };
            _authoringToolsRoot.Add(_previewTransitionBtn);

            _addActorField = new ObjectField("Actor")
            {
                objectType = typeof(CharacterDefinitionSO),
                allowSceneObjects = false,
                style = { marginBottom = 3 }
            };
            _addActorField.RegisterValueChangedCallback(_ => RefreshAuthoringControls());
            _authoringToolsRoot.Add(_addActorField);

            var actorRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 6
                }
            };

            _addActorBtn = new Button(OnAddActorClicked)
            {
                text = "Add Actor",
                style = { flexGrow = 1, marginRight = 4, height = 22 }
            };
            actorRow.Add(_addActorBtn);

            _removeSelectedActorBtn = new Button(OnRemoveSelectedActorClicked)
            {
                text = "Remove Selected",
                style = { flexGrow = 1, height = 22 }
            };
            actorRow.Add(_removeSelectedActorBtn);
            _authoringToolsRoot.Add(actorRow);

            _setBackgroundField = new ObjectField("Background")
            {
                objectType = typeof(BackgroundDefinitionSO),
                allowSceneObjects = false,
                style = { marginBottom = 3 }
            };
            _setBackgroundField.RegisterValueChangedCallback(_ => RefreshAuthoringControls());
            _authoringToolsRoot.Add(_setBackgroundField);

            var bgRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };

            _setBackgroundBtn = new Button(OnSetBackgroundClicked)
            {
                text = "Set Background",
                style = { flexGrow = 1, marginRight = 4, height = 22 }
            };
            bgRow.Add(_setBackgroundBtn);

            _clearBackgroundBtn = new Button(OnClearBackgroundClicked)
            {
                text = "Clear",
                style = { width = 58, height = 22 }
            };
            bgRow.Add(_clearBackgroundBtn);
            _authoringToolsRoot.Add(bgRow);

            return _authoringToolsRoot;
        }

        private VisualElement BuildInspectorSplitter()
        {
            var splitter = new VisualElement
            {
                name = "StoryPreviewInspectorSplitter",
                style =
                {
                    width = 5,
                    flexShrink = 0,
                    backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f))
                }
            };
            _inspectorSplitter = splitter;

            splitter.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || _inspectorPanel == null || _previewInspectorCollapsed)
                    return;

                _isInspectorResizing = true;
                _inspectorResizeStartX = e.position.x;
                _inspectorResizeStartWidth = _inspectorPanel.resolvedStyle.width > 0
                    ? _inspectorPanel.resolvedStyle.width
                    : InspectorWidth;
                splitter.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            splitter.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_isInspectorResizing || _inspectorPanel == null)
                    return;

                float delta = e.position.x - _inspectorResizeStartX;
                float width = Mathf.Clamp(_inspectorResizeStartWidth - delta, MinInspectorWidth, MaxInspectorWidth);
                _inspectorPanel.style.width = width;
                _previewInspectorExpandedWidth = width;
                e.StopPropagation();
            });

            splitter.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!_isInspectorResizing)
                    return;

                _isInspectorResizing = false;
                splitter.ReleasePointer(e.pointerId);
                SavePreviewLayoutPrefs();
                e.StopPropagation();
            });

            return splitter;
        }

        // ?? ?≫꽣 紐⑸줉 媛깆떊 ????????????????????????????

        private void RefreshActorList()
        {
            if (_actorListRoot == null) return;
            _actorListRoot.Clear();

            _actorListRoot.Add(MakeBoldLabel("Background"));
            _actorListRoot.Add(BuildBackgroundRow());
            _actorListRoot.Add(MakeSeparator());
            _actorListRoot.Add(MakeBoldLabel("Camera"));
            _actorListRoot.Add(BuildCameraRow());
            _actorListRoot.Add(MakeSeparator());

            foreach (var kvp in _stageState)
            {
                string actorKey = kvp.Key;
                var data  = kvp.Value;
                var actor = data.actor;
                if (actor == null) continue;
                if (!data.visible) continue;

                bool sel = _selectionKind == StageSelectionKind.Actor && actorKey == _selectedActorKey;

                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row, alignItems = Align.Center,
                        paddingLeft = 4, paddingRight = 4, paddingTop = 2, paddingBottom = 2, marginBottom = 1,
                        backgroundColor = new StyleColor(sel ? new Color(0.26f, 0.36f, 0.50f) : new Color(0.17f, 0.17f, 0.19f)),
                        borderTopLeftRadius = 2, borderTopRightRadius = 2,
                        borderBottomLeftRadius = 2, borderBottomRightRadius = 2
                    }
                };

                row.Add(new Label(data.visible ? "ON" : "OFF")
                {
                    style =
                    {
                        fontSize = 8, marginRight = 4,
                        color = new StyleColor(data.visible ? new Color(0.35f, 0.78f, 0.35f) : new Color(0.45f, 0.45f, 0.45f))
                    }
                });

                row.Add(new Label(actor.DisplayName)
                {
                    style = { fontSize = 10, flexGrow = 1, color = new StyleColor(Color.white) }
                });

                row.Add(new Label(actorKey)
                {
                    style =
                    {
                        fontSize = 8,
                        color = new StyleColor(new Color(0.52f, 0.52f, 0.56f)),
                        unityTextAlign = TextAnchor.MiddleRight,
                        maxWidth = 76,
                        overflow = Overflow.Hidden
                    }
                });

                if (data.focused)
                {
                    row.Add(new Label("F")
                    {
                        style = { fontSize = 8, color = new StyleColor(new Color(0.9f, 0.75f, 0.2f)) }
                    });
                }

                row.RegisterCallback<PointerDownEvent>(e =>
                {
                    SelectActor(actorKey);
                    e.StopPropagation();
                });

                _actorListRoot.Add(row);
            }
        }

        private VisualElement BuildBackgroundRow()
        {
            bool hasBackground = _bgState != null
                && (_bgState.background != null || !string.IsNullOrWhiteSpace(_bgState.ResolvedBackgroundKey));
            bool visible = _bgState is { visible: true };
            bool selected = _selectionKind == StageSelectionKind.Background;

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 2,
                    paddingBottom = 2,
                    marginBottom = 1,
                    backgroundColor = new StyleColor(selected ? new Color(0.26f, 0.36f, 0.50f) : new Color(0.17f, 0.17f, 0.19f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            row.Add(new Label(visible ? "BG" : "--")
            {
                style =
                {
                    fontSize = 8,
                    marginRight = 5,
                    color = new StyleColor(visible ? new Color(0.35f, 0.78f, 0.35f) : new Color(0.45f, 0.45f, 0.45f))
                }
            });

            string label = hasBackground
                ? _bgState.background != null
                    ? _bgState.background.DisplayName
                    : _bgState.ResolvedBackgroundKey
                : "(No Background)";
            row.Add(new Label(label)
            {
                style = { fontSize = 10, flexGrow = 1, color = new StyleColor(Color.white) }
            });

            row.RegisterCallback<PointerDownEvent>(e =>
            {
                SelectBackground();
                e.StopPropagation();
            });

            return row;
        }

        private VisualElement BuildCameraRow()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StoryCameraStateData state = layout?.CameraTrackEditable?.defaultState ?? new StoryCameraStateData();
            bool selected = _selectionKind == StageSelectionKind.Camera;

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 2,
                    paddingBottom = 2,
                    marginBottom = 1,
                    backgroundColor = new StyleColor(selected ? new Color(0.26f, 0.36f, 0.50f) : new Color(0.17f, 0.17f, 0.19f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            row.Add(new Label("CAM")
            {
                style =
                {
                    fontSize = 8,
                    marginRight = 5,
                    color = new StyleColor(new Color(1f, 0.76f, 0.32f))
                }
            });

            string target = !string.IsNullOrWhiteSpace(state.targetActorInstanceKey)
                ? state.targetActorInstanceKey
                : layout?.CameraFocusTarget ?? "";
            string label = string.IsNullOrWhiteSpace(target) ? "Camera" : $"Camera ({target})";
            row.Add(new Label(label)
            {
                style = { fontSize = 10, flexGrow = 1, color = new StyleColor(Color.white) }
            });

            row.RegisterCallback<PointerDownEvent>(e =>
            {
                SelectCamera();
                e.StopPropagation();
            });

            return row;
        }


        private void RefreshActorInspector()
        {
            if (_inspectorRoot == null) return;
            _inspectorRoot.Clear();

            if (!IsStageAuthoringMode)
            {
                _inspectorRoot.Add(new Label("Runtime Preview is read-only.")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.55f, 0.55f, 0.58f)), marginTop = 8 }
                });
                return;
            }

            if (_selectionKind == StageSelectionKind.Background)
            {
                BuildBackgroundInspector();
                return;
            }

            if (_selectionKind == StageSelectionKind.Camera)
            {
                BuildCameraInspector();
                return;
            }

            if (_selectionKind != StageSelectionKind.Actor
                || string.IsNullOrWhiteSpace(_selectedActorKey)
                || !_stageState.TryGetValue(_selectedActorKey, out var data))
            {
            _inspectorRoot.Add(new Label("No actor selected")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)), marginTop = 8 }
                });
                return;
            }

            CharacterDefinitionSO actor = data.actor;
            ValidateTimelineSelection();
            bool isEditingTimelineKey = _selectedTimelineKeyIndex >= 0;
            _inspectorRoot.Add(MakeBoldLabel("Actor"));
            _inspectorRoot.Add(MakeBoldLabel(actor != null ? actor.DisplayName : data.ResolvedActorKey));
            _inspectorRoot.Add(MakeSeparator());

            var keyField = new TextField("Instance Key")
            {
                value = data.ResolvedActorKey,
                isReadOnly = true,
                style = { marginBottom = 3 }
            };
            _inspectorRoot.Add(keyField);

            var characterField = new ObjectField("Character")
            {
                objectType = typeof(CharacterDefinitionSO),
                allowSceneObjects = false,
                value = actor,
                style = { marginBottom = 3 }
            };
            characterField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                var nextActor = e.newValue as CharacterDefinitionSO;
                data.actor = nextActor;
                data.actorKey = StoryActorStateData.ResolveActorKey(nextActor);
                RebuildActorLayer();
                SaveActorStateToCurrent(_selectedActorKey, entry =>
                {
                    entry.actor = nextActor;
                    entry.actorKey = StoryActorStateData.ResolveActorKey(nextActor);
                });
            });
            _inspectorRoot.Add(characterField);

            // ?? Position ????????????????????????????
            var posField = new Vector2Field("Position")
            {
                value = data.normalizedPosition,
                style =
                {
                    marginBottom = 3
                }
            };
            posField.SetEnabled(!isEditingTimelineKey);
            posField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.normalizedPosition = e.newValue;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.normalizedPosition = e.newValue);
                UpdateActorLayerPositions();
                RecordActorKeyframeFromState(_selectedActorKey, data, includePosition: true, includeScale: false);
            });
            _inspectorRoot.Add(posField);

            var actorScaleField = new Vector2Field("Scale")
            {
                value = data.scale,
                style =
                {
                    marginBottom = 3
                }
            };
            actorScaleField.SetEnabled(!isEditingTimelineKey);
            actorScaleField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.scale = ResolveNonZeroScale(e.newValue);
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.scale = data.scale);
                UpdateActorLayerPositions();
                RecordActorKeyframeFromState(_selectedActorKey, data, includePosition: false, includeScale: true);
            });
            _inspectorRoot.Add(actorScaleField);

            // ?? Scale X ?????????????????????????????
            var scaleField = new FloatField("Scale X")
            {
                value = data.scaleX,
                style =
                {
                    marginBottom = 3
                }
            };
            scaleField.SetEnabled(!isEditingTimelineKey);
            scaleField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.scaleX = e.newValue;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.scaleX = e.newValue);
                UpdateActorLayerPositions();
            });
            _inspectorRoot.Add(scaleField);

            // ?? Visible / Focused ????????????????????
            var visToggle = new Toggle("Visible")
            {
                value = data.visible,
                style =
                {
                    marginBottom = 3
                }
            };
            visToggle.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.visible = e.newValue;
                RebuildActorLayer();
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.visible = e.newValue);
            });
            _inspectorRoot.Add(visToggle);

            var focToggle = new Toggle("Focused")
            {
                value = data.focused,
                style =
                {
                    marginBottom = 3
                }
            };
            focToggle.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.focused = e.newValue;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.focused = e.newValue);
                RebuildActorLayer();
                RefreshActorInspector();
            });
            _inspectorRoot.Add(focToggle);

            // ?? Sort Order ???????????????????????????
            var sortField = new IntegerField("Sort Order")
            {
                value = data.sortOrder,
                style =
                {
                    marginBottom = 3
                }
            };
            sortField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.sortOrder = e.newValue;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.sortOrder = e.newValue);
                RebuildActorLayer();
            });
            _inspectorRoot.Add(sortField);

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Transition"));
            _inspectorRoot.Add(new Label("Line transition defaults are still stored on the snapshot, but timeline editing is now the main UI.")
            {
                style =
                {
                    fontSize = 9,
                    color = new StyleColor(new Color(0.58f, 0.60f, 0.66f)),
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 4
                }
            });

            if (HasTimelineMultiSelection)
            {
                BuildSelectedTimelineGroupInspector();
            }
            else
            {
                BuildSelectedTimelineKeyInspector();
                BuildSelectedTimelineSegmentInspector();
            }

            return;
        }

        private void BuildSelectedTimelineKeyInspector()
        {
            if (_selectedTimelineKeyIndex < 0)
                return;

            IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
            if (keyframes == null || _selectedTimelineKeyIndex >= keyframes.Count)
                return;

            StoryActorKeyframeData key = keyframes[_selectedTimelineKeyIndex];
            if (key == null)
                return;
            StoryActorKeyframeData selectedKeyRef = key;

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Selected Key"));
            _inspectorRoot.Add(new Label($"{key.property} @ {StoryTransitionSampler.GetKeyTime(key):0.00}s")
            {
                style = { marginBottom = 4, color = new StyleColor(new Color(0.70f, 0.72f, 0.78f)) }
            });

            var timeField = new FloatField("Time")
            {
                value = StoryTransitionSampler.GetKeyTime(key),
                style = { marginBottom = 3 }
            };
            timeField.RegisterValueChangedCallback(e =>
            {
                float time = Mathf.Max(0f, e.newValue);
                SaveCurrentTimelineKeyframes(currentKeys =>
                {
                    int index = currentKeys.IndexOf(selectedKeyRef);
                    if (index < 0)
                        return;

                    StoryActorKeyframeData currentKey = currentKeys[index];
                    currentKey.timeSeconds = time;
                    currentKey.normalizedTime = Mathf.Clamp01(time / Mathf.Max(1f, GetTimelineDuration()));
                }, refresh: false);
                _timelinePlayheadTime = Mathf.Max(0f, e.newValue);
                ApplyTimelinePlayheadSample();
                RefreshTimelinePanel();
            });
            _inspectorRoot.Add(timeField);

            if (key.property == StoryActorKeyframeProperty.Position)
            {
                var positionField = new Vector2Field("Position")
                {
                    value = key.normalizedPosition,
                    style = { marginBottom = 3 }
                };
                positionField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].normalizedPosition = e.newValue;
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(positionField);
            }
            else if (key.property == StoryActorKeyframeProperty.Scale)
            {
                var scaleField = new Vector2Field("Scale")
                {
                    value = key.scale,
                    style = { marginBottom = 3 }
                };
                scaleField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].scale = ResolveNonZeroScale(e.newValue);
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(scaleField);
            }
            else if (key.property == StoryActorKeyframeProperty.Expression)
            {
                var expressionField = new EnumField("Expression", key.expression)
                {
                    style = { marginBottom = 3 }
                };
                expressionField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].expression = (StoryExpressionType)e.newValue;
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(expressionField);
            }
            else if (key.property == StoryActorKeyframeProperty.BackgroundCut)
            {
                var backgroundField = new ObjectField("Background")
                {
                    objectType = typeof(BackgroundDefinitionSO),
                    allowSceneObjects = false,
                    value = key.background,
                    style = { marginBottom = 3 }
                };
                backgroundField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index < 0)
                            return;

                        var background = e.newValue as BackgroundDefinitionSO;
                        currentKeys[index].background = background;
                        currentKeys[index].backgroundKey = ResolveBackgroundKey(background);
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(backgroundField);
            }
            else if (key.property == StoryActorKeyframeProperty.BackgroundPosition)
            {
                var offsetField = new Vector2Field("Offset")
                {
                    value = key.normalizedPosition,
                    style = { marginBottom = 3 }
                };
                offsetField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].normalizedPosition = e.newValue;
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(offsetField);
            }
            else if (key.property == StoryActorKeyframeProperty.BackgroundScale)
            {
                var bgScaleField = new Vector2Field("Scale")
                {
                    value = key.scale,
                    style = { marginBottom = 3 }
                };
                bgScaleField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].scale = ResolveNonZeroScale(e.newValue);
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(bgScaleField);
            }
            else if (key.property == StoryActorKeyframeProperty.CameraTarget)
            {
                var targetField = new TextField("Target Actor")
                {
                    value = key.cameraTargetActorKey,
                    style = { marginBottom = 3 }
                };
                targetField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].cameraTargetActorKey = e.newValue ?? "";
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(targetField);

                var followModeField = new EnumField("Follow Mode", key.cameraFollowMode)
                {
                    style = { marginBottom = 3 }
                };
                followModeField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].cameraFollowMode = (StoryCameraFollowMode)e.newValue;
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(followModeField);

                var moveModeField = new EnumField("Move Mode", key.cameraMoveMode)
                {
                    style = { marginBottom = 3 }
                };
                moveModeField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].cameraMoveMode = (StoryCameraMoveMode)e.newValue;
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(moveModeField);

                var actorKeysRow = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 4 }
                };
                foreach (var kvp in _stageState)
                {
                    if (kvp.Value == null || !kvp.Value.visible)
                        continue;

                    string captured = kvp.Key;
                    actorKeysRow.Add(MakeBtn(captured, new Color(0.22f, 0.28f, 0.22f), () =>
                    {
                        SaveCurrentTimelineKeyframes(currentKeys =>
                        {
                            int index = currentKeys.IndexOf(selectedKeyRef);
                            if (index >= 0)
                            {
                                currentKeys[index].cameraTargetActorKey = captured;
                                currentKeys[index].cameraSnapshotNormalizedPosition = kvp.Value.normalizedPosition + kvp.Value.EffectiveOffset;
                            }
                        }, refresh: false);
                        ApplyTimelinePlayheadSample();
                        RefreshActorInspector();
                        RefreshTimelinePanel();
                    }));
                }
                _inspectorRoot.Add(actorKeysRow);
            }
            else if (key.property == StoryActorKeyframeProperty.CameraOffset)
            {
                var offsetField = new Vector2Field("Camera Offset")
                {
                    value = key.cameraOffset,
                    style = { marginBottom = 3 }
                };
                offsetField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].cameraOffset = e.newValue;
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(offsetField);
            }
            else if (key.property == StoryActorKeyframeProperty.CameraZoom)
            {
                var zoomField = new FloatField("Camera Zoom")
                {
                    value = key.cameraZoom,
                    style = { marginBottom = 3 }
                };
                zoomField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].cameraZoom = Mathf.Max(0.01f, e.newValue);
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(zoomField);
            }
            else if (key.property == StoryActorKeyframeProperty.CameraShake)
            {
                _inspectorRoot.Add(new HelpBox("Camera Shake keys are no longer supported.", HelpBoxMessageType.Info));
            }

        }

        private void BuildSelectedTimelineGroupInspector()
        {
            List<StoryActorKeyframeData> selected = GetSelectedTimelineKeys(GetCurrentTimelineKeyframes());
            if (selected.Count <= 1)
                return;

            float minTime = float.MaxValue;
            float maxTime = 0f;
            var properties = new SortedSet<string>();
            foreach (StoryActorKeyframeData key in selected)
            {
                if (key == null)
                    continue;

                float time = StoryTransitionSampler.GetKeyTime(key);
                minTime = Mathf.Min(minTime, time);
                maxTime = Mathf.Max(maxTime, time);
                properties.Add(key.property.ToString());
            }

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Selected Keys"));
            _inspectorRoot.Add(new Label($"{selected.Count} keys / {string.Join(", ", properties)}")
            {
                style = { marginBottom = 3, color = new StyleColor(new Color(0.70f, 0.72f, 0.78f)) }
            });
            _inspectorRoot.Add(new Label($"Range: {minTime:0.00}s -> {maxTime:0.00}s")
            {
                style = { marginBottom = 5, color = new StyleColor(new Color(0.70f, 0.72f, 0.78f)) }
            });

            _inspectorRoot.Add(new Label("Multiple keys are selected. Direct value editing is disabled; use group move, Delete, Copy/Paste, or save as preset.")
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 5,
                    color = new StyleColor(new Color(0.58f, 0.60f, 0.66f))
                }
            });

            if (_selectionKind == StageSelectionKind.Actor)
            {
                _inspectorRoot.Add(MakeBtn("Save Selection as Preset", new Color(0.22f, 0.30f, 0.31f), () =>
                {
                    if (SaveSelectedTimelineKeysAsMotionPreset())
                        StoryMotionPresetLibraryWindow.Open();
                }));
            }
            _inspectorRoot.Add(MakeBtn("Delete Selected Keys", new Color(0.34f, 0.20f, 0.20f), RemoveSelectedTimelineKey));
        }

        private void BuildSelectedTimelineSegmentInspector()
        {
            if (_selectedTimelineSegmentKeyIndex < 0)
                return;

            IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
            if (keyframes == null
                || _selectedTimelineSegmentKeyIndex >= keyframes.Count
                || keyframes[_selectedTimelineSegmentKeyIndex] == null)
                return;

            int fromIndex = _selectedTimelineSegmentKeyIndex;
            int toIndex = FindNextKeyIndex(keyframes, fromIndex, _selectedTimelineSegmentProperty);
            if (toIndex < 0)
                return;

            StoryActorKeyframeData from = keyframes[fromIndex];
            StoryActorKeyframeData to = keyframes[toIndex];
            StoryActorKeyframeData segmentKeyRef = from;

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Selected Segment"));
            _inspectorRoot.Add(new Label($"{from.property}: {StoryTransitionSampler.GetKeyTime(from):0.00}s -> {StoryTransitionSampler.GetKeyTime(to):0.00}s")
            {
                style = { marginBottom = 4, color = new StyleColor(new Color(0.70f, 0.72f, 0.78f)) }
            });

            var easingField = new EnumField("Easing", from.easing)
            {
                style = { marginBottom = 3 }
            };
            easingField.RegisterValueChangedCallback(e =>
            {
                SaveCurrentTimelineKeyframes(currentKeys =>
                {
                    int index = currentKeys.IndexOf(segmentKeyRef);
                    if (index >= 0)
                        currentKeys[index].easing = (StoryStageMoveMotionType)e.newValue;
                }, refresh: false);
                ApplyTimelinePlayheadSample();
                RefreshTimelinePanel();
            });
            _inspectorRoot.Add(easingField);
        }

        //
        private void BuildBackgroundInspector()
        {
            StoryBackgroundStateData data = _bgState?.ShallowClone() ?? new StoryBackgroundStateData();
            ValidateTimelineSelection();
            bool isEditingTimelineKey = _selectedTimelineKeyIndex >= 0;

            _inspectorRoot.Add(MakeBoldLabel("Stage Background"));
            _inspectorRoot.Add(MakeSeparator());

            var definitionField = new ObjectField("Definition")
            {
                objectType = typeof(BackgroundDefinitionSO),
                allowSceneObjects = false,
                value = data.background,
                style = { marginBottom = 3 }
            };
            definitionField.SetEnabled(!isEditingTimelineKey);
            definitionField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                var next = e.newValue as BackgroundDefinitionSO;
                data.background = next;
                data.backgroundKey = ResolveBackgroundKey(next);
                data.visible = next != null || !string.IsNullOrWhiteSpace(data.backgroundKey);
                SaveBackgroundStateToCurrent(entry =>
                {
                    entry.background = next;
                    entry.backgroundKey = ResolveBackgroundKey(next);
                    entry.visible = data.visible;
                });
            });
            _inspectorRoot.Add(definitionField);

            var keyField = new TextField("Background Key")
            {
                value = data.ResolvedBackgroundKey,
                style = { marginBottom = 3 }
            };
            keyField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.backgroundKey = e.newValue ?? "";
                SaveBackgroundStateToCurrent(entry => entry.backgroundKey = data.backgroundKey);
            });
            _inspectorRoot.Add(keyField);

            var visibleToggle = new Toggle("Visible")
            {
                value = data.visible,
                style = { marginBottom = 3 }
            };
            visibleToggle.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.visible = e.newValue;
                SaveBackgroundStateToCurrent(entry => entry.visible = e.newValue);
            });
            _inspectorRoot.Add(visibleToggle);

            var offsetField = new Vector2Field("Offset")
            {
                value = data.normalizedOffset,
                style = { marginBottom = 3 }
            };
            offsetField.SetEnabled(!isEditingTimelineKey);
            offsetField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.normalizedOffset = e.newValue;
                SaveBackgroundStateToCurrent(entry => entry.normalizedOffset = e.newValue);
                RecordBackgroundKeyframeFromState(data, includePosition: true, includeScale: false);
            });
            _inspectorRoot.Add(offsetField);

            var scaleField = new Vector2Field("Scale")
            {
                value = data.scale,
                style = { marginBottom = 3 }
            };
            scaleField.SetEnabled(!isEditingTimelineKey);
            scaleField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.scale = ResolveNonZeroScale(e.newValue);
                SaveBackgroundStateToCurrent(entry => entry.scale = data.scale);
                RecordBackgroundKeyframeFromState(data, includePosition: false, includeScale: true);
            });
            _inspectorRoot.Add(scaleField);

            var sortField = new IntegerField("Sort Order")
            {
                value = data.sortOrder,
                style = { marginBottom = 3 }
            };
            sortField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.sortOrder = e.newValue;
                SaveBackgroundStateToCurrent(entry => entry.sortOrder = e.newValue);
            });
            _inspectorRoot.Add(sortField);

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(new Label("Legacy background transition values are kept for runtime fallback. Timeline editing will replace this UI.")
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    fontSize = 10,
                    color = new StyleColor(new Color(0.55f, 0.56f, 0.60f)),
                    marginBottom = 4
                }
            });

            if (HasTimelineMultiSelection)
            {
                BuildSelectedTimelineGroupInspector();
            }
            else
            {
                BuildSelectedTimelineKeyInspector();
                BuildSelectedTimelineSegmentInspector();
            }
        }

        private void BuildCameraInspector()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            if (layout == null)
                return;

            StoryCameraTrackData track = layout.CameraTrackEditable;
            track.defaultState ??= new StoryCameraStateData();
            StoryCameraStateData state = track.defaultState;

            _inspectorRoot.Add(MakeBoldLabel("Camera"));
            _inspectorRoot.Add(MakeSeparator());

            string currentFocus = !string.IsNullOrWhiteSpace(state.targetActorInstanceKey)
                ? state.targetActorInstanceKey
                : layout.CameraFocusTargetEditable;
            var focusField = new TextField("Target")
            {
                value = currentFocus,
                style = { marginBottom = 3 }
            };
            focusField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                SaveCameraStateToCurrent(camera =>
                {
                    camera.targetActorInstanceKey = e.newValue ?? "";
                    layout.CameraFocusTargetEditable = camera.targetActorInstanceKey;
                });
                RebuildActorLayer(refreshInspectorLists: false);
                RefreshBackgroundLayer();
            });
            _inspectorRoot.Add(focusField);

            var followModeField = new EnumField("Follow Mode", state.followMode)
            {
                style = { marginBottom = 3 }
            };
            followModeField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                SaveCameraStateToCurrent(camera => camera.followMode = (StoryCameraFollowMode)e.newValue);
                RefreshTimelinePanel();
            });
            _inspectorRoot.Add(followModeField);

            var moveModeField = new EnumField("Move Mode", state.moveMode)
            {
                style = { marginBottom = 3 }
            };
            moveModeField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                SaveCameraStateToCurrent(camera => camera.moveMode = (StoryCameraMoveMode)e.newValue);
                RefreshTimelinePanel();
            });
            _inspectorRoot.Add(moveModeField);

            var offsetField = new Vector2Field("Offset")
            {
                value = state.normalizedOffset,
                style = { marginBottom = 3 }
            };
            offsetField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                SaveCameraStateToCurrent(camera => camera.normalizedOffset = e.newValue);
                RebuildActorLayer(refreshInspectorLists: false);
                RefreshBackgroundLayer();
            });
            _inspectorRoot.Add(offsetField);

            var zoomField = new FloatField("Zoom")
            {
                value = state.zoomMultiplier,
                style = { marginBottom = 3 }
            };
            zoomField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                SaveCameraStateToCurrent(camera => camera.zoomMultiplier = Mathf.Max(0.01f, e.newValue));
                RebuildActorLayer(refreshInspectorLists: false);
                RefreshBackgroundLayer();
            });
            _inspectorRoot.Add(zoomField);

            var actorKeys = new System.Collections.Generic.List<string>();
            foreach (var kvp in _stageState)
            {
                if (kvp.Value != null && kvp.Value.visible)
                    actorKeys.Add(kvp.Key);
            }

            if (actorKeys.Count > 0)
            {
                var pickLabel = new Label("Quick Pick:")
                {
                    style = { fontSize = 9, color = new StyleColor(new Color(0.62f, 0.64f, 0.68f)), marginBottom = 2 }
                };
                _inspectorRoot.Add(pickLabel);

                var pickRow = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 3 }
                };
                foreach (string key in actorKeys)
                {
                    string captured = key;
                    bool isCurrent = string.Equals(captured, currentFocus, System.StringComparison.Ordinal);
                    var btn = MakeBtn(captured, isCurrent ? new Color(0.22f, 0.40f, 0.28f) : new Color(0.22f, 0.28f, 0.22f), () =>
                    {
                        focusField.SetValueWithoutNotify(captured);
                        SaveCameraStateToCurrent(camera =>
                        {
                            camera.targetActorInstanceKey = captured;
                            layout.CameraFocusTargetEditable = captured;
                        });
                        RebuildActorLayer(refreshInspectorLists: false);
                        RefreshBackgroundLayer();
                        RefreshActorInspector();
                    });
                    btn.style.marginBottom = 2;
                    pickRow.Add(btn);
                }
                _inspectorRoot.Add(pickRow);
            }

            if (!string.IsNullOrWhiteSpace(currentFocus))
            {
                var clearBtn = MakeBtn("Clear Focus", new Color(0.30f, 0.20f, 0.20f), () =>
                {
                    focusField.SetValueWithoutNotify("");
                    SaveCameraStateToCurrent(camera => camera.targetActorInstanceKey = "");
                    layout.CameraFocusTargetEditable = "";
                    RebuildActorLayer(refreshInspectorLists: false);
                    RefreshBackgroundLayer();
                    RefreshActorInspector();
                });
                _inspectorRoot.Add(clearBtn);
            }

            if (HasTimelineMultiSelection)
            {
                BuildSelectedTimelineGroupInspector();
            }
            else
            {
                BuildSelectedTimelineKeyInspector();
                BuildSelectedTimelineSegmentInspector();
            }
        }

        private void OnAddActorClicked()
        {
            if (!IsStageAuthoringMode)
                return;

            var actor = _addActorField?.value as CharacterDefinitionSO;
            if (_currentLine == null || actor == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Add Actor To Stage");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Add Actor To Stage");

            string instanceKey = GenerateActorInstanceKey(actor, layout);
            StoryActorStateData entry = new()
            {
                actor = actor,
                actorKey = StoryActorStateData.ResolveActorKey(actor),
                actorInstanceKey = instanceKey,
                normalizedPosition = new Vector2(0.5f, 0f),
                visible = true,
                focused = true,
                sortOrder = layout.ActorsEditable.Count
            };
            layout.ActorsEditable.Add(entry);

            SelectActor(instanceKey, refreshInspector: false);
            SaveLayoutAndRefresh(layout);
        }

        private void OnImportPreviousStageClicked()
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            if (!TryGetPreviousLine(_currentLine, out StoryLineSO previousLine) || previousLine == null)
                return;

            if (!TryBuildFinalStageStateAtLine(previousLine, out var previousActors, out var previousBackground, out var previousCamera))
                return;

            if (previousActors.Count == 0 && previousBackground == null && IsDefaultCameraState(previousCamera))
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Import Previous Stage");
            if (layout == null)
                return;

            bool hasExistingData = layout.ActorsEditable.Count > 0
                || layout.BackgroundTrackEditable.keyframes.Count > 0
                || layout.ActorTracksEditable.Count > 0
                || layout.CameraTrackEditable.keyframes.Count > 0;

            if (hasExistingData)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Import Previous Stage",
                    "Overwrite the current line snapshot with the previous line's final static result and clear current actor/background/camera tracks?",
                    "Overwrite",
                    "Cancel");
                if (!overwrite)
                    return;
            }

            Undo.RecordObject(layout, "Import Previous Stage");
            layout.ActorsEditable.Clear();
            layout.ActorTracksEditable.Clear();
            layout.BackgroundTrackEditable.keyframes.Clear();
            layout.CameraTrackEditable.keyframes.Clear();

            foreach (var pair in previousActors)
            {
                StoryActorStateData clone = pair.Value.ShallowClone();
                clone.EnsureActorInstanceKey(pair.Key);
                clone.SyncActorKey();
                layout.ActorsEditable.Add(clone);
            }

            CopyBackgroundState(previousBackground, layout.BackgroundEditable);
            CopyCameraState(previousCamera, layout.CameraTrackEditable.defaultState);
            layout.CameraFocusTargetEditable = previousCamera?.targetActorInstanceKey ?? "";
            SaveLayoutAndRefresh(layout);
        }

        private void OnPreviewTransitionClicked()
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            StartLineTransitionPreview(_currentLine);
        }

        private void OnRemoveSelectedActorClicked()
        {
            if (!IsStageAuthoringMode)
                return;

            if (_currentLine == null || _selectionKind != StageSelectionKind.Actor || string.IsNullOrWhiteSpace(_selectedActorKey))
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Remove Actor From Stage");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Remove Actor From Stage");

            StoryActorStateData entry = FindActorEntry(layout, _selectedActorKey);
            if (entry == null)
            {
                entry = _stageState.TryGetValue(_selectedActorKey, out var current)
                    ? current.ShallowClone()
                    : new StoryActorStateData();
                entry.EnsureActorInstanceKey(_selectedActorKey);
                layout.ActorsEditable.Add(entry);
            }

            entry.visible = false;
            SaveLayoutAndRefresh(layout);
        }

        private void OnSetBackgroundClicked()
        {
            if (!IsStageAuthoringMode)
                return;

            var background = _setBackgroundField?.value as BackgroundDefinitionSO;
            if (_currentLine == null || background == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Set Stage Background");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Set Stage Background");

            StoryBackgroundStateData state = layout.BackgroundEditable;
            state.background = background;
            state.backgroundKey = ResolveBackgroundKey(background);
            state.visible = true;
            if (Mathf.Approximately(state.scale.x, 0f) && Mathf.Approximately(state.scale.y, 0f))
                state.scale = Vector2.one;

            _selectionKind = StageSelectionKind.Background;
            _selectedActorKey = null;
            SaveLayoutAndRefresh(layout);
        }

        private void OnClearBackgroundClicked()
        {
            if (!IsStageAuthoringMode)
                return;

            if (_currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Clear Stage Background");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Clear Stage Background");

            StoryBackgroundStateData state = layout.BackgroundEditable;
            state.background = null;
            state.backgroundKey = "";
            state.visible = false;

            _selectionKind = StageSelectionKind.Background;
            _selectedActorKey = null;
            SaveLayoutAndRefresh(layout);
        }

        private void RefreshAuthoringControls()
        {
            bool isAuthoring = IsStageAuthoringMode;
            bool hasLine = _currentLine != null;
            bool hasCurrentStageLayout = FindCurrentStageLayout() != null;
            bool hasPreviousStage = hasLine && TryBuildStageStateBeforeLine(
                _currentLine,
                out var previousActors,
                out var previousBackground)
                && (previousActors.Count > 0 || previousBackground != null);

            SetElementVisible(_authoringToolsRoot, isAuthoring);
            _importPreviousStageBtn?.SetEnabled(isAuthoring && hasLine && hasPreviousStage);
            _previewTransitionBtn?.SetEnabled(isAuthoring && hasLine && (hasCurrentStageLayout || _stageState.Count > 0 || hasPreviousStage));
            _addActorBtn?.SetEnabled(isAuthoring && hasLine && _addActorField?.value is CharacterDefinitionSO);
            _removeSelectedActorBtn?.SetEnabled(isAuthoring && hasLine && _selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey));
            _setBackgroundBtn?.SetEnabled(isAuthoring && hasLine && _setBackgroundField?.value is BackgroundDefinitionSO);
            _clearBackgroundBtn?.SetEnabled(isAuthoring && hasLine);
        }

        private StoryStageLayoutModuleSO FindCurrentStageLayout()
        {
            if (_currentLine == null)
                return null;

            foreach (var module in _currentLine.Modules)
            {
                if (module is StoryStageLayoutModuleSO layout)
                    return layout;
            }

            return null;
        }

        private StoryStageLayoutModuleSO GetOrCreateCurrentStageLayout(string undoName)
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            if (layout != null)
                return layout;

            if (_currentLine == null)
                return null;

            Undo.RecordObject(_currentLine, undoName);
            return StoryEditorUtility.AddModule(_currentLine, typeof(StoryStageLayoutModuleSO)) as StoryStageLayoutModuleSO;
        }

        private string GenerateActorInstanceKey(CharacterDefinitionSO actor, StoryStageLayoutModuleSO layout)
        {
            string baseKey = StoryActorStateData.ResolveActorKey(actor);
            if (string.IsNullOrWhiteSpace(baseKey))
                baseKey = "actor";

            var used = new HashSet<string>(_stageState.Keys);
            if (layout != null)
            {
                foreach (StoryActorStateData entry in layout.ActorsEditable)
                {
                    if (entry == null) continue;
                    string key = entry.ResolvedActorKey;
                    if (!string.IsNullOrWhiteSpace(key))
                        used.Add(key);
                }
            }

            for (int i = 1; i < 1000; i++)
            {
                string candidate = $"{baseKey}_{i:D2}";
                if (!used.Contains(candidate))
                    return candidate;
            }

            return $"{baseKey}_{Guid.NewGuid():N}";
        }

        private static StoryActorStateData FindActorEntry(StoryStageLayoutModuleSO layout, string actorInstanceKey)
        {
            if (layout == null || string.IsNullOrWhiteSpace(actorInstanceKey))
                return null;

            foreach (StoryActorStateData entry in layout.ActorsEditable)
            {
                if (entry?.MatchesActorInstance(actorInstanceKey) == true)
                    return entry;
            }

            return null;
        }

        private static StoryActorTrackData FindActorTrack(StoryStageLayoutModuleSO layout, string actorInstanceKey)
        {
            if (layout == null || string.IsNullOrWhiteSpace(actorInstanceKey))
                return null;

            foreach (StoryActorTrackData track in layout.ActorTracksEditable)
            {
                if (track != null && track.actorInstanceKey == actorInstanceKey)
                    return track;
            }

            return null;
        }

        private static StoryActorTrackData GetOrCreateActorTrack(StoryStageLayoutModuleSO layout, string actorInstanceKey)
        {
            if (layout == null || string.IsNullOrWhiteSpace(actorInstanceKey))
                return null;

            StoryActorTrackData track = FindActorTrack(layout, actorInstanceKey);
            if (track != null)
                return track;

            track = new StoryActorTrackData { actorInstanceKey = actorInstanceKey };
            layout.ActorTracksEditable.Add(track);
            return track;
        }

        private void SaveActorTrackToCurrent(string actorInstanceKey, Action<StoryActorTrackData> setter, bool refresh = false)
        {
            if (!IsStageAuthoringMode || _currentLine == null || string.IsNullOrWhiteSpace(actorInstanceKey))
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Actor Timeline");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Edit Actor Timeline");
            StoryActorTrackData track = GetOrCreateActorTrack(layout, actorInstanceKey);
            if (track == null)
                return;
            track.keyframes ??= new List<StoryActorKeyframeData>();

            StoryActorKeyframeData selectedKey = _selectedTimelineKeyIndex >= 0 && _selectedTimelineKeyIndex < track.keyframes.Count
                ? track.keyframes[_selectedTimelineKeyIndex]
                : null;
            StoryActorKeyframeData selectedSegmentKey = _selectedTimelineSegmentKeyIndex >= 0 && _selectedTimelineSegmentKeyIndex < track.keyframes.Count
                ? track.keyframes[_selectedTimelineSegmentKeyIndex]
                : null;

            setter(track);
            track.keyframes.RemoveAll(k => k == null);
            track.keyframes.Sort((a, b) =>
            {
                int prop = a.property.CompareTo(b.property);
                return prop != 0
                    ? prop
                    : StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b));
            });

            if (selectedKey != null)
                _selectedTimelineKeyIndex = track.keyframes.IndexOf(selectedKey);
            if (selectedSegmentKey != null)
                _selectedTimelineSegmentKeyIndex = track.keyframes.IndexOf(selectedSegmentKey);
            ValidateTimelineSelection();

            MarkLayoutDirty(layout, saveNow: false);

            if (refresh)
            {
                RefreshActorInspector();
                RefreshTimelinePanel();
            }
        }

        private void SaveBackgroundTrackToCurrent(Action<StoryBackgroundTrackData> setter, bool refresh = false)
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Background Timeline");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Edit Background Timeline");
            StoryBackgroundTrackData track = layout.BackgroundTrackEditable;
            if (track == null)
                return;
            track.keyframes ??= new List<StoryActorKeyframeData>();

            StoryActorKeyframeData selectedKey = _selectedTimelineKeyIndex >= 0 && _selectedTimelineKeyIndex < track.keyframes.Count
                ? track.keyframes[_selectedTimelineKeyIndex]
                : null;
            StoryActorKeyframeData selectedSegmentKey = _selectedTimelineSegmentKeyIndex >= 0 && _selectedTimelineSegmentKeyIndex < track.keyframes.Count
                ? track.keyframes[_selectedTimelineSegmentKeyIndex]
                : null;

            setter(track);
            track.keyframes.RemoveAll(k => k == null);
            track.keyframes.Sort((a, b) =>
            {
                int prop = a.property.CompareTo(b.property);
                return prop != 0
                    ? prop
                    : StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b));
            });

            if (selectedKey != null)
                _selectedTimelineKeyIndex = track.keyframes.IndexOf(selectedKey);
            if (selectedSegmentKey != null)
                _selectedTimelineSegmentKeyIndex = track.keyframes.IndexOf(selectedSegmentKey);
            ValidateTimelineSelection();

            MarkLayoutDirty(layout, saveNow: false);

            if (refresh)
            {
                RefreshActorInspector();
                RefreshTimelinePanel();
            }
        }

        private void SaveCameraTrackToCurrent(Action<StoryCameraTrackData> setter, bool refresh = false)
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Camera Timeline");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Edit Camera Timeline");
            StoryCameraTrackData track = layout.CameraTrackEditable;
            if (track == null)
                return;

            track.defaultState ??= new StoryCameraStateData();
            track.keyframes ??= new List<StoryActorKeyframeData>();

            StoryActorKeyframeData selectedKey = _selectedTimelineKeyIndex >= 0 && _selectedTimelineKeyIndex < track.keyframes.Count
                ? track.keyframes[_selectedTimelineKeyIndex]
                : null;
            StoryActorKeyframeData selectedSegmentKey = _selectedTimelineSegmentKeyIndex >= 0 && _selectedTimelineSegmentKeyIndex < track.keyframes.Count
                ? track.keyframes[_selectedTimelineSegmentKeyIndex]
                : null;

            setter(track);
            track.keyframes.RemoveAll(k => k == null);
            track.keyframes.Sort((a, b) =>
            {
                int prop = a.property.CompareTo(b.property);
                return prop != 0
                    ? prop
                    : StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b));
            });

            if (selectedKey != null)
                _selectedTimelineKeyIndex = track.keyframes.IndexOf(selectedKey);
            if (selectedSegmentKey != null)
                _selectedTimelineSegmentKeyIndex = track.keyframes.IndexOf(selectedSegmentKey);
            ValidateTimelineSelection();

            MarkLayoutDirty(layout, saveNow: false);

            if (refresh)
            {
                RefreshActorInspector();
                RefreshTimelinePanel();
            }
        }

        private void SaveCameraStateToCurrent(Action<StoryCameraStateData> setter)
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Camera State");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Edit Camera State");
            StoryCameraTrackData track = layout.CameraTrackEditable;
            track.defaultState ??= new StoryCameraStateData();
            setter(track.defaultState);
            MarkLayoutDirty(layout, saveNow: false);
            RefreshActorList();
            RefreshActorInspector();
            RefreshTimelinePanel();
        }

        private void SaveLayoutAndRefresh(StoryStageLayoutModuleSO layout, bool saveNow = true)
        {
            MarkLayoutDirty(layout, saveNow);

            BuildStageStateAt(_currentLine);
            if (_selectionKind == StageSelectionKind.Actor && !_stageState.ContainsKey(_selectedActorKey))
                ClearStageSelection();
            RebuildActorLayer();
            RefreshActorInspector();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
        }

        private static string ResolveBackgroundKey(BackgroundDefinitionSO background)
        {
            if (background == null)
                return "";

            return !string.IsNullOrWhiteSpace(background.BackgroundId)
                ? background.BackgroundId
                : background.name;
        }

        private static void CopyBackgroundState(StoryBackgroundStateData source, StoryBackgroundStateData target)
        {
            if (target == null)
                return;

            if (source == null)
            {
                target.background = null;
                target.backgroundKey = "";
                target.variantKey = "";
                target.visible = false;
                target.normalizedOffset = Vector2.zero;
                target.scale = Vector2.one;
                target.pivot = new Vector2(0.5f, 0.5f);
                target.tint = Color.white;
                target.opacity = 1f;
                target.sortOrder = -100;
                target.transitionMotion = StoryEnterMotionType.FadeIn;
                target.transitionDuration = 0.35f;
                target.enterMotion = StoryEnterMotionType.FadeIn;
                target.enterDuration = 0.35f;
                target.exitMotion = StoryEnterMotionType.FadeIn;
                target.exitDuration = 0.25f;
                return;
            }

            target.background = source.background;
            target.backgroundKey = source.backgroundKey;
            target.variantKey = source.variantKey;
            target.visible = source.visible;
            target.normalizedOffset = source.normalizedOffset;
            target.scale = source.scale;
            target.pivot = source.pivot;
            target.tint = source.tint;
            target.opacity = source.opacity;
            target.sortOrder = source.sortOrder;
            target.transitionMotion = source.transitionMotion;
            target.transitionDuration = source.transitionDuration;
            target.enterMotion = source.enterMotion;
            target.enterDuration = source.enterDuration;
            target.exitMotion = source.exitMotion;
            target.exitDuration = source.exitDuration;
            target.SyncBackgroundKey();
        }

        private bool TryBuildFinalStageStateAtLine(
            StoryLineSO line,
            out Dictionary<string, StoryActorStateData> actors,
            out StoryBackgroundStateData background,
            out StoryCameraStateData camera)
        {
            actors = new Dictionary<string, StoryActorStateData>();
            background = null;
            camera = new StoryCameraStateData();

            if (line == null)
                return false;

            if (!TryBuildStageStateByEpisodeOrder(line, includeTargetLine: true, actors, ref background))
                return false;

            StoryStageLayoutModuleSO layout = FindStageLayout(line);
            if (layout == null)
                return actors.Count > 0 || background != null;

            foreach (StoryActorTrackData track in layout.ActorTracks)
            {
                if (track == null || string.IsNullOrWhiteSpace(track.actorInstanceKey))
                    continue;

                if (!actors.TryGetValue(track.actorInstanceKey, out StoryActorStateData actorState) || actorState == null)
                    continue;

                float duration = StoryTransitionSampler.GetActorTrackDuration(track);
                actors[track.actorInstanceKey] = StoryTransitionSampler.SampleActorTrackAtTime(actorState, track, duration);
            }

            background = StoryTransitionSampler.SampleBackgroundTrackAtTime(
                background,
                layout.BackgroundTrack,
                StoryTransitionSampler.GetBackgroundTrackDuration(layout.BackgroundTrack));

            camera = StoryTransitionSampler.SampleCameraTrackAtTime(
                layout.CameraTrack,
                layout.CameraFocusTarget,
                StoryTransitionSampler.GetCameraTrackDuration(layout.CameraTrack));
            return actors.Count > 0 || background != null || !IsDefaultCameraState(camera);
        }

        private static StoryStageLayoutModuleSO FindStageLayout(StoryLineSO line)
        {
            if (line == null)
                return null;

            foreach (StoryModuleSO module in line.Modules)
            {
                if (module is StoryStageLayoutModuleSO layout)
                    return layout;
            }

            return null;
        }

        private static void CopyCameraState(StoryCameraStateData source, StoryCameraStateData target)
        {
            if (target == null)
                return;

            if (source == null)
            {
                target.targetActorInstanceKey = "";
                target.followMode = StoryCameraFollowMode.FollowActor;
                target.moveMode = StoryCameraMoveMode.Smooth;
                target.normalizedOffset = Vector2.zero;
                target.zoomMultiplier = 1f;
                target.snapshotNormalizedPosition = new Vector2(0.5f, 0.5f);
                return;
            }

            target.targetActorInstanceKey = source.targetActorInstanceKey ?? "";
            target.followMode = source.followMode;
            target.moveMode = source.moveMode;
            target.normalizedOffset = source.normalizedOffset;
            target.zoomMultiplier = source.zoomMultiplier;
            target.snapshotNormalizedPosition = source.snapshotNormalizedPosition;
        }

        private static bool IsDefaultCameraState(StoryCameraStateData state)
        {
            if (state == null)
                return true;

            return string.IsNullOrWhiteSpace(state.targetActorInstanceKey)
                && state.followMode == StoryCameraFollowMode.FollowActor
                && state.moveMode == StoryCameraMoveMode.Smooth
                && state.normalizedOffset == Vector2.zero
                && Mathf.Approximately(state.zoomMultiplier, 1f)
                && state.snapshotNormalizedPosition == new Vector2(0.5f, 0.5f);
        }

        private void SaveBackgroundStateToCurrent(Action<StoryBackgroundStateData> setter)
        {
            if (!IsStageAuthoringMode)
                return;

            if (_currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Stage Background");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Edit Stage Background");
            setter(layout.BackgroundEditable);
            layout.BackgroundEditable.SyncBackgroundKey();
            _bgState = layout.Background.ShallowClone();
            MarkLayoutDirty(layout, saveNow: false);
            RefreshBackgroundLayer();
            RefreshActorList();
            RefreshAuthoringControls();
        }

        private void SelectActor(string actorInstanceKey, bool refreshInspector = true)
        {
            if (!IsStageAuthoringMode)
                return;

                if (IsActorRecordSelectionLocked(actorInstanceKey))
                    return;

            bool sameActor = _selectionKind == StageSelectionKind.Actor && _selectedActorKey == actorInstanceKey;
            _selectionKind = StageSelectionKind.Actor;
            _selectedActorKey = actorInstanceKey;
            if (!sameActor)
                ClearTimelineSelection(refresh: false);
            RefreshActorList();
            HighlightSelectedActor();
            if (refreshInspector)
                RefreshActorInspector();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
        }

        private void SelectBackground()
        {
            if (!IsStageAuthoringMode)
                return;

            if (IsBackgroundRecordSelectionLocked())
                return;

            _selectionKind = StageSelectionKind.Background;
            _selectedActorKey = null;
            ClearTimelineSelection(refresh: false);
            RefreshActorList();
            HighlightSelectedActor();
            RefreshActorInspector();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
        }

        private void SelectCamera()
        {
            if (!IsStageAuthoringMode)
                return;

            if (_timelineRecordEnabled)
                return;

            _selectionKind = StageSelectionKind.Camera;
            _selectedActorKey = null;
            ClearTimelineSelection(refresh: false);
            RefreshActorList();
            HighlightSelectedActor();
            RefreshActorInspector();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
        }

        private void ClearStageSelection()
        {
            _selectionKind = StageSelectionKind.None;
            _selectedActorKey = null;
            ClearTimelineSelection(refresh: false);
            RefreshTimelinePanel();
        }

        private void ValidateStageSelection()
        {
            if (_selectionKind == StageSelectionKind.Actor && !_stageState.ContainsKey(_selectedActorKey))
                ClearStageSelection();
        }

        private bool DeleteCurrentStageSelection()
        {
            if (!IsStageAuthoringMode)
                return false;

            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
            {
                OnRemoveSelectedActorClicked();
                return true;
            }

            if (_selectionKind == StageSelectionKind.Background)
            {
                OnClearBackgroundClicked();
                return true;
            }

            return false;
        }

        private void SaveActorStateToCurrent(string actorInstanceKey, Action<StoryActorStateData> setter, bool saveNow = false)
        {
            if (!IsStageAuthoringMode)
                return;

            if (_currentLine == null || string.IsNullOrWhiteSpace(actorInstanceKey)) return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Actor Stage State");
            if (layout == null) return;

            Undo.RecordObject(layout, "Edit Actor Stage State");

            var entry = FindActorEntry(layout, actorInstanceKey);

            if (entry == null)
            {
                entry = _stageState.TryGetValue(actorInstanceKey, out var current)
                    ? current.ShallowClone()
                    : new StoryActorStateData();
                entry.EnsureActorInstanceKey(actorInstanceKey);
                layout.ActorsEditable.Add(entry);
            }

            setter(entry);
            entry.SyncActorKey();
            entry.EnsureActorInstanceKey(actorInstanceKey);
            _stageState[entry.ResolvedActorKey] = entry.ShallowClone();

            MarkLayoutDirty(layout, saveNow);
        }

        private static void MarkLayoutDirty(StoryStageLayoutModuleSO layout, bool saveNow)
        {
            if (layout == null)
                return;

            EditorUtility.SetDirty(layout);
            if (saveNow)
                AssetDatabase.SaveAssets();
        }
    }
}
