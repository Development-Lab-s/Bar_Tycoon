using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    [CustomEditor(typeof(StoryLineSO))]
    public sealed class StoryLineSOEditor : UnityEditor.Editor
    {
        private StoryEpisodeSO _episode;

        private bool _nextFoldout = true;

        // Line ID Helper 상태 (StoryLineIdHelperGUI 공용 유틸로 위임)
        private readonly StoryLineIdHelperGUI.State _idState = new();

        // ── 진입점 ──────────────────────────────────

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var line = (StoryLineSO)target;
            _episode = FindEpisode(line);

            serializedObject.Update();

            EditorGUILayout.Space(8);
            // ── Line ID Helper (공용 유틸 호출) ──────
            StoryLineIdHelperGUI.Draw(line, _episode, serializedObject, _idState);

            EditorGUILayout.Space(4);
            DrawNextLineHelper(line);

            serializedObject.ApplyModifiedProperties();
        }

        // ══════════════════════════════════════════
        // Section 2 — Next Line Helper
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

            EditorGUILayout.BeginHorizontal();

            int myIdx = StoryLineIdHelperGUI.IndexInEpisode(line, episode);
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

        private static string Truncate(string s, int max)
        {
            s = s.Replace('\n', ' ');
            return s.Length <= max ? s : s[..max] + "…";
        }
    }
}
