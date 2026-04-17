using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 선택된 StoryLineSO 필드를 그래프 에디터 우측에서 직접 편집하는 패널.
    /// PropertyField + SerializedObject 바인딩으로 Undo 자동 지원.
    /// nextLineId는 episode 기반 드롭다운으로 선택 가능.
    /// </summary>
    public sealed class StoryGraphInspectorPanel : VisualElement
    {
        public event Action LineChanged;

        private StoryLineSO      _line;
        private StoryEpisodeSO   _episode;
        private SerializedObject _so;

        private readonly VisualElement _content;

        // Line ID Helper 상태 — Rebuild()를 거쳐도 suffix/foldout 유지
        private readonly StoryLineIdHelperGUI.State _idState = new();

        // ── 생성자 ───────────────────────────────────

        public StoryGraphInspectorPanel()
        {
            style.width           = 300;
            style.minWidth        = 220;
            style.borderLeftWidth = 1;
            style.borderLeftColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));
            style.backgroundColor = new StyleColor(new Color(0.19f, 0.19f, 0.19f));
            style.flexShrink      = 0;

            var header = new Label("Line 편집");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize       = 12;
            header.style.paddingLeft    = 10;
            header.style.paddingTop     = 8;
            header.style.paddingBottom  = 6;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f));
            Add(header);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.contentContainer.style.paddingLeft  = 10;
            scroll.contentContainer.style.paddingRight = 10;
            scroll.contentContainer.style.paddingTop   = 8;
            _content = scroll.contentContainer;
            Add(scroll);
        }

        // ── 공개 API ─────────────────────────────────

        public void SetLine(StoryLineSO line, StoryEpisodeSO episode = null)
        {
            _line    = line;
            _episode = episode ?? _episode;   // episode가 null이면 기존 유지
            _so      = line != null ? new SerializedObject(line) : null;
            Rebuild();
        }

        public void SetEpisode(StoryEpisodeSO episode)
        {
            _episode = episode;
            Rebuild();
        }

        /// <summary>Undo/Redo 후 패널을 최신 SO 값으로 갱신한다.</summary>
        public void Reload()
        {
            if (_so == null) return;
            _so.Update();
            Rebuild();   // 드롭다운 포함 전체 재구성
        }

        // ── 내부 구성 ─────────────────────────────────

        private void Rebuild()
        {
            _content.Unbind();
            _content.Clear();

            if (_line == null || _so == null)
            {
                var hint = new Label("노드를 선택하세요");
                hint.style.color          = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                hint.style.unityTextAlign = TextAnchor.MiddleCenter;
                hint.style.marginTop      = 20;
                _content.Add(hint);
                return;
            }

            // 기본 섹션
            AddSectionLabel("기본");
            BuildLineIdSection();
            AddField("speaker",      "Speaker");
            AddField("nameOverride", "Name Override");
            AddField("dialogueText", "Dialogue Text");

            // 흐름 섹션
            AddSectionLabel("흐름");
            AddField("logVisible",         "Log Visible");
            AddField("allowTapToComplete", "Allow Tap");

            // 다음 라인 섹션 (드롭다운 헬퍼)
            AddSectionLabel("다음 라인");
            BuildNextLineSection();

            // 모듈 섹션
            AddSectionLabel("모듈");
            BuildModuleSection();

            _content.Bind(_so);
        }

        // ── Line ID 헬퍼 (IMGUIContainer로 공용 유틸 호출) ──────

        private void BuildLineIdSection()
        {
            // StoryLineSOEditor 와 완전히 동일한 UI/동작을 IMGUIContainer 로 임베드
            var container = new IMGUIContainer(() =>
            {
                if (_line == null || _so == null) return;
                _so.Update();
                StoryLineIdHelperGUI.Draw(_line, _episode, _so, _idState, () =>
                {
                    LineChanged?.Invoke();
                    // 다음 프레임에 패널 갱신 (IMGUI 렌더 중 UIToolkit 재빌드 방지)
                    EditorApplication.delayCall += Reload;
                });
            });
            container.style.paddingBottom = 4;
            _content.Add(container);
        }

        // ── Next Line 헬퍼 ─────────────────────────────

        private void BuildNextLineSection()
        {
            // nextLineId PropertyField (직접 입력도 허용)
            AddField("nextLineId", "Next Line ID");

            if (_episode == null)
            {
                var noEp = new Label("에피소드 없음 — 드롭다운 사용 불가");
                noEp.style.fontSize = 10;
                noEp.style.color    = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                noEp.style.marginTop = 4;
                _content.Add(noEp);
                return;
            }

            // 경고
            BuildNextLineWarning();

            // 드롭다운
            var (labels, ids, selectedIdx) = BuildDropdownChoices();

            var dropdown = new DropdownField("드롭다운 선택", labels, selectedIdx);
            dropdown.style.marginTop = 4;
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int idx = labels.IndexOf(evt.newValue);
                if (idx < 0) return;
                string newId = ids[idx];

                Undo.RecordObject(_line, "Set nextLineId");
                var so = new SerializedObject(_line);
                so.FindProperty("nextLineId").stringValue = newId;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(_line);

                LineChanged?.Invoke();
            });
            _content.Add(dropdown);

            // 버튼 행: 순서 다음 + Clear
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.marginTop     = 4;

            // 순서 다음 버튼
            int myIdx       = FindIndexInEpisode(_line, _episode);
            StoryLineSO nextInList = null;
            if (myIdx >= 0)
            {
                for (int i = myIdx + 1; i < _episode.Lines.Count; i++)
                    if (_episode.Lines[i] != null) { nextInList = _episode.Lines[i]; break; }
            }

            var nextBtn = new Button(() =>
            {
                if (nextInList == null) return;
                Undo.RecordObject(_line, "Set nextLineId (순서 다음)");
                var so = new SerializedObject(_line);
                so.FindProperty("nextLineId").stringValue = nextInList.LineId;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(_line);
                LineChanged?.Invoke();
                Reload();
            });
            nextBtn.text = nextInList != null ? $"순서 다음 → {Trunc(nextInList.LineId, 14)}" : "순서 다음 없음";
            nextBtn.SetEnabled(nextInList != null);
            nextBtn.style.flexGrow    = 1;
            nextBtn.style.height      = 20;
            nextBtn.style.fontSize    = 10;
            nextBtn.style.paddingLeft = 4;
            nextBtn.style.paddingRight = 4;
            btnRow.Add(nextBtn);

            var clearBtn = new Button(() =>
            {
                Undo.RecordObject(_line, "Clear nextLineId");
                var so = new SerializedObject(_line);
                so.FindProperty("nextLineId").stringValue = "";
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(_line);
                LineChanged?.Invoke();
                Reload();
            });
            clearBtn.text          = "Clear";
            clearBtn.style.width   = 48;
            clearBtn.style.height  = 20;
            clearBtn.style.fontSize = 10;
            clearBtn.style.marginLeft = 4;
            btnRow.Add(clearBtn);

            _content.Add(btnRow);
        }

        private void BuildNextLineWarning()
        {
            string nextId = _so.FindProperty("nextLineId").stringValue;

            if (string.IsNullOrWhiteSpace(nextId)) return;

            // 자기 참조
            if (nextId == _so.FindProperty("lineId").stringValue)
            {
                AddWarningLabel("⚠ 자기 자신 참조 (무한 루프)");
                return;
            }

            // 에피소드에 없음
            bool found = false;
            for (int i = 0; i < _episode.Lines.Count; i++)
                if (_episode.Lines[i] != null && _episode.Lines[i].LineId == nextId)
                { found = true; break; }

            if (!found)
                AddWarningLabel($"⚠ \"{Trunc(nextId, 20)}\" 에피소드에 없음");
        }

        private (List<string> labels, List<string> ids, int selectedIdx) BuildDropdownChoices()
        {
            var labels = new List<string> { "— 없음 (종료) —" };
            var ids    = new List<string> { "" };
            int selected   = 0;

            string currentNextId = _so.FindProperty("nextLineId").stringValue;

            for (int i = 0; i < _episode.Lines.Count; i++)
            {
                var l = _episode.Lines[i];
                if (l == null || l == _line) continue;

                string label = string.IsNullOrWhiteSpace(l.LineId) ? $"(#{i} ID없음)" : l.LineId;
                if (l.Speaker != null) label += $"  [{l.Speaker.DisplayName}]";
                if (!string.IsNullOrEmpty(l.DialogueText))
                    label += $"  \"{Trunc(l.DialogueText, 16)}\"";

                labels.Add(label);
                ids.Add(l.LineId);

                if (l.LineId == currentNextId) selected = labels.Count - 1;
            }

            return (labels, ids, selected);
        }

        // ── 모듈 섹션 (IMGUIContainer) ─────────────────

        private void BuildModuleSection()
        {
            var container = new IMGUIContainer(DrawModuleSectionIMGUI);
            container.style.marginTop    = 2;
            container.style.marginBottom = 4;
            _content.Add(container);
        }

        private void DrawModuleSectionIMGUI()
        {
            if (_line == null || _so == null) return;
            _so.Update();

            DrawSoModulesIMGUI();
            DrawInlineModulesIMGUI();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ 모듈 추가", GUILayout.Height(22)))
                ShowModuleTypeMenu();

            _so.ApplyModifiedProperties();
        }

        private void DrawSoModulesIMGUI()
        {
            var prop = _so.FindProperty("modules");
            if (prop == null || prop.arraySize == 0) return;

            EditorGUILayout.LabelField("SO 모듈 (Legacy)", EditorStyles.centeredGreyMiniLabel);

            for (int i = 0; i < prop.arraySize; i++)
            {
                var elem  = prop.GetArrayElementAtIndex(i);
                var asset = elem.objectReferenceValue as StoryModuleSO;

                if (asset == null)
                {
                    EditorGUILayout.HelpBox($"#{i}: null SO 참조", MessageType.Warning);
                    continue;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 헤더
                EditorGUILayout.BeginHorizontal();
                elem.isExpanded = EditorGUILayout.Foldout(
                    elem.isExpanded,
                    $"[SO] {asset.DisplayName}  ({asset.Timing})",
                    true);
                if (GUILayout.Button("선택", GUILayout.Width(38), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
                EditorGUILayout.EndHorizontal();

                // 인라인 필드 편집
                if (elem.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    var nested = new SerializedObject(asset);
                    nested.Update();
                    var iter  = nested.GetIterator();
                    bool enter = true;
                    while (iter.NextVisible(enter))
                    {
                        enter = false;
                        if (iter.name == "m_Script") continue;
                        EditorGUILayout.PropertyField(iter, true);
                    }
                    nested.ApplyModifiedProperties();
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(1);
            }

            EditorGUILayout.Space(4);
        }

        private void DrawInlineModulesIMGUI()
        {
            var prop = _so.FindProperty("inlineModules");
            if (prop == null || prop.arraySize == 0) return;

            EditorGUILayout.LabelField("인라인 모듈", EditorStyles.centeredGreyMiniLabel);

            int toDelete = -1;

            for (int i = 0; i < prop.arraySize; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                var data = elem.managedReferenceValue as StoryInlineModuleData;
                string label = data?.DisplayName ?? $"Unknown #{i}";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 헤더 행
                EditorGUILayout.BeginHorizontal();
                elem.isExpanded = EditorGUILayout.Foldout(elem.isExpanded, label, true);

                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    toDelete = i;
                GUI.backgroundColor = prevBg;

                EditorGUILayout.EndHorizontal();

                // 필드 (열린 상태)
                if (elem.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    var child = elem.Copy();
                    var end   = elem.GetEndProperty();
                    bool first = true;
                    while (child.NextVisible(first))
                    {
                        first = false;
                        if (SerializedProperty.EqualContents(child, end)) break;
                        EditorGUILayout.PropertyField(child, true);
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(1);
            }

            // 이터레이션 후 삭제 처리
            if (toDelete >= 0)
            {
                Undo.RecordObject(_line, "Remove Inline Module");
                prop.DeleteArrayElementAtIndex(toDelete);
                _so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_line);
            }
        }

        private void ShowModuleTypeMenu()
        {
            var menu  = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom<StoryInlineModuleData>();

            foreach (var type in types)
            {
                if (type.IsAbstract) continue;

                var temp = Activator.CreateInstance(type) as StoryInlineModuleData;
                string displayName  = temp?.DisplayName ?? type.Name;
                var    capturedType = type;

                menu.AddItem(new GUIContent(displayName), false, () => AddInlineModule(capturedType));
            }

            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("등록된 인라인 모듈 없음"));

            menu.ShowAsContext();
        }

        private void AddInlineModule(Type type)
        {
            if (_line == null || _so == null) return;

            Undo.RecordObject(_line, $"Add Inline Module: {type.Name}");
            _so.Update();

            var prop = _so.FindProperty("inlineModules");
            prop.arraySize++;
            var newElem = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            newElem.managedReferenceValue = Activator.CreateInstance(type);

            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_line);
        }

        // ── 공통 헬퍼 ─────────────────────────────────

        private void AddSectionLabel(string text)
        {
            var lbl = new Label(text);
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.fontSize          = 10;
            lbl.style.color             = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
            lbl.style.marginTop         = 10;
            lbl.style.marginBottom      = 2;
            lbl.style.paddingBottom     = 2;
            lbl.style.borderBottomWidth = 1;
            lbl.style.borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
            _content.Add(lbl);
        }

        private void AddField(string propName, string labelText)
        {
            var prop = _so.FindProperty(propName);
            if (prop == null) return;
            var field = new PropertyField(prop, labelText);
            field.RegisterCallback<SerializedPropertyChangeEvent>(_ => LineChanged?.Invoke());
            _content.Add(field);
        }

        private void AddWarningLabel(string text)
        {
            var lbl = new Label(text);
            lbl.style.fontSize  = 10;
            lbl.style.color     = new StyleColor(new Color(1f, 0.6f, 0.2f));
            lbl.style.marginTop = 2;
            _content.Add(lbl);
        }

        private static int FindIndexInEpisode(StoryLineSO line, StoryEpisodeSO ep)
        {
            for (int i = 0; i < ep.Lines.Count; i++)
                if (ep.Lines[i] == line) return i;
            return -1;
        }

        private static string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace('\n', ' ');
            return s.Length <= max ? s : s[..max] + "…";
        }

        // ── 텍스트 필드 포커스 감지 ──────────────────

        public static bool IsFocusedOnTextField(VisualElement root)
        {
            var el = root?.focusController?.focusedElement as VisualElement;
            while (el != null)
            {
                if (el is TextField or IntegerField or FloatField or LongField) return true;
                el = el.parent;
            }
            return false;
        }
    }
}
