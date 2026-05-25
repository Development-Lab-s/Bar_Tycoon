using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed class StoryBulkLineImportWindow : EditorWindow
    {
        public static event Action<StoryEpisodeSO, List<StoryLineSO>> LinesCreated;
        public static event Action<Vector2, int, float, float, int>   PreviewChanged;
        public static event Action                                     PreviewCleared;

        private StoryEpisodeSO _episode;
        private string         _saveFolder;

        private string  _inputText = "";
        private Vector2 _textScrollPos;
        private Vector2 _speakerScrollPos;

        private List<ParsedLine>                        _parsedLines    = new();
        private List<string>                            _uniqueSpeakers = new();
        private Dictionary<string, CharacterDefinitionSO> _speakerBindings = new();
        private bool _hasParsed;

        // 배치 설정
        private float _startX      = 0f;
        private float _startY      = 0f;
        private int   _columnCount = 10;
        private float _xOffset     = 280f;
        private float _yOffset     = 120f;

        private const string NarrationKey         = "나레이션";
        private const string PlayerPlaceholderSrc = "(주인공 이름)";
        private const string PlayerPlaceholderDst = "{player}";

        public static void Open(StoryEpisodeSO episode, string saveFolder, Vector2 canvasStartPos)
        {
            var w = GetWindow<StoryBulkLineImportWindow>(true, "일괄 라인 입력");
            w._episode    = episode;
            w._saveFolder = saveFolder;
            w._startX     = canvasStartPos.x;
            w._startY     = canvasStartPos.y;
            w.minSize     = new Vector2(500, 600);
            w.ShowUtility();
            w.FirePreviewChanged();
        }

        private void OnGUI()
        {
            GUILayout.Label("스크립트 붙여넣기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "형식: 화자: 대사  |  나레이션: 내용\n() 연출 노트 자동 제거  |  (주인공 이름) → {player}",
                MessageType.None);

            EditorGUILayout.Space(4);

            _textScrollPos = GUILayout.BeginScrollView(_textScrollPos, GUILayout.Height(180));
            _inputText     = GUILayout.TextArea(_inputText, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("분석", GUILayout.Height(24)))
                Analyze();

            if (!_hasParsed)
                return;

            EditorGUILayout.Space(8);
            DrawSpeakerBindings();
            EditorGUILayout.Space(8);
            DrawGridSettings();
            EditorGUILayout.Space(8);
            DrawCreateBar();
        }

        private void DrawSpeakerBindings()
        {
            GUILayout.Label($"감지된 화자 ({_uniqueSpeakers.Count}명)", EditorStyles.boldLabel);

            _speakerScrollPos = GUILayout.BeginScrollView(_speakerScrollPos, GUILayout.MaxHeight(150));
            foreach (var name in _uniqueSpeakers)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(name, GUILayout.Width(130));
                if (name == NarrationKey)
                {
                    GUILayout.Label("→ 나레이션 (자동)", EditorStyles.miniLabel);
                }
                else
                {
                    _speakerBindings[name] = (CharacterDefinitionSO)EditorGUILayout.ObjectField(
                        _speakerBindings.GetValueOrDefault(name),
                        typeof(CharacterDefinitionSO), false);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            var unbound = _uniqueSpeakers
                .Where(n => n != NarrationKey &&
                            (!_speakerBindings.TryGetValue(n, out var b) || b == null))
                .ToList();
            if (unbound.Count > 0)
                EditorGUILayout.HelpBox(
                    $"SO 미연결 → nameOverride로 생성됩니다: {string.Join(", ", unbound)}",
                    MessageType.Info);
        }

        private void DrawGridSettings()
        {
            GUILayout.Label("배치 설정", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();
            GUILayout.Label("시작 X", GUILayout.Width(50));
            _startX = EditorGUILayout.FloatField(_startX, GUILayout.Width(65));
            GUILayout.Space(8);
            GUILayout.Label("시작 Y", GUILayout.Width(50));
            _startY = EditorGUILayout.FloatField(_startY, GUILayout.Width(65));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("열 개수", GUILayout.Width(50));
            _columnCount = EditorGUILayout.IntField(_columnCount, GUILayout.Width(45));
            GUILayout.Space(8);
            GUILayout.Label("X 간격", GUILayout.Width(50));
            _xOffset = EditorGUILayout.FloatField(_xOffset, GUILayout.Width(65));
            GUILayout.Space(8);
            GUILayout.Label("Y 간격", GUILayout.Width(50));
            _yOffset = EditorGUILayout.FloatField(_yOffset, GUILayout.Width(65));
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                FirePreviewChanged();
        }

        private void FirePreviewChanged() =>
            PreviewChanged?.Invoke(new Vector2(_startX, _startY), _columnCount, _xOffset, _yOffset, _parsedLines.Count);

        private void OnDestroy() => PreviewCleared?.Invoke();

        private void DrawCreateBar()
        {
            int count = _parsedLines.Count;
            int cols  = Mathf.Max(1, _columnCount);
            int rows  = count == 0 ? 0 : (count - 1) / cols + 1;
            GUILayout.Label(
                $"생성 예정: {count}개 라인  ({rows}행 × {Mathf.Min(count, cols)}열)",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUI.enabled = count > 0 && _episode != null;
            if (GUILayout.Button("생성", GUILayout.Height(28)))
                CreateLines();
            GUI.enabled = true;
            if (GUILayout.Button("취소", GUILayout.Height(28)))
                Close();
            GUILayout.EndHorizontal();
        }

        // ── 분석 ─────────────────────────────────────

        private void Analyze()
        {
            _parsedLines    = ParseScript(_inputText);
            _uniqueSpeakers = _parsedLines.Select(p => p.SpeakerName).Distinct().ToList();

            // 기존 바인딩 보존
            var kept = new Dictionary<string, CharacterDefinitionSO>();
            foreach (var name in _uniqueSpeakers)
                if (name != NarrationKey && _speakerBindings.TryGetValue(name, out var prev))
                    kept[name] = prev;

            _speakerBindings = kept;
            _hasParsed       = true;
            FirePreviewChanged();
        }

        // ── 생성 ─────────────────────────────────────

        private void CreateLines()
        {
            if (_episode == null || _parsedLines.Count == 0)
                return;

            string folder = string.IsNullOrEmpty(_saveFolder) ? null : _saveFolder;
            int    cols   = Mathf.Max(1, _columnCount);

            var createdLines = new List<StoryLineSO>(_parsedLines.Count);

            for (int i = 0; i < _parsedLines.Count; i++)
            {
                var p       = _parsedLines[i];
                var newLine = StoryEditorUtility.CreateAndAddLine(_episode, folder);

                if (p.SpeakerName != NarrationKey)
                {
                    if (_speakerBindings.TryGetValue(p.SpeakerName, out var charDef) && charDef != null)
                        StoryEditorUtility.SetSpeaker(newLine, charDef);
                    else
                        StoryEditorUtility.SetNameOverride(newLine, p.SpeakerName);
                }

                StoryEditorUtility.SetDialogueText(newLine, p.DialogueText);

                int col = i % cols;
                int row = i / cols;
                StoryEditorUtility.SetEditorNodePosition(
                    newLine,
                    new Vector2(_startX + col * _xOffset, _startY + row * _yOffset));

                createdLines.Add(newLine);
            }

            // 순서대로 nextLineId 자동 연결 (마지막 라인 제외)
            for (int i = 0; i < createdLines.Count - 1; i++)
                StoryEditorUtility.SetNextLineId(createdLines[i], createdLines[i + 1].LineId);

            AssetDatabase.SaveAssets();
            LinesCreated?.Invoke(_episode, createdLines);
            Close();
        }

        // ── 파싱 ─────────────────────────────────────

        private static List<ParsedLine> ParseScript(string input)
        {
            var result = new List<ParsedLine>();
            if (string.IsNullOrWhiteSpace(input))
                return result;

            foreach (var rawLine in input.Split('\n'))
            {
                var trimmed = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // 순수 연출 노트: 줄 전체가 (...) 인 경우 스킵
                if (Regex.IsMatch(trimmed, @"^\([^)]*\)$"))
                    continue;

                // 화자: 대사 분리 (일반/전각 콜론 모두 허용)
                int colonIdx = trimmed.IndexOf(':');
                if (colonIdx < 0) colonIdx = trimmed.IndexOf('：');
                if (colonIdx <= 0)
                    continue;

                string speakerName = trimmed[..colonIdx].Trim();
                string dialogue    = trimmed[(colonIdx + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(speakerName))
                    continue;

                // (주인공 이름) → {player} 먼저 치환
                dialogue = dialogue.Replace(PlayerPlaceholderSrc, PlayerPlaceholderDst);

                // 나머지 () 연출 노트 제거
                dialogue = Regex.Replace(dialogue, @"\([^)]*\)", "");
                dialogue = Regex.Replace(dialogue, @"\s{2,}", " ").Trim();

                if (string.IsNullOrWhiteSpace(dialogue))
                    continue;

                result.Add(new ParsedLine(speakerName, dialogue));
            }

            return result;
        }

        private readonly struct ParsedLine
        {
            public readonly string SpeakerName;
            public readonly string DialogueText;

            public ParsedLine(string speaker, string dialogue)
            {
                SpeakerName  = speaker;
                DialogueText = dialogue;
            }
        }
    }
}
