using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// StoryModuleSO 파생 타입을 검색·선택하는 독립 EditorWindow.
    ///
    /// ■ 표시 대상:  non-abstract, non-generic, non-compiler-generated StoryModuleSO 파생 타입
    /// ■ 표시 제외:  abstract / IsGenericTypeDefinition / Name에 '<' 포함 / StoryModuleSO 자체
    /// ■ 열릴 때마다 TypeCache 기반으로 타입 재수집 → 새 클래스 작성 후 컴파일만 하면 즉시 표시
    /// ■ 키보드:     ↑↓ 선택, Enter/더블클릭 확정, ESC 닫기
    ///
    /// 사용법:
    ///   StoryModulePickerWindow.Show(type => AddModuleToLine(type), nearScreenPos);
    /// </summary>
    public sealed class StoryModulePickerWindow : EditorWindow
    {
        // ── 엔트리 구조 ─────────────────────────────────────────────────────

        private struct ModuleEntry
        {
            public Type   type;
            public string displayName;
            public string category;
            public int    sortPriority;
            public Color  accentColor;
        }

        // ── 상태 ────────────────────────────────────────────────────────────

        private Action<Type>               _onSelected;
        private readonly List<ModuleEntry> _all      = new();
        private readonly List<ModuleEntry> _filtered = new();
        private string                     _search   = "";
        private int                        _selectedIdx;
        private Vector2                    _scroll;
        private bool                       _needFocus;

        private const float RowH    = 22f;
        private const float HeaderH = 16f;
        private const float FooterH = 30f;

        // ── 진입점 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 창을 열거나 기존 인스턴스를 재사용합니다.
        /// nearScreenPos: 창의 좌상단 위치 (screen 좌표). Vector2.zero이면 화면 중앙.
        /// </summary>
        public static void Show(Action<Type> onSelected, Vector2 nearScreenPos = default)
        {
            var wins = Resources.FindObjectsOfTypeAll<StoryModulePickerWindow>();
            var win  = wins.Length > 0 ? wins[0] : CreateInstance<StoryModulePickerWindow>();

            win.titleContent = new GUIContent("Add Module");
            win._onSelected  = onSelected;
            win.ResetState(nearScreenPos);
            win.ShowUtility();
            win.Focus();
        }

        // ── 생명주기 ────────────────────────────────────────────────────────

        private void OnEnable()
        {
            minSize   = new Vector2(240f, 300f);
            _needFocus = true;
            BuildEntries();
        }

        private void OnGUI()
        {
            HandleKeyboard();
            DrawSearchField();
            DrawEntryList();
            DrawFooter();
        }

        // ── 초기화 ──────────────────────────────────────────────────────────

        private void ResetState(Vector2 nearScreenPos)
        {
            _search      = "";
            _selectedIdx = 0;
            _scroll      = Vector2.zero;
            _needFocus   = true;
            BuildEntries();

            // 창 위치 결정
            var size = new Vector2(Mathf.Max(position.width,  280f),
                                   Mathf.Max(position.height, 360f));
            Vector2 pos = nearScreenPos != Vector2.zero
                ? nearScreenPos
                : new Vector2(Screen.width * 0.5f - size.x * 0.5f,
                              Screen.height * 0.5f - size.y * 0.5f);

            position = new Rect(pos, size);
        }

        // ── 타입 수집 ────────────────────────────────────────────────────────

        private void BuildEntries()
        {
            _all.Clear();

            foreach (var type in TypeCache.GetTypesDerivedFrom<StoryModuleSO>())
            {
                if (!IsValidModuleType(type)) continue;

                var attr = StoryModuleMetadataAttribute.Get(type);
                _all.Add(new ModuleEntry
                {
                    type         = type,
                    displayName  = attr?.DisplayName  ?? type.Name,
                    category     = attr?.Category      ?? "General",
                    sortPriority = attr?.SortPriority  ?? 0,
                    accentColor  = StoryModuleMetadataAttribute.GetColor(type, Color.gray),
                });
            }

            _all.Sort((a, b) =>
            {
                int c = string.Compare(a.category, b.category, StringComparison.Ordinal);
                if (c != 0) return c;
                c = a.sortPriority.CompareTo(b.sortPriority);
                return c != 0 ? c : string.Compare(a.displayName, b.displayName, StringComparison.Ordinal);
            });

            ApplyFilter();
        }

        /// <summary>
        /// 표시 필터 규칙 (모두 통과해야 검색창에 표시됨):
        ///   ✓ abstract가 아닌 것
        ///   ✓ generic type definition이 아닌 것
        ///   ✓ 컴파일러 생성 타입이 아닌 것 (이름에 '<' 없음)
        ///   ✓ StoryModuleSO 자체가 아닌 것
        /// </summary>
        private static bool IsValidModuleType(Type t)
        {
            if (t.IsAbstract)              return false;
            if (t.IsGenericTypeDefinition)  return false;
            if (t.Name.Contains('<'))       return false;
            if (t == typeof(StoryModuleSO)) return false;
            if (StoryModuleMetadataAttribute.Get(t)?.IsDeprecated == true) return false;
            return true;
        }

        // ── 필터 ────────────────────────────────────────────────────────────

        private void ApplyFilter()
        {
            _filtered.Clear();
            if (string.IsNullOrWhiteSpace(_search))
            {
                _filtered.AddRange(_all);
            }
            else
            {
                foreach (var e in _all)
                {
                    bool hit = e.displayName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                            || e.category  .IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (hit) _filtered.Add(e);
                }
            }

            _selectedIdx = Mathf.Clamp(_selectedIdx, 0, Mathf.Max(0, _filtered.Count - 1));
        }

        // ── 키보드 핸들러 ────────────────────────────────────────────────────

        private void HandleKeyboard()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.UpArrow:
                    _selectedIdx = Mathf.Max(0, _selectedIdx - 1);
                    ScrollToSelected();
                    Repaint();
                    e.Use();
                    break;

                case KeyCode.DownArrow:
                    _selectedIdx = Mathf.Min(_filtered.Count - 1, _selectedIdx + 1);
                    ScrollToSelected();
                    Repaint();
                    e.Use();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_filtered.Count > 0 && (uint)_selectedIdx < (uint)_filtered.Count)
                    {
                        ConfirmSelection(_filtered[_selectedIdx].type);
                        e.Use();
                    }
                    break;

                case KeyCode.Escape:
                    Close();
                    e.Use();
                    break;
            }
        }

        private void ScrollToSelected()
        {
            float y       = _selectedIdx * RowH;
            float viewH   = position.height - 44f - FooterH;
            if (y < _scroll.y)
                _scroll.y = y;
            else if (y + RowH > _scroll.y + viewH)
                _scroll.y = y + RowH - viewH;
        }

        // ── 검색 필드 ────────────────────────────────────────────────────────

        private void DrawSearchField()
        {
            GUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("ModuleSearch");
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedIdx = 0;
                ApplyFilter();
            }
            GUILayout.Space(2);

            // 열릴 때 한 번만 포커스
            if (_needFocus)
            {
                EditorGUI.FocusTextInControl("ModuleSearch");
                if (Event.current.type == EventType.Repaint)
                    _needFocus = false;
            }
        }

        // ── 엔트리 리스트 ────────────────────────────────────────────────────

        private void DrawEntryList()
        {
            _scroll = GUILayout.BeginScrollView(_scroll,
                GUILayout.ExpandHeight(true));

            string lastCategory = null;
            int flatIdx = 0;

            foreach (var entry in _filtered)
            {
                // 카테고리 헤더
                if (entry.category != lastCategory)
                {
                    lastCategory = entry.category;
                    GUILayout.Space(3);
                    EditorGUILayout.LabelField(entry.category, EditorStyles.centeredGreyMiniLabel,
                        GUILayout.Height(HeaderH));
                }

                bool isSelected = flatIdx == _selectedIdx;

                // 행 배경 (선택)
                Rect rowRect = EditorGUILayout.GetControlRect(false, RowH);
                if (isSelected)
                    EditorGUI.DrawRect(rowRect, new Color(0.25f, 0.45f, 0.75f, 0.45f));
                else if (rowRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.05f));

                // 강조색 점
                var dotRect = new Rect(rowRect.x + 5f,
                    rowRect.y + (rowRect.height - 9f) * 0.5f, 9f, 9f);
                EditorGUI.DrawRect(dotRect, entry.accentColor);

                // 이름
                var labelRect = new Rect(rowRect.x + 18f, rowRect.y, rowRect.width - 18f, rowRect.height);
                var style = isSelected ? EditorStyles.whiteLabel : EditorStyles.label;
                GUI.Label(labelRect, entry.displayName, style);

                // 마우스 이벤트
                var ev = Event.current;
                if (ev.type == EventType.MouseDown && rowRect.Contains(ev.mousePosition))
                {
                    _selectedIdx = flatIdx;
                    if (ev.clickCount >= 2)
                    {
                        ConfirmSelection(entry.type);
                        ev.Use();
                        GUILayout.EndScrollView();
                        return;
                    }
                    Repaint();
                    ev.Use();
                }

                flatIdx++;
            }

            if (_filtered.Count == 0)
            {
                GUILayout.Space(16);
                EditorGUILayout.LabelField("검색 결과 없음", EditorStyles.centeredGreyMiniLabel);

                if (_all.Count == 0)
                {
                    GUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        "StoryModuleSO 파생 타입이 없습니다.\n" +
                        "non-abstract StoryModuleSO 클래스를 작성하고 컴파일하세요.",
                        MessageType.Info);
                }
            }

            GUILayout.EndScrollView();
        }

        // ── 하단 버튼 ────────────────────────────────────────────────────────

        private void DrawFooter()
        {
            GUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.25f, 0.45f, 0.75f);
                bool addDisabled = _filtered.Count == 0
                    || (uint)_selectedIdx >= (uint)_filtered.Count;
                using (new EditorGUI.DisabledScope(addDisabled))
                {
                    if (GUILayout.Button("Add", GUILayout.Height(24)))
                        ConfirmSelection(_filtered[_selectedIdx].type);
                }
                GUI.backgroundColor = prevBg;

                if (GUILayout.Button("Cancel", GUILayout.Height(24), GUILayout.Width(60)))
                    Close();
            }

            GUILayout.Space(2);
        }

        // ── 선택 확정 ────────────────────────────────────────────────────────

        private void ConfirmSelection(Type type)
        {
            _onSelected?.Invoke(type);
            Close();
        }
    }
}
