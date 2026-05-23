using BBJ.GridSystem.Objects;
using BBJ.WorkplaceSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BBJ.GridSystem.Editor
{
    /// <summary>
    /// 쿼터뷰 2D 장애물 배치 에디터 윈도우 v5
    ///
    /// ■ 수정된 버그 목록
    ///
    ///  [BUG-1] 좌표 0.5f 오프셋 오류
    ///    증상: 에디터에서 배치한 오브젝트 위치가 GridManager.ApplyObstacle 위치와 다름
    ///    원인: CellToCenter()에서 cellSize * 0.5f 를 더했으나
    ///          GridManager는 CellToWorld만 사용 (보정 없음)
    ///    수정: CellToWorld 결과를 그대로 사용, 0.5f 보정 제거
    ///
    ///  [BUG-2] static 필드 오염으로 인한 상태 불일치
    ///    증상: 창을 닫고 다시 열면 _gridManager/_layoutSO가 남아있는데
    ///          _placed/_occupied는 비어있어 중복 배치 허용됨
    ///    원인: _gridManager, _layoutSO는 static → 창 재열기 시 유지
    ///          _placed, _occupied는 인스턴스 → 창 재열기 시 초기화
    ///    수정: static 제거, OnEnable에서 씬 스캔으로 _placed/_occupied 복원
    ///
    ///  [BUG-3] Undo 후 _occupied 불일치
    ///    증상: Undo로 GO가 복원될 때 _occupied에 셀이 추가되지 않아
    ///          복원된 위치에 다시 배치 가능해짐
    ///    원인: SyncStateAfterUndoRedo가 삭제만 처리하고 복원은 미처리
    ///    수정: 복원된 GO도 스캔해 _placed/_occupied 재구성
    ///          → RebuildStateFromScene()으로 완전 재구성
    ///
    ///  [BUG-4] RemovePlaced에서 GO 없을 때 _placed 미제거
    ///    증상: GO가 이미 없는 셀을 지우기 모드로 클릭하면
    ///          _placed에 항목이 남아 해당 셀에 영구적으로 배치 불가
    ///    원인: RemovePlaced에서 go == null이면 Undo.DestroyObjectImmediate
    ///          를 건너뛰지만 _placed.Remove는 실행됨 → 실제로는 제거됨.
    ///          그러나 Undo 후 복원 시 goID가 바뀌어 매칭 실패
    ///    수정: RebuildStateFromScene()로 Undo 후 전체 재구성
    ///
    ///  [BUG-5] DrawPlacedSection 루프 중 딕셔너리 수정
    ///    증상: foreach 루프 안에서 toRemove로 Remove를 호출하면
    ///          컬렉션 수정 예외 가능성 존재
    ///    수정: 루프 종료 후 제거 (기존 패턴 유지하되 명확화)
    ///
    ///  [BUG-6] OnDestroy에서 SaveToLayoutSO 호출 시 Dialog 오류
    ///    증상: 창 닫히는 도중 DisplayDialog 호출이 간헐적으로 실패
    ///    원인: OnDestroy 타이밍에 Unity 에디터 UI가 불안정
    ///    수정: _pendingSave 플래그 → OnDisable에서 처리
    ///
    ///  [BUG-7] GUI.backgroundColor 미복원
    ///    증상: 일부 버튼 이후 다른 UI 요소 색상이 오염됨
    ///    원인: ModeBtn에서 backgroundColor를 변경 후 복원 안 함
    ///    수정: ModeBtn 종료 시 Color.white 복원
    ///
    ///  [BUG-8] HasUnsavedChanges 비교 시 매 프레임 ToHashSet() 할당
    ///    증상: OnGUI마다 가비지 생성 → GC 스파이크
    ///    수정: SetEquals 대신 Count + All 비교로 할당 제거
    /// </summary>
    public class ObstacleEditorWindow : EditorWindow
    {
        // ─────────────────────────────────────────────────────────────
        //  상수
        // ─────────────────────────────────────────────────────────────
        private const string MenuPath = "Tools/Obstacle Placer (Quarter-view)";
        private const string WindowTitle = "Obstacle Placer";
        private const string DefaultSaveDir = "Assets/ObstacleLayouts";

        // ─────────────────────────────────────────────────────────────
        //  참조
        //  [FIX-2] static 제거 → 창 재열기 시 상태 오염 방지
        // ─────────────────────────────────────────────────────────────
        private GridManager _gridManager;
        private Grid _grid;           // GetComponent 캐시 (매 이벤트 호출 방지)
        private StageLayoutSO _layoutSO;

        // ─────────────────────────────────────────────────────────────
        //  배치 상태
        //  _placed   : 원점 셀 인덱스 → (GO instanceID, ObjectData)
        //  _occupied : 원점 + BlockedOffsets 전체 점유 셀 집합
        // ─────────────────────────────────────────────────────────────
        private readonly Dictionary<Vector2Int, (int goID, ObjectDataSO data)> _placed
            = new Dictionary<Vector2Int, (int, ObjectDataSO)>();
        private readonly HashSet<Vector2Int> _occupied = new HashSet<Vector2Int>();

        // [FIX-8] 저장 시점 스냅샷을 HashSet으로 보관, 비교 시 할당 없음
        private readonly HashSet<Vector2Int> _savedSnapshot = new HashSet<Vector2Int>();

        // [FIX-8] ToHashSet() 없이 비교
        private bool HasUnsavedChanges
        {
            get
            {
                if (_placed.Count != _savedSnapshot.Count) return true;
                foreach (var k in _placed.Keys)
                    if (!_savedSnapshot.Contains(k)) return true;
                return false;
            }
        }

        // [FIX-6] OnDestroy 타이밍 문제 → OnDisable에서 처리
        private bool _pendingSaveOnClose;

        // ─────────────────────────────────────────────────────────────
        //  모드
        // ─────────────────────────────────────────────────────────────
        private bool _isPlacing;
        private bool _isEraseMode;
        private Vector2Int _hoveredCell = new Vector2Int(int.MinValue, int.MinValue);

        // ─────────────────────────────────────────────────────────────
        //  팔레트
        // ─────────────────────────────────────────────────────────────
        private List<ObjectDataSO> _palette = new List<ObjectDataSO>();
        private int _selectedIdx = -1;
        private ObjectDataSO SelectedOD => (_selectedIdx >= 0 && _selectedIdx < _palette.Count)
                                         ? _palette[_selectedIdx] : null;
        private Vector2 _paletteScroll;
        private Dictionary<ObjectDataSO, Texture2D> _previewCache
            = new Dictionary<ObjectDataSO, Texture2D>();

        // ─────────────────────────────────────────────────────────────
        //  스크롤
        // ─────────────────────────────────────────────────────────────
        private Vector2 _placedScroll;
        private Vector2 _mainScroll;

        // ─────────────────────────────────────────────────────────────
        //  GUIStyle 캐시 (OnGUI 매 프레임 new GUIStyle 할당 방지)
        // ─────────────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _modeStatusStyle;
        private GUIStyle _paletteBoldStyle;
        private static GUIStyle _secLabelStyle;
        private static GUIStyle _hoverLabelStyle;

        // DrawDiamond 버텍스 재사용 배열 (2500+ 셀 × 프레임당 new Vector3[4] 방지)
        private static readonly Vector3[] _diamondVerts = new Vector3[4];

        // ─────────────────────────────────────────────────────────────
        //  색상
        // ─────────────────────────────────────────────────────────────
        private static readonly Color CGrid = new Color(0.38f, 0.48f, 0.58f, 0.28f);
        private static readonly Color CGridB = new Color(0.38f, 0.48f, 0.58f, 0.60f);
        private static readonly Color CHover = new Color(0.40f, 0.85f, 1.00f, 0.28f);
        private static readonly Color CHoverB = new Color(0.40f, 0.85f, 1.00f, 0.90f);
        private static readonly Color CErase = new Color(1.00f, 0.32f, 0.32f, 0.28f);
        private static readonly Color CEraseB = new Color(1.00f, 0.32f, 0.32f, 0.90f);
        private static readonly Color CBlocked = new Color(0.85f, 0.18f, 0.18f, 0.42f);
        private static readonly Color CBlockedB = new Color(1.00f, 0.38f, 0.38f, 0.85f);
        private static readonly Color CInteract = new Color(0.18f, 0.75f, 0.60f, 0.38f);
        private static readonly Color CInteractB = new Color(0.40f, 1.00f, 0.85f, 0.85f);
        private static readonly Color CPlaced = new Color(0.28f, 0.58f, 1.00f, 0.38f);
        private static readonly Color CPlacedB = new Color(0.48f, 0.72f, 1.00f, 0.88f);
        private static readonly Color CConflict = new Color(1.00f, 0.75f, 0.10f, 0.42f);
        private static readonly Color CConflictB = new Color(1.00f, 0.90f, 0.20f, 0.90f);

        // ─────────────────────────────────────────────────────────────
        //  메뉴
        // ─────────────────────────────────────────────────────────────
        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var w = GetWindow<ObstacleEditorWindow>(WindowTitle);
            w.minSize = new Vector2(340f, 560f);
        }

        // ─────────────────────────────────────────────────────────────
        //  라이프사이클
        // ─────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedo;

            RefreshPalette();

            // GridManager 자동 탐색
            if (_gridManager == null)
                _gridManager = FindFirstObjectByType<GridManager>();

            if (_gridManager != null)
            {
                _grid = _gridManager.GetComponent<Grid>();
                RebuildStateFromScene();
            }
        }

        private void OnDisable()
        {
            // [FIX-6] OnDestroy 대신 OnDisable에서 저장 확인 처리
            if (_pendingSaveOnClose)
            {
                _pendingSaveOnClose = false;
                SaveToLayoutSO();
            }

            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;
            _isPlacing = false;
            SceneView.RepaintAll();
        }

        private void OnDestroy()
        {
            if (_placed.Count == 0 || !HasUnsavedChanges) return;

            // [FIX-6] DisplayDialogComplex는 OnDestroy에서 안전하게 호출 가능
            // 단, SaveToLayoutSO의 AssetDatabase 호출은 OnDisable로 위임
            int choice = EditorUtility.DisplayDialogComplex(
                "저장하지 않은 배치가 있습니다",
                $"배치된 장애물 {_placed.Count}개를 SO에 저장하지 않았습니다.\n\n" +
                "• 저장 후 닫기 : StageLayoutSO에 저장합니다\n" +
                "• 그냥 닫기    : 오브젝트는 씬에 유지, SO 저장 안 함",
                "저장 후 닫기",
                "그냥 닫기",
                "");

            if (choice == 0) _pendingSaveOnClose = true;
        }

        // ─────────────────────────────────────────────────────────────
        //  [FIX-2,3,4] 씬 스캔으로 _placed/_occupied 완전 재구성
        //
        //  GridManager 자식 GO 중 이름이 _placed의 data.Prefab.name과
        //  일치하는 것을 찾아 재매핑.
        //  Undo 후 GO instanceID가 바뀌므로 이름+위치로 매칭.
        // ─────────────────────────────────────────────────────────────
        private void RebuildStateFromScene()
        {
            if (_gridManager == null) return;

            if (_grid == null) _grid = _gridManager.GetComponent<Grid>();
            Grid g = _grid;

            // 씬의 실제 GO 수집 (GridManager 자식 전체)
            var sceneGOs = new List<GameObject>();
            for (int i = 0; i < _gridManager.transform.childCount; i++)
            {
                var child = _gridManager.transform.GetChild(i);
                if (child != null) sceneGOs.Add(child.gameObject);
            }
            // HashSet으로 O(1) 조회 (List.Contains는 O(n) → 루프 내에서 O(n²))
            var sceneGOSet = new HashSet<GameObject>(sceneGOs);

            // 현재 _placed 중 GO가 여전히 살아있는 항목은 instanceID만 갱신
            // GO가 사라진 항목은 제거
            var toRemove = new List<Vector2Int>();
            foreach (var kv in _placed)
            {
                var go = EditorUtility.EntityIdToObject(kv.Value.goID) as GameObject;
                if (go == null || !sceneGOSet.Contains(go))
                    toRemove.Add(kv.Key);
            }
            foreach (var cell in toRemove)
            {
                foreach (var fc in Footprint(cell, _placed[cell].data))
                    _occupied.Remove(fc);
                _placed.Remove(cell);
            }

            // [FIX-3] Undo 복원된 GO: _placed에 없지만 씬에는 있는 GO 탐색
            // 프리팹 소스 직접 비교 (이름 매칭은 유사 이름 오매칭으로 BlockedOffsets 오염 위험)
            var placedGOIDs = new HashSet<int>(_placed.Values.Select(v => v.goID));
            foreach (var go in sceneGOs)
            {
                if (go == null) continue;
                if (placedGOIDs.Contains(go.GetInstanceID())) continue; // 이미 추적 중

                // 프리팹 에셋 소스로 ObjectData 확정 매칭
                var prefabRoot   = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go) as GameObject;
                var prefabTycoon = prefabRoot?.GetComponent<TycoonObject>();
                ObjectDataSO matched = prefabTycoon == null ? null :
                    _palette.FirstOrDefault(od =>
                        od != null && od.WorkplacePrefab != null && od.WorkplacePrefab == prefabTycoon);

                if (matched == null) continue;

                // 위치로 셀 인덱스 역산
                Vector3Int cellInt = g.WorldToCell(go.transform.position);
                Vector2Int cell = new Vector2Int(
                    cellInt.x - _gridManager.Offset.x,
                    cellInt.y - _gridManager.Offset.y);

                if (!InGrid(cell)) continue;
                if (_placed.ContainsKey(cell)) continue; // 이미 점유

                var fp = Footprint(cell, matched);
                bool conflict = fp.Any(fc => !InGrid(fc) || _occupied.Contains(fc));
                if (conflict) continue;

                _placed[cell] = (go.GetInstanceID(), matched);
                foreach (var fc in fp) _occupied.Add(fc);
                placedGOIDs.Add(go.GetInstanceID());
            }

            Repaint();
            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────────────────────────
        //  Undo/Redo 콜백
        //  [FIX-3] 완전 재구성으로 교체
        // ─────────────────────────────────────────────────────────────
        private void OnUndoRedo() => RebuildStateFromScene();

        // ─────────────────────────────────────────────────────────────
        //  팔레트
        // ─────────────────────────────────────────────────────────────
        private void RefreshPalette()
        {
            _palette.Clear();
            _previewCache.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:ObjectDataSO"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var od = AssetDatabase.LoadAssetAtPath<ObjectDataSO>(p);
                if (od != null) _palette.Add(od);
            }
            _palette.Sort((a, b) =>
                string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.Ordinal));
            if (_selectedIdx >= _palette.Count) _selectedIdx = _palette.Count - 1;
        }

        // ─────────────────────────────────────────────────────────────
        //  GUI 메인
        // ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            DrawHeader();
            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
            EditorGUILayout.Space(4);
            DrawSetupSection();
            Divider();
            DrawModeToolbar();
            Divider();
            DrawPaletteSection();
            Divider();
            DrawLayoutSOSection();
            Divider();
            DrawPlacedSection();
            GUILayout.FlexibleSpace();
            DrawFooter();
            EditorGUILayout.EndScrollView();
        }

        // ── 헤더 ──────────────────────────────────────────────────────
        private void DrawHeader()
        {
            Rect r = GUILayoutUtility.GetRect(0, 38f);
            EditorGUI.DrawRect(r, new Color(0.08f, 0.10f, 0.14f, 1f));

            string title = HasUnsavedChanges ? "Obstacle Placer  v5  [미저장]" : "Obstacle Placer  v5";
            Color titleCol = HasUnsavedChanges ? new Color(1.0f, 0.80f, 0.30f) : new Color(0.65f, 0.88f, 1f);

            if (_headerStyle == null)
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            _headerStyle.normal.textColor = titleCol;
            GUI.Label(new Rect(r.x + 10, r.y + 2, r.width, r.height), title, _headerStyle);
        }

        // ── 그리드 설정 ────────────────────────────────────────────────
        private void DrawSetupSection()
        {
            SecLabel("Grid Manager");
            EditorGUILayout.BeginHorizontal();
            var prevGM = _gridManager;
            _gridManager = (GridManager)EditorGUILayout.ObjectField(
                _gridManager, typeof(GridManager), true);

            // GridManager가 새로 연결되면 Grid 캐시 갱신 + 씬 상태 재구성
            if (_gridManager != prevGM && _gridManager != null)
            {
                _grid = _gridManager.GetComponent<Grid>();
                RebuildStateFromScene();
            }

            if (GUILayout.Button("자동 탐색", GUILayout.Width(64)))
            {
                _gridManager = FindFirstObjectByType<GridManager>();
                if (_gridManager == null)
                    EditorUtility.DisplayDialog("알림", "씬에 GridManager가 없습니다.", "확인");
                else
                {
                    _grid = _gridManager.GetComponent<Grid>();
                    RebuildStateFromScene();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_gridManager == null)
            {
                EditorGUILayout.HelpBox("GridManager를 지정해야 배치 가능합니다.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(
                $"  그리드 {_gridManager.Size.x}x{_gridManager.Size.y}  |  " +
                $"Offset ({_gridManager.Offset.x},{_gridManager.Offset.y})  |  " +
                $"Cell {_grid.cellSize.x:F3}x{_grid.cellSize.y:F3}",
                EditorStyles.miniLabel);
        }

        // ── 모드 툴바 ──────────────────────────────────────────────────
        private void DrawModeToolbar()
        {
            SecLabel("배치 모드");
            EditorGUILayout.BeginHorizontal();
            ModeBtn("배치", !_isEraseMode && _isPlacing, new Color(0.35f, 0.80f, 1.0f),
                () => { _isPlacing = true; _isEraseMode = false; });
            ModeBtn("지우기", _isEraseMode && _isPlacing, new Color(1.0f, 0.45f, 0.45f),
                () => { _isPlacing = true; _isEraseMode = true; });

            // [FIX-7] backgroundColor 반드시 복원
            GUI.backgroundColor = Color.white;
            GUI.enabled = _isPlacing;
            if (GUILayout.Button("종료", GUILayout.Height(26), GUILayout.Width(48)))
            { _isPlacing = false; SceneView.RepaintAll(); }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            string msg = !_isPlacing ? "비활성 - Scene에서 동작 없음"
                       : _isEraseMode ? "지우기 모드 - 좌클릭: 제거"
                                      : "배치 모드 - 좌클릭: 배치 / 우클릭: 제거";
            Color mc = !_isPlacing ? Color.gray
                     : _isEraseMode ? new Color(1f, 0.55f, 0.55f)
                                    : new Color(0.45f, 0.90f, 1f);
            if (_modeStatusStyle == null)
                _modeStatusStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            _modeStatusStyle.normal.textColor = mc;
            GUILayout.Label(msg, _modeStatusStyle);
        }

        // ── 팔레트 ────────────────────────────────────────────────────
        private void DrawPaletteSection()
        {
            EditorGUILayout.BeginHorizontal();
            SecLabel("장애물 팔레트");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("새로고침", GUILayout.Width(60), GUILayout.Height(16)))
                RefreshPalette();
            EditorGUILayout.EndHorizontal();

            if (_palette.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "ObjectData SO가 없습니다.\nAssets > Create > GridSystem > Object",
                    MessageType.Info);
                return;
            }

            float rowH = 58f;
            float viewH = Mathf.Min(_palette.Count * (rowH + 4f) + 6f, 180f);
            _paletteScroll = EditorGUILayout.BeginScrollView(_paletteScroll, GUILayout.Height(viewH));

            for (int i = 0; i < _palette.Count; i++)
            {
                ObjectDataSO od = _palette[i];
                bool sel = (i == _selectedIdx);

                Rect row = EditorGUILayout.BeginHorizontal(GUILayout.Height(rowH));
                EditorGUI.DrawRect(row,
                    sel ? new Color(0.22f, 0.44f, 0.70f, 0.50f)
                        : new Color(0.16f, 0.16f, 0.18f, 0.50f));

                if (od.WorkplacePrefab != null)
                {
                    if (!_previewCache.TryGetValue(od, out Texture2D tex) || tex == null)
                    {
                        tex = AssetPreview.GetAssetPreview(od.WorkplacePrefab);
                        if (tex != null) _previewCache[od] = tex;
                        else Repaint();
                    }
                    if (tex != null)
                        GUILayout.Label(tex,
                            GUILayout.Width(56), GUILayout.Height(56));
                    else
                        GUILayout.Label("...",
                            GUILayout.Width(56), GUILayout.Height(56));
                }
                else
                    GUILayout.Label("No Prefab", GUILayout.Width(54), GUILayout.Height(54));

                EditorGUILayout.BeginVertical();
                GUILayout.Space(6);
                if (_paletteBoldStyle == null)
                    _paletteBoldStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
                GUILayout.Label(od.DisplayName ?? od.name, _paletteBoldStyle);
                var tsd = GetTileSetData(od);
                GUILayout.Label(
                    $"Blocked:{tsd?.BlockedOffsets?.Length ?? 0}  Walkable:{tsd?.IsWalkable}",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                if (Event.current.type == EventType.MouseDown &&
                    row.Contains(Event.current.mousePosition))
                { _selectedIdx = i; Event.current.Use(); Repaint(); }

                GUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();
        }

        // ── Layout SO ─────────────────────────────────────────────────
        private void DrawLayoutSOSection()
        {
            SecLabel("스테이지 레이아웃 SO");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Layout SO", GUILayout.Width(72));
            var prev = _layoutSO;
            _layoutSO = (StageLayoutSO)EditorGUILayout.ObjectField(
                _layoutSO, typeof(StageLayoutSO), false);
            // SO가 새로 연결될 때만 로드 (null→값 전환 시에만)
            if (_layoutSO != prev && _layoutSO != null && prev == null)
                LoadFromLayoutSO();
            EditorGUILayout.EndHorizontal();

            if (_layoutSO != null)
                EditorGUILayout.LabelField(
                    $"  {_layoutSO.entries.Count}개 항목  |  {AssetDatabase.GetAssetPath(_layoutSO)}",
                    EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.35f, 0.80f, 0.45f);
            if (GUILayout.Button("저장", GUILayout.Height(26)))
                SaveToLayoutSO();
            GUI.backgroundColor = new Color(0.55f, 0.75f, 1.0f);
            GUI.enabled = _layoutSO != null;
            if (GUILayout.Button("불러오기", GUILayout.Height(26)))
                LoadFromLayoutSO();
            GUI.enabled = true;
            GUI.backgroundColor = Color.white; // [FIX-7]
            EditorGUILayout.EndHorizontal();

            if (_layoutSO != null && _layoutSO.entries.Count > 0)
            {
                int show = Mathf.Min(_layoutSO.entries.Count, 5);
                for (int i = 0; i < show; i++)
                {
                    var e = _layoutSO.entries[i];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(8);
                    GUILayout.Label(
                        $"({e.cellIndex.x},{e.cellIndex.y})  {e.obstacleData?.DisplayName ?? e.obstacleData?.name ?? "?"}",
                        EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
                if (_layoutSO.entries.Count > 5)
                    GUILayout.Label($"  ...외 {_layoutSO.entries.Count - 5}개", EditorStyles.miniLabel);
            }
        }

        // ── 배치 목록 ──────────────────────────────────────────────────
        private void DrawPlacedSection()
        {
            string unsaved = HasUnsavedChanges ? "  [미저장]" : "";
            SecLabel($"배치된 장애물 ({_placed.Count}개){unsaved}");

            if (_placed.Count == 0)
            { GUILayout.Label("  없음", EditorStyles.miniLabel); return; }

            float viewH = Mathf.Min(_placed.Count * 22f + 6f, 130f);
            _placedScroll = EditorGUILayout.BeginScrollView(_placedScroll, GUILayout.Height(viewH));

            // [FIX-5] 루프 밖에서 제거
            Vector2Int toRemove = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var kv in _placed)
            {
                var go = EditorUtility.EntityIdToObject(kv.Value.goID) as GameObject;
                bool alive = go != null;

                EditorGUILayout.BeginHorizontal();
                GUI.color = alive ? Color.white : new Color(1f, 0.5f, 0.5f, 0.7f);
                GUILayout.Label(
                    $"  ({kv.Key.x},{kv.Key.y})  {kv.Value.data?.DisplayName ?? kv.Value.data?.name ?? "?"}{(alive ? "" : " [GO없음]")}",
                    EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                GUI.color = Color.white;

                if (alive && GUILayout.Button("O", GUILayout.Width(22), GUILayout.Height(18)))
                    Selection.activeGameObject = go;
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(18)))
                    toRemove = kv.Key;
                GUI.backgroundColor = Color.white; // [FIX-7]
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // [FIX-5] 루프 완전 종료 후 제거
            if (toRemove.x != int.MinValue) { RemovePlaced(toRemove); Repaint(); }
        }

        // ── 하단 ──────────────────────────────────────────────────────
        private void DrawFooter()
        {
            Divider();
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.48f, 0.48f);
            if (GUILayout.Button("전체 제거", GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog("전체 제거",
                    $"배치된 장애물 {_placed.Count}개를 모두 제거할까요?", "제거", "취소"))
                    ClearAll();
            }
            GUI.backgroundColor = Color.white; // [FIX-7]
            if (GUILayout.Button("팔레트 새로고침", GUILayout.Height(22)))
                RefreshPalette();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        // ─────────────────────────────────────────────────────────────
        //  SceneView GUI
        // ─────────────────────────────────────────────────────────────
        private void OnSceneGUI(SceneView view)
        {
            if (!_isPlacing || _gridManager == null || _grid == null) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event evt = Event.current;

            Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            Plane plane = new Plane(Vector3.back, Vector3.zero);
            if (!plane.Raycast(ray, out float dist)) return;

            Vector3 world = ray.GetPoint(dist);
            Vector3Int cellInt = _grid.WorldToCell(world);
            Vector2Int cell = new Vector2Int(
                cellInt.x - _gridManager.Offset.x,
                cellInt.y - _gridManager.Offset.y);

            // Repaint 이벤트 안에서 view.Repaint()를 재호출하면 중첩 렌더가 발생해 크래시
            // → MouseMove/Layout 등 비-Repaint 이벤트에서만 요청
            if (_hoveredCell != cell)
            {
                _hoveredCell = cell;
                if (evt.type != EventType.Repaint) view.Repaint();
            }

            if (evt.type == EventType.Repaint)
            {
                DrawGridOverlay(_grid);
                DrawHoverPreview(_grid, cell);
            }

            // MouseDown 전용 (Drag 오배치 방지)
            if (evt.type == EventType.MouseDown)
            {
                if (evt.button == 0)
                {
                    if (_isEraseMode) RemovePlaced(cell);
                    else PlaceObstacle(cell);
                    evt.Use();
                    Repaint();
                    view.Repaint();
                }
                else if (evt.button == 1 && !_isEraseMode)
                {
                    RemovePlaced(cell);
                    evt.Use();
                    Repaint();
                    view.Repaint();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  그리드 오버레이
        // ─────────────────────────────────────────────────────────────
        private void DrawGridOverlay(Grid g)
        {
            Vector2Int size = _gridManager.Size;
            Vector3 cellSize = g.cellSize;

            // Frustum culling: 씬 뷰 카메라 뷰포트 밖 셀은 Draw 생략
            // 대형 그리드(100×100 = 10,000 Draw calls)에서 발생하는 크래시 방지
            Camera cam = SceneView.currentDrawingSceneView?.camera;
            float marginX = cellSize.x * 2f;
            float marginY = cellSize.y * 2f;

            float halfCellY = cellSize.y * 0.5f;

            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                {
                    // CellToWorld는 셀 바닥 꼭짓점 → DrawDiamond 중심은 반 셀 위
                    Vector3 origin = _gridManager.CellToWorld(new Vector2Int(x, y));
                    Vector3 center = origin + new Vector3(0f, halfCellY, 0f);

                    if (cam != null)
                    {
                        Vector3 vp = cam.WorldToViewportPoint(center);
                        float vpMarginX = marginX / cam.pixelWidth;
                        float vpMarginY = marginY / cam.pixelHeight;
                        if (vp.z < 0 ||
                            vp.x < -vpMarginX || vp.x > 1f + vpMarginX ||
                            vp.y < -vpMarginY || vp.y > 1f + vpMarginY)
                            continue;
                    }

                    Vector2Int idx = new Vector2Int(x, y);
                    Color fill, border;
                    if (_placed.ContainsKey(idx)) { fill = CPlaced; border = CPlacedB; }
                    else if (_occupied.Contains(idx)) { fill = CBlocked; border = CBlockedB; }
                    else { fill = CGrid; border = CGridB; }

                    DrawDiamond(center, cellSize, fill, border);
                }
        }

        // ─────────────────────────────────────────────────────────────
        //  호버 프리뷰
        // ─────────────────────────────────────────────────────────────
        private void DrawHoverPreview(Grid g, Vector2Int cell)
        {
            if (!InGrid(cell)) return;
            float halfCellY = g.cellSize.y * 0.5f;
            Vector3 center = _gridManager.CellToWorld(cell) + new Vector3(0f, halfCellY, 0f);

            DrawDiamond(center, g.cellSize,
                _isEraseMode ? CErase : CHover,
                _isEraseMode ? CEraseB : CHoverB);

            if (SelectedOD == null || _isEraseMode) return;

            var selectedTsd = GetTileSetData(SelectedOD);
            if (selectedTsd?.BlockedOffsets != null)
                foreach (var bo in selectedTsd.BlockedOffsets)
                {
                    Vector2Int tc = cell + bo;
                    if (!InGrid(tc)) continue;
                    bool conflict = _occupied.Contains(tc) || _placed.ContainsKey(tc);
                    Vector3 tcCenter = _gridManager.CellToWorld(tc) + new Vector3(0f, halfCellY, 0f);
                    DrawDiamond(tcCenter, g.cellSize,
                        conflict ? CConflict : CBlocked,
                        conflict ? CConflictB : CBlockedB);
                }

            _hoverLabelStyle ??= new GUIStyle { normal = { textColor = Color.white }, fontSize = 9 };
            Handles.Label(center + Vector3.up * (g.cellSize.y * 0.75f), SelectedOD.DisplayName ?? SelectedOD.name, _hoverLabelStyle);
        }

        // ─────────────────────────────────────────────────────────────
        //  배치
        // ─────────────────────────────────────────────────────────────
        private void PlaceObstacle(Vector2Int cell)
        {
            if (SelectedOD == null || SelectedOD.WorkplacePrefab == null)
            {
                Debug.LogWarning("[ObstaclePlacer] 팔레트에서 장애물을 먼저 선택하세요.");
                return;
            }
            if (!InGrid(cell))
            {
                Debug.LogWarning($"[ObstaclePlacer] ({cell.x},{cell.y}) 그리드 범위 밖");
                return;
            }

            var fp = Footprint(cell, SelectedOD);
            foreach (var fc in fp)
            {
                if (!InGrid(fc))
                {
                    Debug.LogWarning($"[ObstaclePlacer] BlockedOffset ({fc.x},{fc.y}) 범위 밖");
                    return;
                }
                if (_occupied.Contains(fc))
                {
                    Debug.LogWarning($"[ObstaclePlacer] ({fc.x},{fc.y}) 이미 점유됨");
                    return;
                }
            }

            Vector3 worldPos = _gridManager.CellToWorld(cell);
            worldPos.z = 0f;

            var placedInstance = (TycoonObject)PrefabUtility.InstantiatePrefab(SelectedOD.WorkplacePrefab);
            GameObject go = placedInstance.gameObject;
            go.transform.position = worldPos;
            go.transform.SetParent(_gridManager.transform);
            Undo.RegisterCreatedObjectUndo(go, $"Place [{cell.x},{cell.y}]");

            _placed[cell] = (go.GetInstanceID(), SelectedOD);
            foreach (var fc in fp) _occupied.Add(fc);
        }

        // ─────────────────────────────────────────────────────────────
        //  제거
        // ─────────────────────────────────────────────────────────────
        private void RemovePlaced(Vector2Int cell)
        {
            if (!_placed.TryGetValue(cell, out var entry)) return;

            foreach (var fc in Footprint(cell, entry.data))
                _occupied.Remove(fc);

            var go = EditorUtility.EntityIdToObject(entry.goID) as GameObject;
            if (go != null) Undo.DestroyObjectImmediate(go);

            _placed.Remove(cell);
        }

        private void ClearAll()
        {
            foreach (var kv in _placed)
            {
                var go = EditorUtility.EntityIdToObject(kv.Value.goID) as GameObject;
                if (go != null) Undo.DestroyObjectImmediate(go);
            }
            _placed.Clear();
            _occupied.Clear();
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────
        //  SO 저장
        // ─────────────────────────────────────────────────────────────
        private void SaveToLayoutSO()
        {
            if (_placed.Count == 0)
            {
                EditorUtility.DisplayDialog("저장 실패", "배치된 장애물이 없습니다.", "확인");
                return;
            }

            if (_layoutSO == null)
            {
                if (!System.IO.Directory.Exists(DefaultSaveDir))
                {
                    System.IO.Directory.CreateDirectory(DefaultSaveDir);
                    AssetDatabase.Refresh();
                }
                string savePath = EditorUtility.SaveFilePanelInProject(
                    "StageLayoutSO 저장 위치 선택",
                    "StageLayout", "asset",
                    "저장할 폴더와 파일명을 입력하세요.",
                    DefaultSaveDir);
                if (string.IsNullOrEmpty(savePath)) return;

                _layoutSO = ScriptableObject.CreateInstance<StageLayoutSO>();
                AssetDatabase.CreateAsset(_layoutSO, savePath);
            }

            Undo.RecordObject(_layoutSO, "Save StageLayout");
            _layoutSO.entries.Clear();
            foreach (var kv in _placed)
            {
                _layoutSO.entries.Add(new PlacedObstacleEntry
                {
                    cellIndex = kv.Key,
                    obstacleData = kv.Value.data
                });
            }

            EditorUtility.SetDirty(_layoutSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // [FIX-8] ToHashSet() 없이 스냅샷 갱신
            _savedSnapshot.Clear();
            foreach (var k in _placed.Keys) _savedSnapshot.Add(k);

            EditorUtility.DisplayDialog("저장 완료",
                $"{_placed.Count}개 배치 정보를 저장했습니다.\n" +
                $"경로: {AssetDatabase.GetAssetPath(_layoutSO)}", "확인");

            EditorGUIUtility.PingObject(_layoutSO);
            Selection.activeObject = _layoutSO;
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────
        //  SO 불러오기
        // ─────────────────────────────────────────────────────────────
        private void LoadFromLayoutSO()
        {
            if (_layoutSO == null) return;
            if (_gridManager == null)
            {
                EditorUtility.DisplayDialog("로드 실패", "GridManager를 먼저 지정하세요.", "확인");
                return;
            }
            if (_placed.Count > 0)
            {
                if (!EditorUtility.DisplayDialog("기존 배치 초기화",
                    $"현재 배치 {_placed.Count}개를 제거하고 SO에서 불러올까요?",
                    "예", "취소"))
                    return;
                ClearAll();
            }

            int loaded = 0, skipped = 0;

            foreach (var entry in _layoutSO.entries)
            {
                if (entry.obstacleData == null || entry.obstacleData.WorkplacePrefab == null)
                { skipped++; continue; }

                Vector2Int cell = entry.cellIndex;
                if (!InGrid(cell)) { skipped++; continue; }

                var fp = Footprint(cell, entry.obstacleData);
                if (fp.Any(fc => !InGrid(fc) || _occupied.Contains(fc)))
                { skipped++; continue; }

                Vector3 worldPos = _gridManager.CellToWorld(cell);
                worldPos.z = 0f;

                var loadedInstance = (TycoonObject)PrefabUtility.InstantiatePrefab(
                    entry.obstacleData.WorkplacePrefab);
                GameObject go = loadedInstance.gameObject;
                go.transform.position = worldPos;
                go.transform.SetParent(_gridManager.transform);
                Undo.RegisterCreatedObjectUndo(go, $"Load [{cell.x},{cell.y}]");

                _placed[cell] = (go.GetInstanceID(), entry.obstacleData);
                foreach (var fc in fp) _occupied.Add(fc);
                loaded++;
            }

            // [FIX-8] ToHashSet() 없이 스냅샷 갱신
            _savedSnapshot.Clear();
            foreach (var k in _placed.Keys) _savedSnapshot.Add(k);

            Repaint();
            SceneView.RepaintAll();
            Debug.Log($"[ObstaclePlacer] 로드 완료: {loaded}개 성공 / {skipped}개 스킵");
        }

        // ─────────────────────────────────────────────────────────────
        //  헬퍼
        // ─────────────────────────────────────────────────────────────
        private static TileSetData GetTileSetData(ObjectDataSO od)
        {
            if (od?.WorkplacePrefab == null) return null;
            return od.WorkplacePrefab?.TileSetData;
        }

        private static List<Vector2Int> Footprint(Vector2Int origin, ObjectDataSO od)
        {
            var r = new List<Vector2Int> { origin };
            var tsd = GetTileSetData(od);
            if (tsd?.BlockedOffsets != null)
                foreach (var b in tsd.BlockedOffsets) r.Add(origin + b);
            return r;
        }

        private bool InGrid(Vector2Int idx)
            => _gridManager != null
            && idx.x >= 0 && idx.x < _gridManager.Size.x
            && idx.y >= 0 && idx.y < _gridManager.Size.y;

        private static void DrawDiamond(Vector3 c, Vector3 cs, Color fill, Color border)
        {
            float hw = cs.x * 0.5f, hh = cs.y * 0.5f;
            _diamondVerts[0] = new Vector3(c.x,      c.y + hh, c.z);
            _diamondVerts[1] = new Vector3(c.x + hw, c.y,      c.z);
            _diamondVerts[2] = new Vector3(c.x,      c.y - hh, c.z);
            _diamondVerts[3] = new Vector3(c.x - hw, c.y,      c.z);
            Handles.DrawSolidRectangleWithOutline(_diamondVerts, fill, border);
        }

        private static void Divider()
        {
            GUILayout.Space(4);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1f), new Color(1f, 1f, 1f, 0.08f));
            GUILayout.Space(4);
        }

        private static void SecLabel(string t)
        {
            _secLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
                { normal = { textColor = new Color(0.65f, 0.88f, 1f) } };
            GUILayout.Label(t, _secLabelStyle);
        }

        // [FIX-7] ModeBtn 종료 시 backgroundColor 복원
        private void ModeBtn(string label, bool active, Color col, System.Action onClick)
        {
            GUI.backgroundColor = active ? col : new Color(0.28f, 0.28f, 0.30f);
            if (GUILayout.Button(label, GUILayout.Height(26)))
            { onClick(); SceneView.RepaintAll(); }
            GUI.backgroundColor = Color.white;
        }
    }
}