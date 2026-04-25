using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 스토리 스테이지 미리보기 및 캐릭터 배치 authoring 전용 EditorWindow.
    ///
    /// ■ 구조
    ///   - StoryPreviewWindow.cs          : 핵심 필드 / 생명주기 / 외부 API / 공용 유틸
    ///   - StoryPreviewWindow.Layout.cs   : UI 계층 빌드 (TopBar / LeftPanel / StageRenderingRoot)
    ///   - StoryPreviewWindow.StageWorld.cs : Stage World pan/zoom / GameView 감지
    ///   - StoryPreviewWindow.Actors.cs   : 액터 레이어 렌더링 / 드래그 인터랙션
    ///   - StoryPreviewWindow.Background.cs : 배경 레이어 렌더링
    ///   - StoryPreviewWindow.Inspector.cs  : 액터 목록 / 인스펙터 / SO 저장
    ///   - StoryPreviewWindow.Playback.cs   : 재생 흐름 / 스테이지 상태 누적 / 대화·선택지
    /// </summary>
    public sealed partial class StoryPreviewWindow : EditorWindow
    {
        private enum PreviewMode { RuntimePreview, StageAuthoring }
        private enum StageSelectionKind { None, Actor, Background }

        // ── 메뉴 / 열기 ──────────────────────────────

        [MenuItem("Tools/Story/Story Preview")]
        public static StoryPreviewWindow Open()
        {
            var w = GetWindow<StoryPreviewWindow>("Story Preview");
            w.minSize = new Vector2(780, 520);
            return w;
        }

        // ── 외부 진입점 ──────────────────────────────

        /// <summary>그래프 에디터가 에피소드를 변경했을 때 호출.</summary>
        public static void NotifyEpisodeChanged(StoryEpisodeSO episode)
        {
            if (!HasOpenInstances<StoryPreviewWindow>()) return;
            GetWindow<StoryPreviewWindow>(false, null, false).SetEpisode(episode);
        }

        /// <summary>그래프 에디터에서 라인 노드를 선택했을 때 호출.</summary>
        public static void NotifyLineSelected(StoryLineSO line)
        {
            if (!HasOpenInstances<StoryPreviewWindow>()) return;
            GetWindow<StoryPreviewWindow>(false, null, false).OnExternalLineSelected(line);
        }

        // ── 렌더 관련 상수 ────────────────────────────

        private const int   FallbackRenderWidth  = 1080;
        private const int   FallbackRenderHeight = 1920;
        private const float ActorWidthFrac       = 0.24f;
        private const float DefaultActorAspect   = 1.8f;
        private const float ActorMinHeightScale  = 0.45f;
        private const float ActorMaxHeightScale  = 1.10f;
        private const float InspectorWidth       = 220f;
        private const float DialoguePanelH       = 80f;
        private const string PrefsKeyPrefix      = "CheolYee.StoryPreview.";

        // ── Stage World 상수 ───────────────────────────

        /// <summary>zoom = 1 일 때, 에디터 픽셀 기준 카메라 프레임 너비.</summary>
        private const float DefaultUnitPixels = 540f;
        private const float WorldPaddingPixels = 960f;
        private const float GridMinorPixels = 40f;
        private const float GridMajorPixels = 160f;
        private const float MinZoom           = 0.12f;
        private const float MaxZoom           = 4f;
        private const float ZoomStep          = 0.12f;
        private const float MinInspectorWidth = 180f;
        private const float MaxInspectorWidth = 460f;

        // ── 에피소드 / 재생 상태 ──────────────────────

        [SerializeField] private StoryEpisodeSO episode;
        [SerializeField] private PreviewMode    previewMode = PreviewMode.RuntimePreview;

        private StoryLineSO _currentLine;
        private bool        _isPlaying;
        private bool        _isLineSample;
        private StoryLineSO _pendingFromLine;

        // ── GameView 해상도 ────────────────────────────

        private Vector2Int _renderResolution       = new(FallbackRenderWidth, FallbackRenderHeight);
        private string     _renderResolutionSource = "Fallback";

        // ── 스테이지 누적 상태 ────────────────────────

        private readonly Dictionary<string, StoryActorStateData> _stageState = new();
        private StoryBackgroundStateData _bgState;

        // ── Stage World (pan / zoom) ───────────────────

        private float   _stageZoom      = 1f;
        private Vector2 _stagePanOffset;
        private bool    _isPanDragging;
        private Vector2 _panDragStartMouse;
        private Vector2 _panDragStartOffset;

        // ── 액터 드래그 인터랙션 ──────────────────────

        private StageSelectionKind _selectionKind;
        private string              _selectedActorKey;
        private string              _draggingActorKey;
        private Vector2               _dragStartPanelPos;
        private Vector2               _dragStartNormPos;

        // ── UI 참조: 상단 바 ──────────────────────────

        private ObjectField _episodeField;
        private EnumField   _previewModeField;
        private Label       _statusLabel;
        private Button      _playBtn;
        private Button      _fromHereBtn;
        private Button      _sampleLineBtn;
        private Button      _stopBtn;
        private Button      _nextBtn;
        private Button      _refreshGameViewBtn;
        private Button      _workspaceModeBtn;
        private Button      _collapseInspectorBtn;
        private Button      _collapseRuntimeUiBtn;
        private Label       _renderAreaLabel;
        private VisualElement _authoringToolsRoot;
        private ObjectField   _addActorField;
        private ObjectField   _setBackgroundField;
        private Button        _addActorBtn;
        private Button        _removeSelectedActorBtn;
        private Button        _setBackgroundBtn;
        private Button        _clearBackgroundBtn;

        // ── UI 참조: Stage World ───────────────────────

        private VisualElement _stageWrapper;       // overflow=Hidden, pan/zoom 이벤트 수신
        private VisualElement _stageWorld;         // transform.position/scale 으로 pan/zoom 적용
        private VisualElement _authoringGridLayer;
        private VisualElement _cameraFrameGuide;   // 카메라 뷰 영역 가이드 테두리
        private VisualElement _cameraGizmoLayer;
        private VisualElement _backgroundLayer;    // 배경 레이어 (world 공간)
        private VisualElement _actorLayer;         // 액터 레이어 (world 공간, 프레임 밖 허용)
        private Label         _emptyStageLabel;

        // ── UI 참조: 카메라 프레임 내부 오버레이 ──────

        private VisualElement _renderChoiceArea;
        private VisualElement _renderDialoguePanel;
        private Label         _renderSpeakerLabel;
        private Label         _renderDialogueLabel;

        // ── UI 참조: RuntimePreview 전용 하단 패널 ────

        private VisualElement _dialoguePanel;
        private Label         _speakerLabel;
        private Label         _dialogueLabel;
        private VisualElement _choiceArea;

        // ── UI 참조: 우측 인스펙터 ────────────────────

        private VisualElement _actorListRoot;
        private VisualElement _inspectorRoot;
        private VisualElement _inspectorPanel;
        private VisualElement _inspectorSplitter;
        private bool _previewInspectorCollapsed;
        private bool _previewRuntimeUiCollapsed;
        private float _previewInspectorExpandedWidth = InspectorWidth;
        private bool _isInspectorResizing;
        private float _inspectorResizeStartX;
        private float _inspectorResizeStartWidth;

        // actor → VisualElement 매핑
        private readonly Dictionary<string, VisualElement> _actorElements = new();

        // ── 생명주기 ─────────────────────────────────

        private void CreateGUI()
        {
            LoadPreviewLayoutPrefs();

            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.Add(BuildTopBar());

            var main = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            main.Add(BuildLeftPanel());
            main.Add(BuildInspectorSplitter());
            main.Add(BuildActorInspector());
            root.Add(main);

            RefreshRenderAreaFromGameView();
            ApplyPreviewLayoutVisibility();
            ApplyPreviewModeVisibility();
        }

        private void OnEnable()  => Undo.undoRedoPerformed += OnUndoRedo;
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            SavePreviewLayoutPrefs();
        }
        private void OnFocus()   => RefreshRenderAreaFromGameView();

        private void OnUndoRedo()
        {
            BuildStageStateAt(_currentLine);
            if (_selectionKind == StageSelectionKind.Actor && !_stageState.ContainsKey(_selectedActorKey))
                ClearStageSelection();
            RebuildActorLayer();
            RefreshActorInspector();
            RefreshAuthoringControls();
        }

        private void OnGUI()
        {
            var e = Event.current;
            if (previewMode != PreviewMode.StageAuthoring || e.type != EventType.KeyDown)
                return;

            if (e.keyCode != KeyCode.Delete && e.keyCode != KeyCode.Backspace)
                return;

            if (IsFocusedOnTextInput(rootVisualElement))
                return;

            if (DeleteCurrentStageSelection())
                e.Use();
        }

        // ── 외부 API ─────────────────────────────────

        public void SetEpisode(StoryEpisodeSO ep)
        {
            episode = ep;
            if (_episodeField != null) _episodeField.SetValueWithoutNotify(ep);
            StopPlayback();
            _stageState.Clear();
            _bgState = null;
            RebuildActorLayer();
            RefreshDialogue();
            RefreshButtons();
        }

        public void OnExternalLineSelected(StoryLineSO line)
        {
            _pendingFromLine = line;
            RefreshButtons();
            if (_isPlaying) return;

            if (line != null) { ShowLineSnapshot(line); return; }

            _isLineSample = false;
            _currentLine  = null;
            _stageState.Clear();
            _bgState = null;
            _actorLayer?.Clear();
            _actorElements.Clear();
            ClearStageSelection();
            RefreshBackgroundLayer();
            RefreshActorList();
            SetEmptyStageVisible(true);
            RefreshDialogue();
            RefreshChoices();
            RefreshActorInspector();
        }

        // ── 공용 유틸 (partial 파일 간 공유) ─────────

        internal static Button MakeBtn(string text, Color bg, Action click) =>
            new(click)
            {
                text = text,
                style = { height = 22, paddingLeft = 8, paddingRight = 8, marginRight = 4, backgroundColor = new StyleColor(bg), fontSize = 10 }
            };

        internal static Color ActorPlaceholderColor(CharacterDefinitionSO actor)
        {
            int   hash = (actor.CharacterId ?? actor.name).GetHashCode();
            float h    = (hash & 0xFF) / 255f;
            return Color.HSVToRGB(h, 0.55f, 0.72f);
        }

        internal static void SetElementVisible(VisualElement el, bool visible)
        {
            if (el != null) el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal static Label MakeBoldLabel(string text) =>
            new(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10, marginBottom = 3 } };

        internal static VisualElement MakeSeparator() =>
            new() { style = { height = 1, backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f)), marginTop = 5, marginBottom = 5 } };

        private static bool IsFocusedOnTextInput(VisualElement root)
        {
            var element = root?.focusController?.focusedElement as VisualElement;
            while (element != null)
            {
                if (element is TextField or IntegerField or FloatField or LongField or Vector2Field)
                    return true;

                element = element.parent;
            }

            return false;
        }
    }
}
