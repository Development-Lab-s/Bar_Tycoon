using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 인라인 모듈 타입을 검색·선택하는 PopupWindowContent.
    /// 타입 발견과 카테고리/이름/정렬은 StoryModuleMetadataAttribute 에서 읽습니다.
    /// PopupWindow.Show(rect, new StoryModuleSearchPopup(onSelected)) 으로 사용.
    /// </summary>
    public sealed class StoryModuleSearchPopup : PopupWindowContent
    {
        // ── 엔트리 ───────────────────────────────────

        private struct Entry
        {
            public Type   type;
            public string displayName;
            public string category;
            public int    sortPriority;
        }

        // ── 상태 ─────────────────────────────────────

        private readonly Action<Type> _onSelected;
        private readonly List<Entry>  _all;
        private          List<Entry>  _filtered;
        private          string       _search = "";
        private          Vector2      _scroll;

        private const float PopupW = 240f;
        private const float PopupH = 300f;

        // ── 생성자 ───────────────────────────────────

        public StoryModuleSearchPopup(Action<Type> onSelected)
        {
            _onSelected = onSelected;
            _all        = BuildAllEntries();
            _filtered   = new List<Entry>(_all);
        }

        // ── PopupWindowContent 오버라이드 ─────────────

        public override Vector2 GetWindowSize() => new Vector2(PopupW, PopupH);

        public override void OnOpen()
        {
            EditorApplication.delayCall += () =>
            {
                if (editorWindow != null)
                    editorWindow.Repaint();
            };
        }

        public override void OnGUI(Rect rect)
        {
            // ── 검색 필드 ────────────────────────────
            GUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("ModuleSearch");
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
                ApplyFilter();

            if (Event.current.type == EventType.Repaint)
                EditorGUI.FocusTextInControl("ModuleSearch");

            GUILayout.Space(2);

            // ── 모듈 리스트 ──────────────────────────
            _scroll = GUILayout.BeginScrollView(_scroll);

            string lastCategory = null;

            foreach (var e in _filtered)
            {
                if (e.category != lastCategory)
                {
                    lastCategory = e.category;
                    GUILayout.Space(2);
                    EditorGUILayout.LabelField(e.category, EditorStyles.centeredGreyMiniLabel);
                }

                if (GUILayout.Button(e.displayName, EditorStyles.toolbarButton))
                {
                    _onSelected?.Invoke(e.type);
                    editorWindow?.Close();
                    GUIUtility.ExitGUI();
                }
            }

            if (_filtered.Count == 0)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("검색 결과 없음", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.EndScrollView();
        }

        // ── 필터 ─────────────────────────────────────

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_search))
            {
                _filtered = new List<Entry>(_all);
                return;
            }

            _filtered = new List<Entry>();
            foreach (var e in _all)
            {
                bool nameMatch = e.displayName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
                bool catMatch  = e.category.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
                if (nameMatch || catMatch)
                    _filtered.Add(e);
            }
        }

        // ── 타입 수집 (attribute 기반) ─────────────────

        private static List<Entry> BuildAllEntries()
        {
            var result = new List<Entry>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<StoryInlineModuleData>())
            {
                if (type.IsAbstract) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                var attr = StoryModuleMetadataAttribute.Get(type);

                // displayName: attribute > 인스턴스 DisplayName > 타입명
                string displayName;
                if (attr != null)
                {
                    displayName = attr.DisplayName;
                }
                else
                {
                    try
                    {
                        var inst = Activator.CreateInstance(type) as StoryInlineModuleData;
                        displayName = inst?.DisplayName ?? type.Name;
                    }
                    catch
                    {
                        displayName = type.Name;
                    }
                }

                result.Add(new Entry
                {
                    type         = type,
                    displayName  = displayName,
                    category     = attr?.Category     ?? "General",
                    sortPriority = attr?.SortPriority ?? 0,
                });
            }

            // category 이름 → sortPriority → displayName 순 정렬
            result.Sort((a, b) =>
            {
                int catCmp = string.Compare(a.category, b.category, StringComparison.Ordinal);
                if (catCmp != 0) return catCmp;
                int priCmp = a.sortPriority.CompareTo(b.sortPriority);
                return priCmp != 0 ? priCmp
                    : string.Compare(a.displayName, b.displayName, StringComparison.Ordinal);
            });

            return result;
        }
    }
}
