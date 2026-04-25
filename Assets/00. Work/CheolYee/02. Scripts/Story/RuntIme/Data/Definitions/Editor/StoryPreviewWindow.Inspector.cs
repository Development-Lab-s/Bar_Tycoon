using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;

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

            panel.Add(BuildAuthoringTools());
            panel.Add(MakeSeparator());

            panel.Add(MakeBoldLabel("Stage Actors"));

            _actorListRoot = new VisualElement { style = { flexDirection = FlexDirection.Column, marginBottom = 4 } };
            panel.Add(_actorListRoot);
            panel.Add(MakeSeparator());

            _inspectorRoot = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1 } };
            panel.Add(_inspectorRoot);

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
                data.scale = ResolveNonZeroScale(e.newValue);
                RebuildActorLayer();
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
                data.scaleX = e.newValue;
                RebuildActorLayer();
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
                data.sortOrder = e.newValue;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.sortOrder = e.newValue);
                RebuildActorLayer();
            });
            _inspectorRoot.Add(sortField);

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Enter Motion"));

            var motionField = new EnumField("Motion", data.enterMotion)
            {
                style =
                {
                    marginBottom = 3
                }
            };
            motionField.RegisterValueChangedCallback(e =>
            {
                var v = (StoryEnterMotionType)e.newValue;
                data.enterMotion = v;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.enterMotion = v);
            });
            _inspectorRoot.Add(motionField);

            var durField = new FloatField("Duration")
            {
                value = data.enterDuration,
                style =
                {
                    marginBottom = 3
                }
            };
            durField.RegisterValueChangedCallback(e =>
            {
                data.enterDuration = Mathf.Clamp(e.newValue, 0f, 3f);
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.enterDuration = data.enterDuration);
            });
            _inspectorRoot.Add(durField);
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
                data.sortOrder = e.newValue;
                SaveBackgroundStateToCurrent(entry => entry.sortOrder = e.newValue);
            });
            _inspectorRoot.Add(sortField);
        }

        private void OnAddActorClicked()
        {
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

        private void OnRemoveSelectedActorClicked()
        {
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
            bool isAuthoring = previewMode == PreviewMode.StageAuthoring;
            bool hasLine = _currentLine != null;

            SetElementVisible(_authoringToolsRoot, isAuthoring);
            _addActorBtn?.SetEnabled(hasLine && _addActorField?.value is CharacterDefinitionSO);
            _removeSelectedActorBtn?.SetEnabled(hasLine && _selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey));
            _setBackgroundBtn?.SetEnabled(hasLine && _setBackgroundField?.value is BackgroundDefinitionSO);
            _clearBackgroundBtn?.SetEnabled(hasLine);
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

        private void SaveLayoutAndRefresh(StoryStageLayoutModuleSO layout)
        {
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();

            BuildStageStateAt(_currentLine);
            if (_selectionKind == StageSelectionKind.Actor && !_stageState.ContainsKey(_selectedActorKey))
                ClearStageSelection();
            RebuildActorLayer();
            RefreshActorInspector();
            RefreshAuthoringControls();
        }

        private static string ResolveBackgroundKey(BackgroundDefinitionSO background)
        {
            if (background == null)
                return "";

            return !string.IsNullOrWhiteSpace(background.BackgroundId)
                ? background.BackgroundId
                : background.name;
        }

        private void SaveBackgroundStateToCurrent(Action<StoryBackgroundStateData> setter)
        {
            if (_currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Stage Background");
            if (layout == null)
                return;

            Undo.RecordObject(layout, "Edit Stage Background");
            setter(layout.BackgroundEditable);
            SaveLayoutAndRefresh(layout);
        }

        private void SelectActor(string actorInstanceKey, bool refreshInspector = true)
        {
            _selectionKind = StageSelectionKind.Actor;
            _selectedActorKey = actorInstanceKey;
            RefreshActorList();
            HighlightSelectedActor();
            if (refreshInspector)
                RefreshActorInspector();
            RefreshAuthoringControls();
        }

        private void SelectBackground()
        {
            _selectionKind = StageSelectionKind.Background;
            _selectedActorKey = null;
            RefreshActorList();
            HighlightSelectedActor();
            RefreshActorInspector();
            RefreshAuthoringControls();
        }

        private void ClearStageSelection()
        {
            _selectionKind = StageSelectionKind.None;
            _selectedActorKey = null;
        }

        private void ValidateStageSelection()
        {
            if (_selectionKind == StageSelectionKind.Actor && !_stageState.ContainsKey(_selectedActorKey))
                ClearStageSelection();
        }

        private bool DeleteCurrentStageSelection()
        {
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

        private void SaveActorStateToCurrent(string actorInstanceKey, Action<StoryActorStateData> setter)
        {
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

            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
        }
    }
}
