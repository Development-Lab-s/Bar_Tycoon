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
            w.minSize = new Vector2(860, 500);
        }

        // ── 상태 ────────────────────────────────────
        private StoryEpisodeSO _episode;
        private StoryLineSO    _selectedLine;
        private string         _saveFolder = "";   // 비어 있으면 에피소드 폴더 사용

        // ── UI 참조 ─────────────────────────────────
        private StoryGraphCanvasView _canvas;
        private Label                _statusLineId;
        private Label                _statusNextId;
        private Label                _statusWarning;
        private Button               _clearNextBtn;

        // ── 진입점 ──────────────────────────────────

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            root.Add(BuildTopBar());

            // 중앙: 캔버스 전체
            _canvas = new StoryGraphCanvasView();
            _canvas.style.flexGrow = 1;
            _canvas.NodeSelected   += OnCanvasNodeSelected;
            _canvas.ConnectionMade += OnConnectionMade;
            root.Add(_canvas);

            root.Add(BuildStatusBar());
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

            // Episode 필드
            var epField = new ObjectField("Episode") { objectType = typeof(StoryEpisodeSO) };
            epField.style.flexGrow   = 1;
            epField.style.marginRight = 10;
            epField.RegisterValueChangedCallback(evt =>
            {
                _episode      = evt.newValue as StoryEpisodeSO;
                _selectedLine = null;
                RebuildCanvas();
                RefreshStatusBar();
            });
            bar.Add(epField);

            // 저장 폴더 표시
            var folderLabel = new Label("저장 폴더:");
            folderLabel.style.fontSize   = 10;
            folderLabel.style.marginRight = 4;
            bar.Add(folderLabel);

            var folderDisplay = new Label("(에피소드 폴더)") { name = "folderDisplay" };
            folderDisplay.style.fontSize        = 10;
            folderDisplay.style.color           = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
            folderDisplay.style.minWidth        = 140;
            folderDisplay.style.overflow        = Overflow.Hidden;
            folderDisplay.style.marginRight     = 4;
            bar.Add(folderDisplay);

            var browseBtn = new Button(() => OnBrowseFolderClicked(folderDisplay)) { text = "…" };
            browseBtn.style.width       = 28;
            browseBtn.style.paddingLeft  = 2;
            browseBtn.style.paddingRight = 2;
            browseBtn.style.marginRight  = 10;
            bar.Add(browseBtn);

            // + Line 버튼
            var addBtn = new Button(OnAddLineClicked) { text = "+ Line" };
            addBtn.style.height      = 22;
            addBtn.style.paddingLeft  = 8;
            addBtn.style.paddingRight = 8;
            bar.Add(addBtn);

            return bar;
        }

        private void OnBrowseFolderClicked(Label display)
        {
            string folder = EditorUtility.OpenFolderPanel(
                "StoryLineSO 저장 폴더 선택",
                "Assets",
                "");

            if (string.IsNullOrEmpty(folder)) return;

            // 절대 경로 → project-relative
            if (folder.StartsWith(Application.dataPath))
                folder = "Assets" + folder[Application.dataPath.Length..];

            _saveFolder  = folder;
            display.text = folder;
            display.tooltip = folder;
        }

        // ── 캔버스 재구성 ────────────────────────────

        private void RebuildCanvas()
        {
            if (_episode == null)
            {
                _canvas.Rebuild(System.Array.Empty<StoryLineSO>());
                return;
            }
            _canvas.Rebuild(_episode.Lines);
        }

        // ── 노드 선택 핸들러 ─────────────────────────

        private void OnCanvasNodeSelected(StoryGraphNodeView node)
        {
            _selectedLine = node?.Line;
            RefreshStatusBar();
        }

        // ── 연결 확정 핸들러 ─────────────────────────

        private void OnConnectionMade(StoryGraphNodeView src, StoryGraphNodeView dst)
        {
            if (src?.Line == null || dst?.Line == null) return;

            string newNextId = dst.Line.LineId;
            if (string.IsNullOrWhiteSpace(newNextId))
            {
                EditorUtility.DisplayDialog(
                    "연결 실패",
                    "대상 노드의 lineId가 비어 있습니다. 먼저 ID를 지정하세요.",
                    "확인");
                return;
            }

            Undo.RecordObject(src.Line, "Set nextLineId");
            StoryEditorUtility.SetNextLineId(src.Line, newNextId);
            src.Refresh();
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
            RefreshStatusBar();
        }

        // ── 하단 상태 바 ─────────────────────────────

        private VisualElement BuildStatusBar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection     = FlexDirection.Row;
            bar.style.alignItems        = Align.Center;
            bar.style.paddingLeft       = 10;
            bar.style.paddingRight      = 10;
            bar.style.paddingTop        = 5;
            bar.style.paddingBottom     = 5;
            bar.style.borderTopWidth    = 1;
            bar.style.borderTopColor    = new StyleColor(new Color(0.1f, 0.1f, 0.1f));
            bar.style.backgroundColor   = new StyleColor(new Color(0.16f, 0.16f, 0.16f));

            _statusLineId = new Label("—");
            _statusLineId.style.fontSize   = 11;
            _statusLineId.style.marginRight = 14;
            _statusLineId.style.unityFontStyleAndWeight = FontStyle.Bold;

            _statusNextId = new Label("");
            _statusNextId.style.fontSize   = 11;
            _statusNextId.style.marginRight = 14;
            _statusNextId.style.color       = new StyleColor(new Color(0.65f, 0.65f, 0.65f));

            _statusWarning = new Label("");
            _statusWarning.style.fontSize   = 11;
            _statusWarning.style.flexGrow   = 1;
            _statusWarning.style.color      = new StyleColor(new Color(1f, 0.6f, 0.2f));

            _clearNextBtn = new Button(OnClearNextClicked) { text = "Next 지우기" };
            _clearNextBtn.style.height      = 20;
            _clearNextBtn.style.paddingLeft  = 8;
            _clearNextBtn.style.paddingRight = 8;

            var deleteBtn = new Button(OnDeleteLineClicked) { text = "Delete Line" };
            deleteBtn.style.height           = 20;
            deleteBtn.style.paddingLeft       = 8;
            deleteBtn.style.paddingRight      = 8;
            deleteBtn.style.marginLeft        = 6;
            deleteBtn.style.backgroundColor  = new StyleColor(new Color(0.55f, 0.15f, 0.15f));

            bar.Add(_statusLineId);
            bar.Add(_statusNextId);
            bar.Add(_statusWarning);
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

            _statusLineId.text = idEmpty ? "⚠ ID 없음" : _selectedLine.LineId;
            _statusNextId.text = nextEmpty ? "→ (종료)" : $"→ {_selectedLine.NextLineId}";

            _clearNextBtn.SetEnabled(!nextEmpty);

            // 경고 조합
            var warnings = new List<string>();
            if (idEmpty) warnings.Add("lineId 없음");
            if (!nextEmpty && _episode != null && !EpisodeContains(_episode, _selectedLine.NextLineId))
                warnings.Add($"\"{_selectedLine.NextLineId}\" 를 에피소드에서 찾을 수 없음");
            if (!nextEmpty && _selectedLine.NextLineId == _selectedLine.LineId)
                warnings.Add("자기 자신 참조 (무한 루프)");

            _statusWarning.text = warnings.Count > 0 ? "⚠ " + string.Join(" / ", warnings) : "";
        }

        // ── 상태 바 버튼 핸들러 ──────────────────────

        private void OnClearNextClicked()
        {
            if (_selectedLine == null) return;
            Undo.RecordObject(_selectedLine, "Clear nextLineId");
            StoryEditorUtility.SetNextLineId(_selectedLine, "");
            _canvas.RefreshAll();
            RefreshStatusBar();
        }

        private void OnDeleteLineClicked()
        {
            if (_selectedLine == null || _episode == null) return;

            bool confirm = EditorUtility.DisplayDialog(
                "Line 삭제",
                $"\"{_selectedLine.LineId}\" 를 삭제합니다.\n이 작업은 되돌릴 수 없습니다 (asset 파일도 삭제됩니다).\n계속하시겠습니까?",
                "삭제", "취소");

            if (!confirm) return;

            StoryEditorUtility.DeleteLine(_episode, _selectedLine);
            _selectedLine = null;
            RebuildCanvas();
            RefreshStatusBar();
        }

        // ── 유틸 ────────────────────────────────────

        private static bool EpisodeContains(StoryEpisodeSO ep, string lineId)
        {
            for (int i = 0; i < ep.Lines.Count; i++)
                if (ep.Lines[i] != null && ep.Lines[i].LineId == lineId)
                    return true;
            return false;
        }
    }
}
