using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;

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

            _inspectorScrollView.Add(BuildAuthoringTools());
            _inspectorScrollView.Add(MakeSeparator());

            _inspectorScrollView.Add(MakeBoldLabel("Stage Actors"));

            _actorListRoot = new VisualElement { style = { flexDirection = FlexDirection.Column, marginBottom = 4 } };
            _inspectorScrollView.Add(_actorListRoot);
            _inspectorScrollView.Add(MakeSeparator());

            _inspectorRoot = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1 } };
            _inspectorScrollView.Add(_inspectorRoot);

            return panel;
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

            foreach (var kvp in _stageState)
            {
                string actorKey = kvp.Key;
                var data  = kvp.Value;
                var actor = data.actor;
                if (actor == null) continue;

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
            posField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.normalizedPosition = e.newValue;
                RepositionActors();
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.normalizedPosition = e.newValue);
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
            actorScaleField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.scale = ResolveNonZeroScale(e.newValue);
                PositionSelectedActorElement();
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.scale = data.scale);
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
            scaleField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.scaleX = e.newValue;
                PositionSelectedActorElement();
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.scaleX = e.newValue);
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
            _inspectorRoot.Add(MakeBoldLabel("Motion"));
            _inspectorRoot.Add(new Label("Preview: previous accumulated state -> current line state")
            {
                style =
                {
                    fontSize = 9,
                    color = new StyleColor(new Color(0.58f, 0.60f, 0.66f)),
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 4
                }
            });

            var motionField = new EnumField("Enter", data.enterMotion)
            {
                style =
                {
                    marginBottom = 3
                }
            };
            motionField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                var v = (StoryEnterMotionType)e.newValue;
                data.enterMotion = v;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.enterMotion = v);
            });
            _inspectorRoot.Add(motionField);

            var durField = new FloatField("Enter Duration")
            {
                value = data.enterDuration,
                style =
                {
                    marginBottom = 3
                }
            };
            durField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.enterDuration = Mathf.Clamp(e.newValue, 0f, 3f);
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.enterDuration = data.enterDuration);
            });
            _inspectorRoot.Add(durField);

            var moveField = new EnumField("Move", data.moveMotion)
            {
                style = { marginBottom = 3 }
            };
            moveField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                var v = (StoryStageMoveMotionType)e.newValue;
                data.moveMotion = v;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.moveMotion = v);
            });
            _inspectorRoot.Add(moveField);

            var moveDurationField = new FloatField("Move Duration")
            {
                value = data.moveDuration,
                style = { marginBottom = 3 }
            };
            moveDurationField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.moveDuration = Mathf.Clamp(e.newValue, 0f, 3f);
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.moveDuration = data.moveDuration);
            });
            _inspectorRoot.Add(moveDurationField);

            var exitField = new EnumField("Exit", data.exitMotion)
            {
                style = { marginBottom = 3 }
            };
            exitField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                var v = (StoryEnterMotionType)e.newValue;
                data.exitMotion = v;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.exitMotion = v);
            });
            _inspectorRoot.Add(exitField);

            var exitDurationField = new FloatField("Exit Duration")
            {
                value = data.exitDuration,
                style = { marginBottom = 3 }
            };
            exitDurationField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.exitDuration = Mathf.Clamp(e.newValue, 0f, 3f);
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.exitDuration = data.exitDuration);
            });
            _inspectorRoot.Add(exitDurationField);

            return;

            void RepositionActors()
            {
                foreach (var kvp in _actorElements)
                {
                    if (_stageState.TryGetValue(kvp.Key, out var actorStateData))
                        PositionActorElement(kvp.Value, actorStateData);
                }
            }
        }

        private void BuildActorTimelineInspector(StoryActorStateData actorState)
        {
            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Line Keyframes"));

            if (string.IsNullOrWhiteSpace(_selectedActorKey))
            {
                _inspectorRoot.Add(new Label("No actor instance selected."));
                return;
            }

            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StoryActorTrackData track = FindActorTrack(layout, _selectedActorKey);
            int keyframeCount = track?.keyframes?.Count ?? 0;

            var buttonRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginBottom = 4 }
            };

            buttonRow.Add(new Button(() => AddActorKeyframeFromCurrent(actorState))
            {
                text = "Add Key",
                style = { flexGrow = 1, marginRight = 4, height = 22 }
            });
            buttonRow.Add(new Button(() => RemoveLastActorKeyframe())
            {
                text = "Remove Last",
                style = { flexGrow = 1, height = 22 }
            });
            _inspectorRoot.Add(buttonRow);

            if (keyframeCount == 0)
            {
                _inspectorRoot.Add(new Label("No keyframes. Add Key stores the selected actor's current state.")
                {
                    style =
                    {
                        fontSize = 9,
                        color = new StyleColor(new Color(0.58f, 0.60f, 0.66f)),
                        whiteSpace = WhiteSpace.Normal
                    }
                });
                return;
            }

            for (int i = 0; i < keyframeCount; i++)
            {
                StoryActorKeyframeData keyframe = track.keyframes[i];
                if (keyframe == null)
                    continue;

                _inspectorRoot.Add(MakeBoldLabel($"Key {i + 1}"));

                int index = i;
                var timeField = new Slider("Time", 0f, 1f)
                {
                    value = keyframe.normalizedTime,
                    showInputField = true,
                    style = { marginBottom = 2 }
                };
                timeField.RegisterValueChangedCallback(e =>
                {
                    SaveActorTrackToCurrent(_selectedActorKey, currentTrack =>
                    {
                        if (index < currentTrack.keyframes.Count)
                            currentTrack.keyframes[index].normalizedTime = Mathf.Clamp01(e.newValue);
                    });
                });
                _inspectorRoot.Add(timeField);

                var positionField = new Vector2Field("Position")
                {
                    value = keyframe.normalizedPosition,
                    style = { marginBottom = 2 }
                };
                positionField.RegisterValueChangedCallback(e =>
                {
                    SaveActorTrackToCurrent(_selectedActorKey, currentTrack =>
                    {
                        if (index < currentTrack.keyframes.Count)
                            currentTrack.keyframes[index].normalizedPosition = e.newValue;
                    });
                });
                _inspectorRoot.Add(positionField);

                var scaleField = new Vector2Field("Scale")
                {
                    value = keyframe.scale,
                    style = { marginBottom = 2 }
                };
                scaleField.RegisterValueChangedCallback(e =>
                {
                    SaveActorTrackToCurrent(_selectedActorKey, currentTrack =>
                    {
                        if (index < currentTrack.keyframes.Count)
                            currentTrack.keyframes[index].scale = ResolveNonZeroScale(e.newValue);
                    });
                });
                _inspectorRoot.Add(scaleField);

                var visibleToggle = new Toggle("Visible")
                {
                    value = keyframe.visible,
                    style = { marginBottom = 2 }
                };
                visibleToggle.RegisterValueChangedCallback(e =>
                {
                    SaveActorTrackToCurrent(_selectedActorKey, currentTrack =>
                    {
                        if (index < currentTrack.keyframes.Count)
                            currentTrack.keyframes[index].visible = e.newValue;
                    });
                });
                _inspectorRoot.Add(visibleToggle);

                var easingField = new EnumField("Easing", keyframe.easing)
                {
                    style = { marginBottom = 5 }
                };
                easingField.RegisterValueChangedCallback(e =>
                {
                    SaveActorTrackToCurrent(_selectedActorKey, currentTrack =>
                    {
                        if (index < currentTrack.keyframes.Count)
                            currentTrack.keyframes[index].easing = (StoryStageMoveMotionType)e.newValue;
                    });
                });
                _inspectorRoot.Add(easingField);
            }
        }


        //
        private void BuildBackgroundInspector()
        {
            StoryBackgroundStateData data = _bgState?.ShallowClone() ?? new StoryBackgroundStateData();

            _inspectorRoot.Add(MakeBoldLabel("Stage Background"));
            _inspectorRoot.Add(MakeSeparator());

            var definitionField = new ObjectField("Definition")
            {
                objectType = typeof(BackgroundDefinitionSO),
                allowSceneObjects = false,
                value = data.background,
                style = { marginBottom = 3 }
            };
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
            offsetField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.normalizedOffset = e.newValue;
                SaveBackgroundStateToCurrent(entry => entry.normalizedOffset = e.newValue);
            });
            _inspectorRoot.Add(offsetField);

            var scaleField = new Vector2Field("Scale")
            {
                value = data.scale,
                style = { marginBottom = 3 }
            };
            scaleField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.scale = ResolveNonZeroScale(e.newValue);
                SaveBackgroundStateToCurrent(entry => entry.scale = data.scale);
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
            _inspectorRoot.Add(MakeBoldLabel("Transition"));

            var transitionField = new EnumField("Motion", data.transitionMotion)
            {
                style = { marginBottom = 3 }
            };
            transitionField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                var v = (StoryEnterMotionType)e.newValue;
                data.transitionMotion = v;
                SaveBackgroundStateToCurrent(entry => entry.transitionMotion = v);
            });
            _inspectorRoot.Add(transitionField);

            var transitionDurationField = new FloatField("Duration")
            {
                value = data.transitionDuration,
                style = { marginBottom = 3 }
            };
            transitionDurationField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.transitionDuration = Mathf.Clamp(e.newValue, 0f, 3f);
                SaveBackgroundStateToCurrent(entry => entry.transitionDuration = data.transitionDuration);
            });
            _inspectorRoot.Add(transitionDurationField);
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
            if (!IsStageAuthoringMode || _currentLine == null || FindCurrentStageLayout() != null)
                return;

            if (!TryBuildStageStateBeforeLine(_currentLine, out var previousActors, out var previousBackground))
                return;

            if (previousActors.Count == 0 && previousBackground == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Import Previous Stage");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Import Previous Stage");
            layout.ActorsEditable.Clear();

            foreach (var pair in previousActors)
            {
                StoryActorStateData clone = pair.Value.ShallowClone();
                clone.EnsureActorInstanceKey(pair.Key);
                clone.SyncActorKey();
                layout.ActorsEditable.Add(clone);
            }

            CopyBackgroundState(previousBackground, layout.BackgroundEditable);
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
            _importPreviousStageBtn?.SetEnabled(isAuthoring && hasLine && !hasCurrentStageLayout && hasPreviousStage);
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

        private void AddActorKeyframeFromCurrent(StoryActorStateData actorState)
        {
            if (!IsStageAuthoringMode || _currentLine == null || string.IsNullOrWhiteSpace(_selectedActorKey) || actorState == null)
                return;

            SaveActorTrackToCurrent(_selectedActorKey, track =>
            {
                float time = track.keyframes.Count == 0 ? 0f : 1f;
                track.keyframes.Add(new StoryActorKeyframeData
                {
                    normalizedTime = time,
                    timeSeconds = time,
                    normalizedPosition = actorState.normalizedPosition,
                    scale = actorState.scale,
                    scaleX = actorState.scaleX,
                    visible = actorState.visible,
                    easing = actorState.moveMotion
                });
            }, refresh: true);
        }

        private void RemoveLastActorKeyframe()
        {
            if (!IsStageAuthoringMode || _currentLine == null || string.IsNullOrWhiteSpace(_selectedActorKey))
                return;

            SaveActorTrackToCurrent(_selectedActorKey, track =>
            {
                if (track.keyframes.Count > 0)
                    track.keyframes.RemoveAt(track.keyframes.Count - 1);
            }, refresh: true);
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

            setter(track);
            track.keyframes.RemoveAll(k => k == null);
            track.keyframes.Sort((a, b) => StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b)));
            MarkLayoutDirty(layout, saveNow: false);

            if (refresh)
            {
                RefreshActorInspector();
                RefreshTimelinePanel();
            }
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

            if (_timelineRecordEnabled
                && !string.IsNullOrWhiteSpace(_timelineRecordActorKey)
                && actorInstanceKey != _timelineRecordActorKey)
                return;

            _selectionKind = StageSelectionKind.Actor;
            _selectedActorKey = actorInstanceKey;
            _selectedTimelineKeyIndex = -1;
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

            _selectionKind = StageSelectionKind.Background;
            _selectedActorKey = null;
            _selectedTimelineKeyIndex = -1;
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
            _selectedTimelineKeyIndex = -1;
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
