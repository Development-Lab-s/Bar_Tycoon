using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed class StoryGraphEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Story/Story Graph Editor")]
        public static void Open()
        {
            var w = GetWindow<StoryGraphEditorWindow>("Story Graph Editor");
            w.minSize = new Vector2(1000, 500);
        }

        // ── 상태 ────────────────────────────────────
        private StoryEpisodeSO _episode;
        private StoryLineSO    _selectedLine;
        private string         _saveFolder = "";

        // ── UI 참조 ─────────────────────────────────
        private StoryGraphCanvasView     _canvas;
        private StoryGraphInspectorPanel _inspectorPanel;
        private Label                    _statusLineId;
        private Label                    _statusNextId;
        private Label                    _statusWarning;
        private Button                   _clearNextBtn;
        private Label                    _folderDisplay;   // 상단바 폴더 표시

        // ── 생명주기 ─────────────────────────────────

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            // 뷰 상태 저장
            var key = EpisodePrefsKey;
            if (key != null) _canvas?.SaveViewState(key);
        }

        /// <summary>에피소드 GUID 기반 EditorPrefs 키. 에피소드 없으면 null.</summary>
        private string EpisodePrefsKey => _episode != null
            ? "StoryGraph_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_episode))
            : null;

        private void OnUndoRedoPerformed()
        {
            _canvas?.RefreshAll();
            _canvas?.RefreshNodePositions();
            _inspectorPanel?.Reload();
            RefreshStatusBar();
        }

        // ── 진입점 ──────────────────────────────────

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            root.Add(BuildTopBar());

            var center = new VisualElement();
            center.style.flexDirection = FlexDirection.Row;
            center.style.flexGrow      = 1;

            _canvas = new StoryGraphCanvasView();
            _canvas.style.flexGrow   = 1;
            _canvas.NodeSelected     += OnCanvasNodeSelected;
            _canvas.ConnectionMade   += OnConnectionMade;
            _canvas.NodeDisconnected += OnNodeDisconnected;
            center.Add(_canvas);

            _inspectorPanel = new StoryGraphInspectorPanel();
            _inspectorPanel.LineChanged += OnInspectorLineChanged;
            center.Add(_inspectorPanel);

            root.Add(center);
            root.Add(BuildStatusBar());

        }

        // ── 키보드 + Zoom (IMGUI 방식 – UIToolkit보다 신뢰성 높음) ──

        private void OnGUI()
        {
            var e = Event.current;

            if (e.type != EventType.KeyDown) return;
            if (e.keyCode != KeyCode.Delete && e.keyCode != KeyCode.Backspace) return;
            if (StoryGraphInspectorPanel.IsFocusedOnTextField(rootVisualElement)) return;

            // 1. edge 선택 → 연결 해제
            var edgeSrc = _canvas?.SelectedEdgeSrc;
            if (edgeSrc != null)
            {
                Undo.RecordObject(edgeSrc.Line, "Disconnect Edge");
                StoryEditorUtility.SetNextLineId(edgeSrc.Line, "");
                _canvas.ClearSelectedEdge();
                _canvas.RefreshAll();
                if (_selectedLine == edgeSrc.Line) _inspectorPanel?.Reload();
                RefreshStatusBar();
                e.Use();
                return;
            }

            // 2. node 선택 → nextLineId 해제
            OnDeleteConnectionClicked();
            e.Use();
        }

        private void OnDeleteConnectionClicked()
        {
            if (_selectedLine == null) return;
            if (string.IsNullOrWhiteSpace(_selectedLine.NextLineId)) return;

            Undo.RecordObject(_selectedLine, "Disconnect nextLineId");
            StoryEditorUtility.SetNextLineId(_selectedLine, "");
            _canvas.RefreshAll();
            _inspectorPanel.Reload();
            RefreshStatusBar();
        }

        // ── 상단 바 ──────────────────────────────────

        private VisualElement BuildTopBar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection     = FlexDirection.Row;
            bar.style.alignItems        = Align.Center;
            bar.style.paddingLeft       = 8;
            bar.style.paddingRight      = 8;
            bar.style.paddingTop        = 6;
            bar.style.paddingBottom     = 6;
            bar.style.borderBottomWidth = 1;
            bar.style.borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));

            var epField = new ObjectField("Episode") { objectType = typeof(StoryEpisodeSO) };
            epField.style.flexGrow    = 1;
            epField.style.marginRight = 10;
            epField.RegisterValueChangedCallback(evt =>
            {
                // 기존 에피소드 뷰 저장
                var oldKey = EpisodePrefsKey;
                if (oldKey != null) _canvas?.SaveViewState(oldKey);

                _episode      = evt.newValue as StoryEpisodeSO;
                _selectedLine = null;
                _inspectorPanel.SetLine(null, _episode);
                LoadFolderFromEpisode();
                RebuildCanvas();
                RefreshStatusBar();

                // 새 에피소드 뷰 로드
                var newKey = EpisodePrefsKey;
                if (newKey != null) _canvas?.LoadViewState(newKey);
            });
            bar.Add(epField);

            var folderLabel = new Label("저장 폴더:");
            folderLabel.style.fontSize   = 10;
            folderLabel.style.marginRight = 4;
            bar.Add(folderLabel);

            _folderDisplay = new Label("(에피소드 폴더)") { name = "folderDisplay" };
            _folderDisplay.style.fontSize    = 10;
            _folderDisplay.style.color       = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
            _folderDisplay.style.minWidth    = 140;
            _folderDisplay.style.overflow    = Overflow.Hidden;
            _folderDisplay.style.marginRight = 4;
            bar.Add(_folderDisplay);

            var browseBtn = new Button(OnBrowseFolderClicked) { text = "…" };
            browseBtn.style.width        = 28;
            browseBtn.style.paddingLeft  = 2;
            browseBtn.style.paddingRight = 2;
            browseBtn.style.marginRight  = 10;
            bar.Add(browseBtn);

            var addBtn = new Button(OnAddLineClicked) { text = "+ Line" };
            addBtn.style.height       = 22;
            addBtn.style.paddingLeft  = 8;
            addBtn.style.paddingRight = 8;
            bar.Add(addBtn);

            return bar;
        }

        // ── 저장 폴더 퍼시스턴스 ────────────────────

        private void LoadFolderFromEpisode()
        {
            if (_episode == null)
            {
                _saveFolder          = "";
                _folderDisplay.text  = "(에피소드 폴더)";
                _folderDisplay.tooltip = "";
                return;
            }

            string stored = _episode.EditorLineSaveFolder;
            if (!string.IsNullOrEmpty(stored))
            {
                _saveFolder            = stored;
                _folderDisplay.text    = stored;
                _folderDisplay.tooltip = stored;
            }
            else
            {
                _saveFolder          = "";
                _folderDisplay.text  = "(에피소드 폴더)";
                _folderDisplay.tooltip = "";
            }
        }

        private void OnBrowseFolderClicked()
        {
            string folder = EditorUtility.OpenFolderPanel("StoryLineSO 저장 폴더 선택", "Assets", "");
            if (string.IsNullOrEmpty(folder)) return;

            if (folder.StartsWith(Application.dataPath))
                folder = "Assets" + folder[Application.dataPath.Length..];

            _saveFolder              = folder;
            _folderDisplay.text      = folder;
            _folderDisplay.tooltip   = folder;

            // 에피소드에 저장 (Unity 재시작 후에도 유지)
            if (_episode != null)
                StoryEditorUtility.SetEditorLineSaveFolder(_episode, folder);
        }

        // ── 캔버스 재구성 ────────────────────────────

        private void RebuildCanvas()
        {
            _canvas.Rebuild(_episode != null ? _episode.Lines : System.Array.Empty<StoryLineSO>());
        }

        // ── 노드 선택 핸들러 ─────────────────────────

        private void OnCanvasNodeSelected(StoryGraphNodeView node)
        {
            _selectedLine = node?.Line;
            _inspectorPanel.SetLine(_selectedLine, _episode);
            RefreshStatusBar();
        }

        // ── 연결 확정 핸들러 ─────────────────────────

        private void OnConnectionMade(StoryGraphNodeView src, StoryGraphNodeView dst)
        {
            if (src?.Line == null || dst?.Line == null) return;

            string newNextId = dst.Line.LineId;
            if (string.IsNullOrWhiteSpace(newNextId))
            {
                EditorUtility.DisplayDialog("연결 실패", "대상 노드의 lineId가 비어 있습니다.", "확인");
                return;
            }

            Undo.RecordObject(src.Line, "Set nextLineId");
            StoryEditorUtility.SetNextLineId(src.Line, newNextId);
            src.Refresh();
            _canvas.RefreshAll();
            if (_selectedLine == src.Line) _inspectorPanel.Reload();
            RefreshStatusBar();
        }

        private void OnNodeDisconnected(StoryGraphNodeView node)
        {
            if (_selectedLine == node.Line) _inspectorPanel.Reload();
            RefreshStatusBar();
        }

        private void OnInspectorLineChanged()
        {
            _canvas.RefreshAll();
            RefreshStatusBar();
        }

        // ── + Line 액션 ──────────────────────────────

        private void OnAddLineClicked()
        {
            if (_episode == null)
            {
                EditorUtility.DisplayDialog("Episode 없음", "상단에서 StoryEpisodeSO를 먼저 선택하세요.", "확인");
                return;
            }

            string folder = string.IsNullOrEmpty(_saveFolder) ? null : _saveFolder;
            var newLine   = StoryEditorUtility.CreateAndAddLine(_episode, folder);

            RebuildCanvas();
            _canvas.SelectNode(newLine);
            _selectedLine = newLine;
            _inspectorPanel.SetLine(newLine, _episode);
            RefreshStatusBar();
        }

        // ── 하단 상태 바 ─────────────────────────────

        private VisualElement BuildStatusBar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection   = FlexDirection.Row;
            bar.style.alignItems      = Align.Center;
            bar.style.paddingLeft     = 10;
            bar.style.paddingRight    = 10;
            bar.style.paddingTop      = 5;
            bar.style.paddingBottom   = 5;
            bar.style.borderTopWidth  = 1;
            bar.style.borderTopColor  = new StyleColor(new Color(0.1f, 0.1f, 0.1f));
            bar.style.backgroundColor = new StyleColor(new Color(0.16f, 0.16f, 0.16f));

            _statusLineId = new Label("—");
            _statusLineId.style.fontSize                = 11;
            _statusLineId.style.marginRight             = 14;
            _statusLineId.style.unityFontStyleAndWeight = FontStyle.Bold;

            _statusNextId = new Label("");
            _statusNextId.style.fontSize   = 11;
            _statusNextId.style.marginRight = 14;
            _statusNextId.style.color       = new StyleColor(new Color(0.65f, 0.65f, 0.65f));

            _statusWarning = new Label("");
            _statusWarning.style.fontSize = 11;
            _statusWarning.style.flexGrow = 1;
            _statusWarning.style.color    = new StyleColor(new Color(1f, 0.6f, 0.2f));

            var hintLbl = new Label("[Del] 연결 해제  [Edge 클릭+Del]");
            hintLbl.style.fontSize   = 10;
            hintLbl.style.color      = new StyleColor(new Color(0.45f, 0.45f, 0.45f));
            hintLbl.style.marginRight = 10;

            _clearNextBtn = new Button(OnClearNextClicked) { text = "Next 지우기" };
            _clearNextBtn.style.height       = 20;
            _clearNextBtn.style.paddingLeft  = 8;
            _clearNextBtn.style.paddingRight = 8;

            var deleteBtn = new Button(OnDeleteLineClicked) { text = "Delete Line" };
            deleteBtn.style.height          = 20;
            deleteBtn.style.paddingLeft     = 8;
            deleteBtn.style.paddingRight    = 8;
            deleteBtn.style.marginLeft      = 6;
            deleteBtn.style.backgroundColor = new StyleColor(new Color(0.55f, 0.15f, 0.15f));

            bar.Add(_statusLineId);
            bar.Add(_statusNextId);
            bar.Add(_statusWarning);
            bar.Add(hintLbl);
            bar.Add(_clearNextBtn);
            bar.Add(deleteBtn);

            return bar;
        }

        private void RefreshStatusBar()
        {
            if (_selectedLine == null)
            {
                _statusLineId.text  = "—";
                _statusNextId.text  = "";
                _statusWarning.text = "";
                _clearNextBtn.SetEnabled(false);
                return;
            }

            bool idEmpty   = string.IsNullOrWhiteSpace(_selectedLine.LineId);
            bool nextEmpty = string.IsNullOrWhiteSpace(_selectedLine.NextLineId);

            _statusLineId.text = idEmpty   ? "⚠ ID 없음" : _selectedLine.LineId;
            _statusNextId.text = nextEmpty ? "→ (종료)"  : $"→ {_selectedLine.NextLineId}";
            _clearNextBtn.SetEnabled(!nextEmpty);

            var warnings = new List<string>();
            if (idEmpty) warnings.Add("lineId 없음");
            if (!nextEmpty && _episode != null && !EpisodeContains(_episode, _selectedLine.NextLineId))
                warnings.Add($"\"{_selectedLine.NextLineId}\" 에피소드에 없음");
            if (!nextEmpty && _selectedLine.NextLineId == _selectedLine.LineId)
                warnings.Add("자기 자신 참조");

            _statusWarning.text = warnings.Count > 0 ? "⚠ " + string.Join(" / ", warnings) : "";
        }

        // ── 상태 바 버튼 핸들러 ──────────────────────

        private void OnClearNextClicked()
        {
            if (_selectedLine == null) return;
            Undo.RecordObject(_selectedLine, "Clear nextLineId");
            StoryEditorUtility.SetNextLineId(_selectedLine, "");
            _canvas.RefreshAll();
            _inspectorPanel.Reload();
            RefreshStatusBar();
        }

        private void OnDeleteLineClicked()
        {
            if (_selectedLine == null || _episode == null) return;

            bool confirm = EditorUtility.DisplayDialog(
                "Line 삭제",
                $"\"{_selectedLine.LineId}\" 를 삭제합니다.\nasset 파일도 삭제됩니다. 계속하시겠습니까?",
                "삭제", "취소");
            if (!confirm) return;

            StoryEditorUtility.DeleteLine(_episode, _selectedLine);
            _selectedLine = null;
            _inspectorPanel.SetLine(null, _episode);
            RebuildCanvas();
            RefreshStatusBar();
        }

        // ── 유틸 ────────────────────────────────────

        private static bool EpisodeContains(StoryEpisodeSO ep, string lineId)
        {
            for (int i = 0; i < ep.Lines.Count; i++)
                if (ep.Lines[i] != null && ep.Lines[i].LineId == lineId) return true;
            return false;
        }
    }
}
