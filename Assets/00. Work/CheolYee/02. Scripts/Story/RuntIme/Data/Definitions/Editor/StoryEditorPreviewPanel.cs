using System;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 그래프 에디터 하단에 붙는 에피소드 프리뷰 패널.
    /// 실제 런타임(UniTask/MonoBehaviour) 없이 StoryLineSO 데이터를 직접 순회한다.
    /// 재생 결과는 실제 authoring 데이터 기반이며, 런타임과 동일한 source-of-truth를 사용한다.
    /// </summary>
    public sealed class StoryEditorPreviewPanel : VisualElement
    {
        // ── 이벤트 ───────────────────────────────────
        /// <summary>프리뷰 재생 라인이 바뀔 때. null = 정지 상태.</summary>
        public event Action<string> PreviewLineChanged;

        // ── 상태 ────────────────────────────────────
        private StoryEpisodeSO _episode;
        private StoryLineSO    _currentLine;
        private string         _pendingFromLineId;   // "From Here" 재생 기준
        private bool           _isPlaying;

        // ── UI 참조 ─────────────────────────────────
        private readonly Button       _playBtn;
        private readonly Button       _fromHereBtn;
        private readonly Button       _stopBtn;
        private readonly Button       _nextBtn;
        private readonly Label        _statusLabel;
        private readonly Label        _lineIdLabel;
        private readonly Label        _speakerLabel;
        private readonly Label        _dialogueLabel;
        private readonly Label        _modulesLabel;
        private readonly VisualElement _choiceArea;
        private readonly Label        _hintLabel;

        public StoryEditorPreviewPanel()
        {
            style.flexDirection      = FlexDirection.Column;
            style.backgroundColor   = new StyleColor(new Color(0.13f, 0.13f, 0.13f));
            style.borderTopWidth    = 1f;
            style.borderTopColor    = new StyleColor(new Color(0.08f, 0.08f, 0.08f));
            style.paddingLeft       = 10f;
            style.paddingRight      = 10f;
            style.paddingTop        = 6f;
            style.paddingBottom     = 6f;

            // ── 컨트롤 바 ─────────────────────────────
            var controlBar = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 5 } };

            var previewLabel = new Label("Preview") { style = { fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 10, color = new StyleColor(new Color(0.8f, 0.8f, 0.8f)) } };
            controlBar.Add(previewLabel);

            _playBtn = MakeButton("▶ Play", new Color(0.2f, 0.55f, 0.2f), OnPlayClicked);
            _fromHereBtn = MakeButton("▶ From Here", new Color(0.2f, 0.4f, 0.55f), OnFromHereClicked);
            _stopBtn = MakeButton("■ Stop", new Color(0.5f, 0.2f, 0.2f), OnStopClicked);
            _nextBtn = MakeButton("▷ Next", new Color(0.35f, 0.35f, 0.35f), OnNextClicked);

            controlBar.Add(_playBtn);
            controlBar.Add(_fromHereBtn);
            controlBar.Add(_stopBtn);
            controlBar.Add(_nextBtn);

            _statusLabel = new Label("정지")
            {
                style =
                {
                    marginLeft = 12,
                    fontSize = 10,
                    color = new StyleColor(new Color(0.55f, 0.55f, 0.55f))
                }
            };
            controlBar.Add(_statusLabel);

            Add(controlBar);

            // ── 라인 표시 영역 ────────────────────────
            var contentRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };

            var left = new VisualElement { style = { flexGrow = 1, marginRight = 8 } };

            var lineRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            _lineIdLabel = new Label("—") { style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.75f, 1f)), marginRight = 8 } };
            _speakerLabel = new Label("") { style = { fontSize = 10, color = new StyleColor(new Color(0.75f, 0.75f, 0.75f)) } };
            lineRow.Add(_lineIdLabel);
            lineRow.Add(_speakerLabel);
            left.Add(lineRow);

            _dialogueLabel = new Label("")
            {
                style =
                {
                    fontSize = 11,
                    whiteSpace = WhiteSpace.Normal,
                    color = new StyleColor(new Color(0.92f, 0.92f, 0.92f)),
                    marginBottom = 4,
                    flexGrow = 1
                }
            };
            left.Add(_dialogueLabel);

            _modulesLabel = new Label("")
            {
                style =
                {
                    fontSize = 9,
                    color = new StyleColor(new Color(0.55f, 0.75f, 0.55f)),
                    whiteSpace = WhiteSpace.Normal
                }
            };
            left.Add(_modulesLabel);

            contentRow.Add(left);

            _choiceArea = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    minWidth = 150f,
                    maxWidth = 280f,
                    alignSelf = Align.Center
                }
            };
            contentRow.Add(_choiceArea);

            Add(contentRow);

            _hintLabel = new Label("[Space] 다음  |  그래프에서 노드를 선택 후 'From Here'로 선택 라인부터 재생")
            {
                style =
                {
                    fontSize = 9,
                    color = new StyleColor(new Color(0.38f, 0.38f, 0.38f)),
                    marginTop = 4
                }
            };
            Add(_hintLabel);

            RefreshButtons();
            RefreshContent();
        }

        // ── 외부 API ─────────────────────────────────

        public void SetEpisode(StoryEpisodeSO episode)
        {
            if (_episode == episode) return;
            Stop();
            _episode = episode;
            RefreshButtons();
        }

        /// <summary>캔버스 선택 노드가 바뀌면 EditorWindow에서 호출. "From Here" 대상 업데이트.</summary>
        public void NotifySelectionChanged(string selectedLineId)
        {
            _pendingFromLineId = selectedLineId;
            RefreshButtons();
        }

        // ── 버튼 핸들러 ──────────────────────────────

        private void OnPlayClicked()
        {
            if (_episode == null) return;
            string startId = _episode.EntryLineId;
            if (string.IsNullOrWhiteSpace(startId) && _episode.Lines.Count > 0)
                startId = _episode.Lines[0]?.LineId;
            StartFrom(startId);
        }

        private void OnFromHereClicked()
        {
            if (_episode == null) return;
            StartFrom(_pendingFromLineId);
        }

        private void OnStopClicked() => Stop();

        private void OnNextClicked()
        {
            if (!_isPlaying || _currentLine == null) return;
            string nextId = _currentLine.NextLineId;
            if (string.IsNullOrWhiteSpace(nextId))
                Stop();
            else
                AdvanceTo(nextId);
        }

        // ── 재생 흐름 ─────────────────────────────────

        private void StartFrom(string lineId)
        {
            if (_episode == null) return;
            _isPlaying = true;

            if (!string.IsNullOrWhiteSpace(lineId) && _episode.TryGetLine(lineId, out var line))
                ShowLine(line);
            else if (_episode.Lines.Count > 0)
                ShowLine(_episode.Lines[0]);
            else
                Stop();

            RefreshButtons();
        }

        private void AdvanceTo(string lineId)
        {
            if (_episode == null || string.IsNullOrWhiteSpace(lineId)) { Stop(); return; }
            if (_episode.TryGetLine(lineId, out var line))
                ShowLine(line);
            else
                Stop();
        }

        private void ShowLine(StoryLineSO line)
        {
            _currentLine = line;
            RefreshContent();
            RefreshButtons();
            PreviewLineChanged?.Invoke(line?.LineId);
        }

        private void Stop()
        {
            _isPlaying   = false;
            _currentLine = null;
            RefreshContent();
            RefreshButtons();
            PreviewLineChanged?.Invoke(null);
        }

        private void OnChoiceSelected(string reactionStartLineId)
        {
            if (string.IsNullOrWhiteSpace(reactionStartLineId))
                Stop();
            else
                AdvanceTo(reactionStartLineId);
        }

        // ── UI 갱신 ──────────────────────────────────

        private void RefreshContent()
        {
            _choiceArea.Clear();

            if (_currentLine == null)
            {
                _lineIdLabel.text    = "—";
                _speakerLabel.text   = "";
                _dialogueLabel.text  = _isPlaying ? "(라인 없음)" : "재생 중이 아닙니다.";
                _modulesLabel.text   = "";
                _statusLabel.text    = _isPlaying ? "재생 중" : "정지";
                return;
            }

            _lineIdLabel.text  = _currentLine.LineId ?? "—";
            _speakerLabel.text = _currentLine.GetResolvedSpeakerName() is { Length: > 0 } n ? $"({n})" : "(내레이션)";
            _dialogueLabel.text = _currentLine.DialogueText ?? "";
            _statusLabel.text   = "재생 중";

            // 모듈 요약
            var modDesc = new System.Text.StringBuilder();
            IStoryChoiceLikeModule choiceLike = null;
            foreach (var m in _currentLine.Modules)
            {
                if (m == null) continue;
                if (m is IStoryChoiceLikeModule cl) { choiceLike = cl; continue; }
                if (modDesc.Length > 0) modDesc.Append("  |  ");
                modDesc.Append(m.DisplayName);
                AppendModuleDetail(modDesc, m);
            }
            _modulesLabel.text = modDesc.Length > 0 ? $"Modules: {modDesc}" : "";

            // 선택지
            if (choiceLike != null)
                BuildChoiceButtons(choiceLike);
        }

        private static void AppendModuleDetail(System.Text.StringBuilder sb, StoryModuleSO m)
        {
            if (m is StoryCharacterEnterModuleSO enter)
                sb.Append($"({enter.Character?.DisplayName ?? "?"}, {enter.Slot}, {enter.Motion})");
        }

        private void BuildChoiceButtons(IStoryChoiceLikeModule choiceLike)
        {
            var header = new Label("선택지:")
            {
                style =
                {
                    fontSize = 9,
                    color = new StyleColor(new Color(1f, 0.78f, 0.3f)),
                    marginBottom = 3
                }
            };
            _choiceArea.Add(header);

            foreach (var opt in choiceLike.Options)
            {
                string reactionId = opt.ReactionStartLineId;
                var btn = new Button(() => OnChoiceSelected(reactionId))
                {
                    text = opt.Text,
                    style =
                    {
                        marginBottom = 2,
                        whiteSpace = WhiteSpace.Normal,
                        fontSize = 10
                    }
                };
                _choiceArea.Add(btn);
            }
        }

        private void RefreshButtons()
        {
            bool hasEpisode  = _episode != null;
            bool hasFromHere = hasEpisode && !string.IsNullOrWhiteSpace(_pendingFromLineId);
            bool hasNext     = _isPlaying && _currentLine != null
                               && !string.IsNullOrWhiteSpace(_currentLine.NextLineId);

            _playBtn.SetEnabled(hasEpisode);
            _fromHereBtn.SetEnabled(hasFromHere);
            _stopBtn.SetEnabled(_isPlaying);
            _nextBtn.SetEnabled(hasNext);
        }

        // ── 헬퍼 ────────────────────────────────────

        private static Button MakeButton(string text, Color bg, Action click)
        {
            var btn = new Button(click)
            {
                text = text,
                style =
                {
                    height = 22,
                    paddingLeft = 8,
                    paddingRight = 8,
                    marginRight = 4,
                    backgroundColor = new StyleColor(bg),
                    fontSize = 10
                }
            };
            return btn;
        }
    }
}
