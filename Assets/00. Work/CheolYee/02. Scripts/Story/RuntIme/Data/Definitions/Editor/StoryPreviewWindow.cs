using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 스토리 에피소드를 시각적으로 미리보고 캐릭터 배치를 authoring 하는 별도 EditorWindow.
    ///
    /// ■ 사용 흐름
    ///   1. StoryGraphEditorWindow 상단 "Preview" 버튼 또는 Tools/Story/Story Preview 로 열기
    ///   2. 그래프 에디터에서 노드 선택 → 해당 라인 시점의 스테이지 상태 표시
    ///   3. 배우 드래그 → 위치 즉시 StoryStageLayoutModuleSO sub-asset 에 저장
    ///   4. 런타임 재생 시 동일 데이터를 StoryStageLayoutModuleExecutor 가 읽어 반영
    /// </summary>
    public sealed class StoryPreviewWindow : EditorWindow
    {
        [MenuItem("Tools/Story/Story Preview")]
        public static StoryPreviewWindow Open()
        {
            var w = GetWindow<StoryPreviewWindow>("Story Preview");
            w.minSize = new Vector2(780, 520);
            return w;
        }

        // ── 외부 진입점 ──────────────────────────────
        /// <summary>그래프 에디터가 에피소드 변경 시 호출.</summary>
        public static void NotifyEpisodeChanged(StoryEpisodeSO episode)
        {
            if (!HasOpenInstances<StoryPreviewWindow>()) return;
            GetWindow<StoryPreviewWindow>(false, null, false).SetEpisode(episode);
        }

        /// <summary>그래프 에디터에서 노드 선택 시 호출.</summary>
        public static void NotifyLineSelected(StoryLineSO line)
        {
            if (!HasOpenInstances<StoryPreviewWindow>()) return;
            GetWindow<StoryPreviewWindow>(false, null, false).OnExternalLineSelected(line);
        }

        // ── 상수 ─────────────────────────────────────
        private const float StageAspect      = 16f / 9f;
        private const float ActorWidthFrac   = 0.16f;   // 스테이지 폭 대비 액터 폭 비율
        private const float DefaultActorAspect = 1.8f;  // 스프라이트 없을 때 height/width
        private const float InspectorWidth   = 220f;
        private const float DialoguePanelH   = 80f;

        // ── 에피소드/재생 상태 ────────────────────────
        [SerializeField] private StoryEpisodeSO _episode;
        private StoryLineSO _currentLine;
        private bool        _isPlaying;

        // ── 스테이지 누적 상태 ────────────────────────
        /// <summary>현재 표시 중인 스테이지 상태 (라인 경로를 따라 누적).</summary>
        private readonly Dictionary<CharacterDefinitionSO, StoryActorStateData> _stageState = new();

        // ── 액터 인터랙션 ─────────────────────────────
        private CharacterDefinitionSO _selectedActor;
        private CharacterDefinitionSO _draggingActor;
        private Vector2               _dragStartScreenPos;
        private Vector2               _dragStartNormPos;

        // ── UI 참조 ──────────────────────────────────
        private ObjectField    _episodeField;
        private Label          _statusLabel;
        private Button         _playBtn;
        private Button         _fromHereBtn;
        private Button         _stopBtn;
        private Button         _nextBtn;
        private VisualElement  _stageWrapper;
        private VisualElement  _stageCanvas;
        private VisualElement  _actorLayer;
        private Label          _speakerLabel;
        private Label          _dialogueLabel;
        private VisualElement  _choiceArea;
        private VisualElement  _inspectorRoot;

        // actor → element 매핑 (드래그 / 하이라이트 갱신)
        private readonly Dictionary<CharacterDefinitionSO, VisualElement> _actorElements = new();

        // ── 외부에서 보낸 "여기서 재생" 대상 라인 ────────
        private StoryLineSO _pendingFromLine;

        // ── 생명주기 ─────────────────────────────────

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            root.Add(BuildTopBar());

            var main = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexGrow = 1 }
            };

            main.Add(BuildLeftPanel());
            main.Add(BuildActorInspector());
            root.Add(main);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            BuildStageStateAt(_currentLine);
            RebuildActorLayer();
            RefreshActorInspector();
        }

        // ── 상단 바 ──────────────────────────────────

        private VisualElement BuildTopBar()
        {
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 8, paddingRight = 8,
                    paddingTop = 5, paddingBottom = 5,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f))
                }
            };

            _episodeField = new ObjectField("Episode") { objectType = typeof(StoryEpisodeSO), style = { flexGrow = 1, marginRight = 12 } };
            _episodeField.RegisterValueChangedCallback(e => SetEpisode(e.newValue as StoryEpisodeSO));
            bar.Add(_episodeField);

            _playBtn     = MakeBtn("▶ Play",      new Color(0.18f, 0.5f, 0.18f), OnPlay);
            _fromHereBtn = MakeBtn("▶ From Here", new Color(0.18f, 0.35f, 0.55f), OnFromHere);
            _stopBtn     = MakeBtn("■ Stop",      new Color(0.5f, 0.18f, 0.18f), OnStop);
            _nextBtn     = MakeBtn("▷ Next",      new Color(0.3f, 0.3f, 0.3f),  OnNext);

            bar.Add(_playBtn);
            bar.Add(_fromHereBtn);
            bar.Add(_stopBtn);
            bar.Add(_nextBtn);

            _statusLabel = new Label("정지") { style = { marginLeft = 10, fontSize = 10, color = new StyleColor(new Color(0.55f, 0.55f, 0.55f)) } };
            bar.Add(_statusLabel);

            return bar;
        }

        // ── 좌측 패널 (스테이지 + 대화 + 선택지) ────────

        private VisualElement BuildLeftPanel()
        {
            var left = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };

            // 스테이지 래퍼
            _stageWrapper = new VisualElement { style = { flexGrow = 1, overflow = Overflow.Hidden, backgroundColor = new StyleColor(new Color(0.05f, 0.05f, 0.05f)) } };
            _stageCanvas  = new VisualElement { style = { position = Position.Absolute, overflow = Overflow.Hidden, backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.12f)) } };
            _actorLayer   = new VisualElement { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0 } };
            _stageCanvas.Add(_actorLayer);
            _stageWrapper.Add(_stageCanvas);
            _stageWrapper.RegisterCallback<GeometryChangedEvent>(OnStageWrapperResized);
            left.Add(_stageWrapper);

            // 대화 패널
            var dialoguePanel = new VisualElement
            {
                style =
                {
                    flexShrink = 0,
                    height = DialoguePanelH,
                    paddingLeft = 14, paddingRight = 14,
                    paddingTop = 8, paddingBottom = 8,
                    backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.14f)),
                    borderTopWidth = 1, borderTopColor = new StyleColor(new Color(0.08f, 0.08f, 0.08f))
                }
            };
            _speakerLabel = new Label("") { style = { fontSize = 10, color = new StyleColor(new Color(0.7f, 0.7f, 0.4f)), marginBottom = 3 } };
            _dialogueLabel = new Label("") { style = { fontSize = 12, whiteSpace = WhiteSpace.Normal, color = new StyleColor(new Color(0.92f, 0.92f, 0.92f)) } };
            dialoguePanel.Add(_speakerLabel);
            dialoguePanel.Add(_dialogueLabel);
            left.Add(dialoguePanel);

            // 선택지
            _choiceArea = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 4 } };
            left.Add(_choiceArea);

            return left;
        }

        // ── 우측 액터 인스펙터 패널 ──────────────────

        private VisualElement BuildActorInspector()
        {
            var panel = new VisualElement
            {
                style =
                {
                    width = InspectorWidth,
                    flexShrink = 0,
                    borderLeftWidth = 1,
                    borderLeftColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f)),
                    paddingLeft = 8, paddingRight = 8, paddingTop = 8
                }
            };

            var title = new Label("액터 인스펙터") { style = { fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 8, color = new StyleColor(new Color(0.8f, 0.8f, 0.8f)) } };
            panel.Add(title);

            _inspectorRoot = new VisualElement();
            panel.Add(_inspectorRoot);

            RefreshActorInspector();
            return panel;
        }

        // ── 스테이지 크기 자동 조정 ────────────────────

        private void OnStageWrapperResized(GeometryChangedEvent e)
        {
            float wrapW = e.newRect.width;
            float wrapH = e.newRect.height;
            if (wrapW <= 0 || wrapH <= 0) return;

            float w = wrapW;
            float h = w / StageAspect;
            if (h > wrapH) { h = wrapH; w = h * StageAspect; }

            float ox = (wrapW - w) * 0.5f;
            float oy = (wrapH - h) * 0.5f;
            _stageCanvas.style.left   = ox;
            _stageCanvas.style.top    = oy;
            _stageCanvas.style.width  = w;
            _stageCanvas.style.height = h;

            RepositionActors();
        }

        // ── 외부 API ─────────────────────────────────

        public void SetEpisode(StoryEpisodeSO ep)
        {
            _episode = ep;
            if (_episodeField != null) _episodeField.SetValueWithoutNotify(ep);
            StopPlayback();
            _stageState.Clear();
            RebuildActorLayer();
            RefreshDialogue();
            RefreshButtons();
        }

        public void OnExternalLineSelected(StoryLineSO line)
        {
            _pendingFromLine = line;
            RefreshButtons();

            // 재생 중이 아니면 즉시 해당 라인 상태 표시 (프리뷰 스냅)
            if (!_isPlaying)
                ShowLineSnapshot(line);
        }

        // ── 재생 제어 ─────────────────────────────────

        private void OnPlay()
        {
            if (_episode == null) return;
            string startId = _episode.EntryLineId;
            if (string.IsNullOrWhiteSpace(startId) && _episode.Lines.Count > 0)
                startId = _episode.Lines[0]?.LineId;
            StartPlayFrom(startId);
        }

        private void OnFromHere()
        {
            if (_pendingFromLine == null || _episode == null) return;
            StartPlayFrom(_pendingFromLine.LineId);
        }

        private void OnStop()
        {
            StopPlayback();
        }

        private void OnNext()
        {
            if (!_isPlaying || _currentLine == null) return;
            string next = _currentLine.NextLineId;
            if (string.IsNullOrWhiteSpace(next)) { StopPlayback(); return; }
            AdvanceTo(next);
        }

        private void StartPlayFrom(string lineId)
        {
            if (_episode == null) return;
            _isPlaying = true;
            _stageState.Clear();
            _selectedActor = null;
            AdvanceTo(lineId);
        }

        private void AdvanceTo(string lineId)
        {
            if (string.IsNullOrWhiteSpace(lineId) || _episode == null) { StopPlayback(); return; }
            if (!_episode.TryGetLine(lineId, out var line)) { StopPlayback(); return; }
            ShowLine(line);
        }

        private void ShowLine(StoryLineSO line)
        {
            _currentLine = line;
            BuildStageStateAt(line);
            RebuildActorLayer();
            RefreshDialogue();
            RefreshChoices();
            RefreshButtons();
            RefreshActorInspector();
            Repaint();
        }

        private void ShowLineSnapshot(StoryLineSO line)
        {
            if (line == null) return;
            _currentLine = line;
            BuildStageStateAt(line);
            RebuildActorLayer();
            RefreshDialogue();
            RefreshChoices();
            RefreshButtons();
            RefreshActorInspector();
            Repaint();
        }

        private void StopPlayback()
        {
            _isPlaying   = false;
            _currentLine = null;
            _stageState.Clear();
            _actorElements.Clear();
            _actorLayer?.Clear();
            RefreshDialogue();
            RefreshChoices();
            RefreshButtons();
            RefreshActorInspector();
            _statusLabel.text = "정지";
        }

        private void OnChoiceSelected(string reactionStartLineId)
        {
            if (string.IsNullOrWhiteSpace(reactionStartLineId)) { StopPlayback(); return; }
            AdvanceTo(reactionStartLineId);
        }

        // ── 스테이지 상태 누적 계산 ────────────────────

        /// <summary>에피소드 진입점부터 targetLine 까지 graph 를 따라가며 stage state 를 누적한다.</summary>
        private void BuildStageStateAt(StoryLineSO targetLine)
        {
            _stageState.Clear();
            if (_episode == null || targetLine == null) return;

            var visited = new HashSet<string>();
            string currentId = _episode.EntryLineId;
            if (string.IsNullOrWhiteSpace(currentId) && _episode.Lines.Count > 0)
                currentId = _episode.Lines[0]?.LineId;

            while (!string.IsNullOrWhiteSpace(currentId) && !visited.Contains(currentId))
            {
                visited.Add(currentId);
                if (!_episode.TryGetLine(currentId, out var line)) break;

                ApplyStageModulesToState(line);
                if (line == targetLine) break;

                currentId = line.NextLineId;
            }

            // 진입점부터 도달하지 못한 경우(orphan / choice branch 내 노드)
            // → 해당 라인의 모듈만 직접 적용
            if (!visited.Contains(targetLine.LineId))
            {
                _stageState.Clear();
                ApplyStageModulesToState(targetLine);
            }
        }

        private void ApplyStageModulesToState(StoryLineSO line)
        {
            foreach (var m in line.Modules)
            {
                if (m is not StoryStageLayoutModuleSO layout) continue;
                foreach (var data in layout.Actors)
                {
                    if (data?.actor == null) continue;
                    _stageState[data.actor] = data;
                }
            }
        }

        // ── 액터 레이어 렌더링 ─────────────────────────

        private void RebuildActorLayer()
        {
            _actorLayer.Clear();
            _actorElements.Clear();

            float stageW = _stageCanvas.resolvedStyle.width;
            float stageH = _stageCanvas.resolvedStyle.height;

            foreach (var kvp in _stageState)
            {
                var actor = kvp.Key;
                var data  = kvp.Value;
                if (!data.visible) continue;

                var el = CreateActorElement(actor, data);
                _actorLayer.Add(el);
                _actorElements[actor] = el;

                if (stageW > 0 && stageH > 0)
                    PositionActorElement(el, data, stageW, stageH);
            }
        }

        private VisualElement CreateActorElement(CharacterDefinitionSO actor, StoryActorStateData data)
        {
            var el = new VisualElement
            {
                name = actor.CharacterId,
                style =
                {
                    position = Position.Absolute,
                    overflow = Overflow.Hidden,
                    borderTopLeftRadius = 3, borderTopRightRadius = 3
                },
                tooltip = actor.DisplayName
            };

            // 스프라이트 or 색상 플레이스홀더
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
                var nameLbl = new Label(actor.DisplayName) { style = { fontSize = 9, color = new StyleColor(Color.white), unityTextAlign = TextAnchor.UpperCenter } };
                el.Add(nameLbl);
            }

            // 포커스 dim
            ApplyActorFocusStyle(el, data);

            // 선택 하이라이트
            if (_selectedActor == actor)
                el.style.borderTopWidth = el.style.borderRightWidth =
                el.style.borderBottomWidth = el.style.borderLeftWidth = 2;

            // 인터랙션 등록
            RegisterActorInteraction(el, actor, data);

            return el;
        }

        private static void ApplyActorFocusStyle(VisualElement el, StoryActorStateData data)
        {
            el.style.unityBackgroundImageTintColor = new StyleColor(
                data.focused ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f));
            el.style.opacity = data.focused ? 1f : 0.65f;
        }

        private void PositionActorElement(VisualElement el, StoryActorStateData data, float stageW, float stageH)
        {
            var sprite = data.actor?.PreviewSprite;
            float aw = stageW * ActorWidthFrac * Mathf.Abs(data.scaleX);
            float aspect = (sprite != null && sprite.rect.width > 0)
                ? sprite.rect.height / sprite.rect.width
                : DefaultActorAspect;
            float ah = aw * aspect;

            // UIElements: Y=0 이 위쪽. normY=0 → 지면(하단), normY=1 → 상단
            float x = data.normalizedPosition.x * stageW - aw * 0.5f;
            float y = stageH - data.normalizedPosition.y * stageH - ah;

            el.style.width  = aw;
            el.style.height = ah;
            el.style.left   = Mathf.Clamp(x, -aw * 0.5f, stageW - aw * 0.5f);
            el.style.top    = Mathf.Clamp(y, 0f, stageH - 4f);
        }

        private void RepositionActors()
        {
            float stageW = _stageCanvas.resolvedStyle.width;
            float stageH = _stageCanvas.resolvedStyle.height;
            if (stageW <= 0 || stageH <= 0) return;

            foreach (var kvp in _actorElements)
            {
                if (!_stageState.TryGetValue(kvp.Key, out var data)) continue;
                PositionActorElement(kvp.Value, data, stageW, stageH);
            }
        }

        // ── 액터 드래그 인터랙션 ──────────────────────

        private void RegisterActorInteraction(VisualElement el, CharacterDefinitionSO actor, StoryActorStateData data)
        {
            el.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                _selectedActor      = actor;
                _draggingActor      = actor;
                _dragStartScreenPos = e.position;
                _dragStartNormPos   = data.normalizedPosition;
                el.CapturePointer(e.pointerId);
                HighlightSelectedActor();
                RefreshActorInspector();
                e.StopPropagation();
            });

            el.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (_draggingActor != actor) return;
                float stageW = _stageCanvas.resolvedStyle.width;
                float stageH = _stageCanvas.resolvedStyle.height;
                if (stageW <= 0 || stageH <= 0) return;

                Vector2 delta = (Vector2)e.position - _dragStartScreenPos;
                float   normDX = delta.x / stageW;
                float   normDY = -delta.y / stageH;  // screen Y 반전

                var newNorm = new Vector2(
                    Mathf.Clamp01(_dragStartNormPos.x + normDX),
                    Mathf.Clamp01(_dragStartNormPos.y + normDY));

                data.normalizedPosition = newNorm;  // 임시 업데이트 (참조이므로 stageState에도 반영)
                PositionActorElement(el, data, stageW, stageH);
                RefreshActorInspector();
                e.StopPropagation();
            });

            el.RegisterCallback<PointerUpEvent>(e =>
            {
                if (_draggingActor != actor) return;
                _draggingActor = null;
                el.ReleasePointer(e.pointerId);
                // 드래그 완료 → SO에 저장
                SaveActorStateToCurrent(actor, entry => entry.normalizedPosition = data.normalizedPosition);
                e.StopPropagation();
            });
        }

        private void HighlightSelectedActor()
        {
            foreach (var kvp in _actorElements)
            {
                bool sel = kvp.Key == _selectedActor;
                float bw = sel ? 2f : 0f;
                kvp.Value.style.borderTopWidth = kvp.Value.style.borderRightWidth =
                kvp.Value.style.borderBottomWidth = kvp.Value.style.borderLeftWidth = bw;
                kvp.Value.style.borderTopColor = kvp.Value.style.borderRightColor =
                kvp.Value.style.borderBottomColor = kvp.Value.style.borderLeftColor =
                    new StyleColor(new Color(0.9f, 0.7f, 0.2f));
            }
        }

        // ── 저장: 현재 라인의 StoryStageLayoutModuleSO 에 기록 ──

        private void SaveActorStateToCurrent(CharacterDefinitionSO actor, Action<StoryActorStateData> modify)
        {
            if (_currentLine == null) return;

            // 현재 라인의 StageLayoutModule 탐색 (없으면 생성)
            StoryStageLayoutModuleSO module = null;
            foreach (var m in _currentLine.Modules)
                if (m is StoryStageLayoutModuleSO sl) { module = sl; break; }

            if (module == null)
            {
                module = (StoryStageLayoutModuleSO)StoryEditorUtility.AddModule(
                    _currentLine, typeof(StoryStageLayoutModuleSO));
            }

            // actors 리스트에서 해당 캐릭터 항목 탐색 (없으면 추가)
            var list  = module.ActorsEditable;
            var entry = list.Find(a => a.actor == actor);
            if (entry == null)
            {
                // 누적 상태를 초기값으로 복사
                entry = _stageState.TryGetValue(actor, out var acc)
                    ? acc.ShallowClone()
                    : new StoryActorStateData { actor = actor };
                list.Add(entry);
            }

            Undo.RecordObject(module, "Edit Actor State");
            modify(entry);
            EditorUtility.SetDirty(module);

            // 로컬 stageState도 sync
            _stageState[actor] = entry;
        }

        // ── 액터 인스펙터 패널 갱신 ──────────────────

        private void RefreshActorInspector()
        {
            _inspectorRoot.Clear();

            if (_selectedActor == null || !_stageState.TryGetValue(_selectedActor, out var data))
            {
                _inspectorRoot.Add(new Label("액터를 클릭하면\n상태를 편집할 수 있습니다.")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)), whiteSpace = WhiteSpace.Normal }
                });
                return;
            }

            var actor = _selectedActor;

            _inspectorRoot.Add(MakeBoldLabel(actor.DisplayName));
            _inspectorRoot.Add(MakeSeparator());

            AddVec2Field("위치 (0-1)", data.normalizedPosition, v =>
            {
                data.normalizedPosition = v;
                RepositionActors();
                SaveActorStateToCurrent(actor, e => e.normalizedPosition = v);
            });

            AddFloatField("Scale X", data.scaleX, v =>
            {
                data.scaleX = v;
                RebuildActorLayer();
                SaveActorStateToCurrent(actor, e => e.scaleX = v);
            });

            AddToggleField("표시", data.visible, v =>
            {
                data.visible = v;
                RebuildActorLayer();
                SaveActorStateToCurrent(actor, e => e.visible = v);
            });

            AddToggleField("포커스", data.focused, v =>
            {
                data.focused = v;
                if (_actorElements.TryGetValue(actor, out var el))
                    ApplyActorFocusStyle(el, data);
                SaveActorStateToCurrent(actor, e => e.focused = v);
            });

            AddIntField("Sort Order", data.sortOrder, v =>
            {
                data.sortOrder = v;
                SaveActorStateToCurrent(actor, e => e.sortOrder = v);
            });

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("등장 연출"));

            AddEnumField("Enter Motion", data.enterMotion, v =>
            {
                data.enterMotion = (StoryEnterMotionType)v;
                SaveActorStateToCurrent(actor, e => e.enterMotion = (StoryEnterMotionType)v);
            });

            AddFloatField("Enter Duration", data.enterDuration, v =>
            {
                data.enterDuration = v;
                SaveActorStateToCurrent(actor, e => e.enterDuration = v);
            });

            AddFloatField("Move Duration", data.moveDuration, v =>
            {
                data.moveDuration = v;
                SaveActorStateToCurrent(actor, e => e.moveDuration = v);
            });

            AddTextField("Pose Key", data.poseKey, v =>
            {
                data.poseKey = v;
                SaveActorStateToCurrent(actor, e => e.poseKey = v);
            });

            // 스테이지 레이아웃 모듈 없으면 안내
            bool hasModule = false;
            if (_currentLine != null)
                foreach (var m in _currentLine.Modules)
                    if (m is StoryStageLayoutModuleSO) { hasModule = true; break; }

            if (!hasModule)
                _inspectorRoot.Add(new Label("* 이 라인에 StageLayout 모듈이 없음.\n  값 수정 시 자동 생성됩니다.")
                    { style = { fontSize = 9, color = new StyleColor(new Color(1f, 0.65f, 0.2f)), marginTop = 6, whiteSpace = WhiteSpace.Normal } });
        }

        // ── 대화/선택지 갱신 ──────────────────────────

        private void RefreshDialogue()
        {
            if (_speakerLabel == null) return;
            if (_currentLine == null)
            {
                _speakerLabel.text  = "";
                _dialogueLabel.text = _isPlaying ? "(라인 없음)" : "에피소드를 선택하고 재생하세요.";
                return;
            }
            _speakerLabel.text  = _currentLine.GetResolvedSpeakerName() is { Length: > 0 } n ? n : "(내레이션)";
            _dialogueLabel.text = _currentLine.DialogueText ?? "";
            _statusLabel.text   = $"재생 중  [{_currentLine.LineId}]";
        }

        private void RefreshChoices()
        {
            _choiceArea.Clear();
            if (_currentLine == null) return;

            foreach (var m in _currentLine.Modules)
            {
                if (m is not IStoryChoiceLikeModule choice) continue;
                foreach (var opt in choice.Options)
                {
                    string rxId = opt.ReactionStartLineId;
                    _choiceArea.Add(new Button(() => OnChoiceSelected(rxId))
                    {
                        text = opt.Text,
                        style = { marginRight = 6, marginBottom = 4, whiteSpace = WhiteSpace.Normal, maxWidth = 200 }
                    });
                }
            }
        }

        // ── 버튼 상태 갱신 ────────────────────────────

        private void RefreshButtons()
        {
            bool hasEp    = _episode != null;
            bool hasFrom  = hasEp && _pendingFromLine != null;
            bool hasNext  = _isPlaying && _currentLine != null && !string.IsNullOrWhiteSpace(_currentLine.NextLineId);

            _playBtn?.SetEnabled(hasEp);
            _fromHereBtn?.SetEnabled(hasFrom);
            _stopBtn?.SetEnabled(_isPlaying);
            _nextBtn?.SetEnabled(hasNext);
        }

        // ── 인스펙터 헬퍼 ─────────────────────────────

        private static Label MakeBoldLabel(string text) =>
            new(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10, marginBottom = 3 } };

        private static VisualElement MakeSeparator()
        {
            var s = new VisualElement { style = { height = 1, backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f)), marginTop = 5, marginBottom = 5 } };
            return s;
        }

        private void AddVec2Field(string label, Vector2 value, Action<Vector2> onChange)
        {
            var f = new Vector2Field(label) { value = value };
            f.style.marginBottom = 3;
            f.RegisterValueChangedCallback(e => onChange(e.newValue));
            _inspectorRoot.Add(f);
        }

        private void AddFloatField(string label, float value, Action<float> onChange)
        {
            var f = new FloatField(label) { value = value };
            f.style.marginBottom = 3;
            f.RegisterValueChangedCallback(e => onChange(e.newValue));
            _inspectorRoot.Add(f);
        }

        private void AddIntField(string label, int value, Action<int> onChange)
        {
            var f = new IntegerField(label) { value = value };
            f.style.marginBottom = 3;
            f.RegisterValueChangedCallback(e => onChange(e.newValue));
            _inspectorRoot.Add(f);
        }

        private void AddToggleField(string label, bool value, Action<bool> onChange)
        {
            var f = new Toggle(label) { value = value };
            f.style.marginBottom = 3;
            f.RegisterValueChangedCallback(e => onChange(e.newValue));
            _inspectorRoot.Add(f);
        }

        private void AddEnumField(string label, Enum value, Action<Enum> onChange)
        {
            var f = new EnumField(label, value);
            f.style.marginBottom = 3;
            f.RegisterValueChangedCallback(e => onChange(e.newValue));
            _inspectorRoot.Add(f);
        }

        private void AddTextField(string label, string value, Action<string> onChange)
        {
            var f = new TextField(label) { value = value };
            f.style.marginBottom = 3;
            f.RegisterValueChangedCallback(e => onChange(e.newValue));
            _inspectorRoot.Add(f);
        }

        // ── 유틸 ─────────────────────────────────────

        private static Button MakeBtn(string text, Color bg, Action click) =>
            new(click)
            {
                text = text,
                style = { height = 22, paddingLeft = 8, paddingRight = 8, marginRight = 4, backgroundColor = new StyleColor(bg), fontSize = 10 }
            };

        private static Color ActorPlaceholderColor(CharacterDefinitionSO actor)
        {
            int hash = (actor.CharacterId ?? actor.name).GetHashCode();
            float h = ((hash & 0xFF) / 255f);
            return Color.HSVToRGB(h, 0.55f, 0.72f);
        }
    }
}
