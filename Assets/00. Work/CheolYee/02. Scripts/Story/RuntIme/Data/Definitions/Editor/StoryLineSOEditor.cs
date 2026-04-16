using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    [CustomEditor(typeof(StoryLineSO))]
    public sealed class StoryLineSOEditor : UnityEditor.Editor
    {
        private StoryEpisodeSO _episode;

        private bool _idFoldout   = true;
        private bool _nextFoldout = true;
        private string _idSuffix  = "";

        // ── 진입점 ──────────────────────────────────

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var line    = (StoryLineSO)target;
            _episode    = FindEpisode(line);

            EditorGUILayout.Space(8);
            DrawLineIdHelper(line);

            EditorGUILayout.Space(4);
            DrawNextLineHelper(line);
        }

        // ══════════════════════════════════════════
        // Section 1 — Line ID Helper
        // ══════════════════════════════════════════

        private void DrawLineIdHelper(StoryLineSO line)
        {
            _idFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_idFoldout, "── Line ID Helper ──");
            if (!_idFoldout) { EditorGUILayout.EndFoldoutHeaderGroup(); return; }

            var idProp = serializedObject.FindProperty("lineId");

            // ── 상태 경고 ──────────────────────────
            DrawLineIdWarnings(line);

            EditorGUILayout.Space(2);

            // ── 소속 에피소드 (읽기 전용 표시) ───────
            if (_episode != null)
            {
                int myIdx = FindIndexInEpisode(line, _episode);
                string episodeInfo = myIdx >= 0
                    ? $"{_episode.name}  (순서 #{myIdx + 1})"
                    : _episode.name;

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("소속 에피소드", _episode, typeof(StoryEpisodeSO), false);

                EditorGUILayout.LabelField("에피소드 내 순서", myIdx >= 0 ? $"#{myIdx + 1}" : "미포함", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("소속 에피소드를 찾을 수 없습니다. 에피소드 Lines 목록에 추가하면 번호 기반 생성이 가능합니다.", MessageType.None);
            }

            EditorGUILayout.Space(4);

            // ── Suffix 입력 ────────────────────────
            _idSuffix = EditorGUILayout.TextField("Suffix (선택)", _idSuffix);
            EditorGUILayout.LabelField("", $"예: {PreviewGeneratedId(line)}",  EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            // ── 버튼 행 ────────────────────────────
            EditorGUILayout.BeginHorizontal();

            bool idIsEmpty = string.IsNullOrWhiteSpace(idProp.stringValue);

            // 생성 버튼: lineId가 비어 있을 때만 활성
            using (new EditorGUI.DisabledScope(!idIsEmpty && false)) // always enabled, color로 구분
            {
                string genLabel = idIsEmpty ? "ID 생성" : "ID 재생성";
                Color prevColor = GUI.backgroundColor;
                GUI.backgroundColor = idIsEmpty ? new Color(0.5f, 0.9f, 0.5f) : new Color(1f, 0.75f, 0.4f);

                if (GUILayout.Button(genLabel))
                    TryGenerateLineId(line, idProp, forceOverwrite: !idIsEmpty);

                GUI.backgroundColor = prevColor;
            }

            // 복사 버튼
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(idProp.stringValue)))
            {
                if (GUILayout.Button("ID 복사", GUILayout.Width(70)))
                    GUIUtility.systemCopyBuffer = idProp.stringValue;
            }

            EditorGUILayout.EndHorizontal();

            // lineId가 이미 있을 때 재생성 경고
            if (!idIsEmpty)
            {
                EditorGUILayout.HelpBox(
                    $"현재 lineId \"{idProp.stringValue}\" 를 참조하는 다른 라인이 있을 수 있습니다.\n재생성하면 해당 참조가 끊어집니다.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void TryGenerateLineId(StoryLineSO line, SerializedProperty idProp, bool forceOverwrite)
        {
            string generated = GenerateLineId(line);
            if (string.IsNullOrEmpty(generated))
            {
                EditorUtility.DisplayDialog("ID 생성 실패", "소속 에피소드를 찾을 수 없어 번호를 결정할 수 없습니다.\n에피소드 Lines 목록에 이 라인을 추가하세요.", "확인");
                return;
            }

            if (!forceOverwrite || EditorUtility.DisplayDialog(
                    "ID 재생성 확인",
                    $"현재 ID \"{idProp.stringValue}\" 를 \"{generated}\" 로 변경합니다.\n기존 ID를 참조하는 nextLineId 들은 수동으로 수정해야 합니다.\n계속하시겠습니까?",
                    "변경", "취소"))
            {
                serializedObject.Update();
                Undo.RecordObject(target, "Generate lineId");
                idProp.stringValue = generated;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        private string GenerateLineId(StoryLineSO line)
        {
            if (_episode == null) return null;

            int idx = FindIndexInEpisode(line, _episode);
            if (idx < 0) return null;

            int number = idx + 1;
            string baseId = $"line_{number:D3}";
            string suffix = _idSuffix?.Trim();

            return string.IsNullOrEmpty(suffix) ? baseId : $"{baseId}_{suffix}";
        }

        private string PreviewGeneratedId(StoryLineSO line)
        {
            if (_episode == null) return "에피소드 필요";
            int idx = FindIndexInEpisode(line, _episode);
            if (idx < 0) return "에피소드 Lines에 추가 필요";

            int number = idx + 1;
            string baseId = $"line_{number:D3}";
            string suffix = _idSuffix?.Trim();
            return string.IsNullOrEmpty(suffix) ? baseId : $"{baseId}_{suffix}";
        }

        private void DrawLineIdWarnings(StoryLineSO line)
        {
            var idProp = serializedObject.FindProperty("lineId");

            // lineId 비어 있음
            if (string.IsNullOrWhiteSpace(idProp.stringValue))
            {
                EditorGUILayout.HelpBox("lineId가 비어 있습니다. 아래 버튼으로 생성하세요.", MessageType.Warning);
                return;
            }

            // lineId 중복
            if (_episode != null)
            {
                int dup = 0;
                for (int i = 0; i < _episode.Lines.Count; i++)
                    if (_episode.Lines[i] != null && _episode.Lines[i].LineId == line.LineId)
                        dup++;
                if (dup > 1)
                    EditorGUILayout.HelpBox($"lineId \"{line.LineId}\" 가 에피소드에 {dup}개 존재합니다 (중복).", MessageType.Error);
            }

            // 자기 자신 참조
            if (!string.IsNullOrWhiteSpace(line.NextLineId) && line.NextLineId == line.LineId)
                EditorGUILayout.HelpBox("nextLineId가 자기 자신을 가리킵니다 (무한 루프).", MessageType.Error);
        }

        // ══════════════════════════════════════════
        // Section 2 — Next Line Helper (기존)
        // ══════════════════════════════════════════

        private void DrawNextLineHelper(StoryLineSO line)
        {
            _nextFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_nextFoldout, "── Next Line Helper ──");
            if (!_nextFoldout) { EditorGUILayout.EndFoldoutHeaderGroup(); return; }

            DrawNextLineWarnings(line, _episode);
            EditorGUILayout.Space(2);

            if (_episode != null)
                DrawNextLineControls(line, _episode);
            else
                EditorGUILayout.HelpBox(
                    "소속 StoryEpisodeSO를 찾을 수 없습니다.\n드롭다운을 쓰려면 에피소드의 Lines 목록에 이 라인을 추가하세요.",
                    MessageType.None);

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawNextLineControls(StoryLineSO line, StoryEpisodeSO episode)
        {
            var nextIdProp    = serializedObject.FindProperty("nextLineId");
            string currentNextId = nextIdProp.stringValue;

            // 드롭다운 항목 구성 (자기 자신 제외)
            var labels = new List<string> { "— 없음 (종료) —" };
            var ids    = new List<string> { "" };
            int selectedIdx = 0;

            for (int i = 0; i < episode.Lines.Count; i++)
            {
                StoryLineSO l = episode.Lines[i];
                if (l == null || l == line) continue;

                string label = l.LineId;
                if (l.Speaker != null)
                    label += $"  [{l.Speaker.name}]";
                if (!string.IsNullOrEmpty(l.DialogueText))
                    label += $"  \"{Truncate(l.DialogueText, 18)}\"";

                labels.Add(label);
                ids.Add(l.LineId);

                if (l.LineId == currentNextId)
                    selectedIdx = labels.Count - 1;
            }

            EditorGUI.BeginChangeCheck();
            int newIdx = EditorGUILayout.Popup("다음 줄 선택", selectedIdx, labels.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.Update();
                nextIdProp.stringValue = ids[newIdx];
                serializedObject.ApplyModifiedProperties();
            }

            // 버튼 행
            EditorGUILayout.BeginHorizontal();

            int myIdx = FindIndexInEpisode(line, episode);
            if (myIdx >= 0)
            {
                StoryLineSO nextInList = null;
                for (int i = myIdx + 1; i < episode.Lines.Count; i++)
                    if (episode.Lines[i] != null) { nextInList = episode.Lines[i]; break; }

                using (new EditorGUI.DisabledScope(nextInList == null))
                {
                    string btnLabel = nextInList != null
                        ? $"순서 다음 → {nextInList.LineId}"
                        : "순서 다음 없음";

                    if (GUILayout.Button(btnLabel))
                    {
                        serializedObject.Update();
                        nextIdProp.stringValue = nextInList!.LineId;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(54)))
            {
                serializedObject.Update();
                nextIdProp.stringValue = "";
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("소속 에피소드", episode, typeof(StoryEpisodeSO), false);
        }

        private static void DrawNextLineWarnings(StoryLineSO line, StoryEpisodeSO episode)
        {
            string nextId = line.NextLineId;

            if (string.IsNullOrWhiteSpace(nextId))
            {
                EditorGUILayout.HelpBox("nextLineId가 비어 있습니다 → 에피소드 종료 라인.", MessageType.Info);
                return;
            }

            if (episode == null) return;

            bool found = false;
            for (int i = 0; i < episode.Lines.Count; i++)
                if (episode.Lines[i] != null && episode.Lines[i].LineId == nextId)
                { found = true; break; }

            if (!found)
                EditorGUILayout.HelpBox($"nextLineId \"{nextId}\" 를 에피소드에서 찾을 수 없습니다.", MessageType.Warning);
        }

        // ── 공통 유틸 ────────────────────────────────

        private StoryEpisodeSO FindEpisode(StoryLineSO line)
        {
            if (_episode != null)
            {
                for (int i = 0; i < _episode.Lines.Count; i++)
                    if (_episode.Lines[i] == line) return _episode;
                _episode = null;
            }

            string[] guids = AssetDatabase.FindAssets("t:StoryEpisodeSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ep = AssetDatabase.LoadAssetAtPath<StoryEpisodeSO>(path);
                if (ep == null) continue;
                for (int i = 0; i < ep.Lines.Count; i++)
                    if (ep.Lines[i] == line) return ep;
            }
            return null;
        }

        private static int FindIndexInEpisode(StoryLineSO line, StoryEpisodeSO episode)
        {
            for (int i = 0; i < episode.Lines.Count; i++)
                if (episode.Lines[i] == line) return i;
            return -1;
        }

        private static string Truncate(string s, int max)
        {
            s = s.Replace('\n', ' ');
            return s.Length <= max ? s : s[..max] + "…";
        }
    }
}
