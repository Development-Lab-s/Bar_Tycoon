#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using BBJ.GridSystem.Objects;
using UnityEditor;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Editor
{
    public class WorkplaceSetupWindow : EditorWindow
    {
        // ── 상수 ──────────────────────────────────────────────────────
        private const string MenuPath = "Tools/Workplace Setup";
        private const float  CanvasH  = 320f;
        private const float  TileBase = 48f;
        private const int    RangeMin = 1;
        private const int    RangeMax = 8;

        // ── 페인트 모드 ────────────────────────────────────────────────
        private enum PaintMode { Blocked, Role, Eraser }
        private PaintMode _paintMode     = PaintMode.Blocked;
        private int       _activeRoleIdx;

        // ── 프리팹 상태 ────────────────────────────────────────────────
        private TycoonObject _prefab;
        private string       _prefabPath;
        private TileSetData  _tileSetData;
        private bool         _hasVisualRoot;
        private bool         _isWorkplace;

        // ── 편집 데이터 ─ Blocked ──────────────────────────────────────
        private bool                _isWalkable;
        private HashSet<Vector2Int> _blocked = new();

        // ── 편집 데이터 ─ InteractPoints (role → 셀 집합) ─────────────
        private Dictionary<InteractRoleSO, HashSet<Vector2Int>> _rolePoints = new();

        // ── 비주얼 오프셋 ──────────────────────────────────────────────
        private Vector3 _visualOffset;
        private float   _cellSize = 0.5f;

        // ── Role 목록 (프로젝트 스캔) ──────────────────────────────────
        private List<InteractRoleSO> _roles = new();

        // ── 색상 팔레트 캐시 (hash → Color) ───────────────────────────
        private Dictionary<int, Color> _colorCache = new();
        private Dictionary<Vector2Int, List<InteractRoleSO>> _cellRoleMap = new();

        // ── 캔버스 상태 ────────────────────────────────────────────────
        private Vector2    _panOffset;
        private float      _zoom = 1f;
        private bool       _isPanning;
        private Vector2    _panStartMouse, _panStartOffset;
        private bool       _isDragPainting;
        private bool       _dragErasing;
        private Vector2Int _lastDragCell = new(int.MinValue, int.MinValue);
        private Vector2Int _hovered      = new(int.MinValue, int.MinValue);

        // ── 스프라이트 드래그 ──────────────────────────────────────────
        private bool    _isDraggingSprite;
        private Vector2 _spriteDragStart;
        private Vector3 _spriteDragStartOffset;

        // ── 뷰 범위 ────────────────────────────────────────────────────
        private int _vMinX = -3, _vMaxX = 3, _vMinY = -3, _vMaxY = 3;

        // ── 스크롤 ─────────────────────────────────────────────────────
        private Vector2 _mainScroll;

        // ── 파생값 ─────────────────────────────────────────────────────
        private float TileW => TileBase * _zoom;
        private float TileH => TileBase * 0.5f * _zoom;

        // ── 고정 색상 ──────────────────────────────────────────────────
        private static readonly Color ColEmpty        = new(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Color ColBorderEmpty  = new(0.35f, 0.38f, 0.44f, 1f);
        private static readonly Color ColOrigin       = new(0.98f, 0.78f, 0.46f, 0.95f);
        private static readonly Color ColOriginBorder = new(0.93f, 0.62f, 0.09f, 1f);
        private static readonly Color ColBlocked      = new(0.80f, 0.22f, 0.22f, 0.90f);
        private static readonly Color ColBlockedB     = new(1.00f, 0.40f, 0.40f, 1f);
        private static readonly Color ColHoverBlock   = new(1.00f, 0.45f, 0.45f, 0.35f);
        private static readonly Color ColHoverErase   = new(1.00f, 0.85f, 0.20f, 0.35f);

        // ── 텍스처 ─────────────────────────────────────────────────────
        private Texture2D _diamondFillTex;
        private Texture2D _diamondBorderTex;

        // ── 스프라이트 프리뷰 ──────────────────────────────────────────
        private struct SpriteLayer { public Sprite Sprite; public Vector2 LocalOffset; }
        private readonly List<SpriteLayer> _spriteLayers = new();

        // ── GUIStyle 캐시 ──────────────────────────────────────────────
        private GUIStyle _tileLabelStyle;
        private GUIStyle _hintLabelStyle;

        // ── 생명주기 ───────────────────────────────────────────────────
        private void OnEnable()
        {
            const int W = 64, H = 32;
            _diamondFillTex   = BuildDiamondFillTex(W, H);
            _diamondBorderTex = BuildDiamondBorderTex(W, H, 2.5f);
            _hintLabelStyle   = new GUIStyle(EditorStyles.miniLabel);
            _hintLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
        }

        private void OnDisable()
        {
            DestroyImmediate(_diamondFillTex);
            DestroyImmediate(_diamondBorderTex);
            _diamondFillTex = _diamondBorderTex = null;
        }

        // ── 다이아몬드 텍스처 빌드 ─────────────────────────────────────
        private static Texture2D BuildDiamondFillTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs(x - cx) / (w * 0.5f);
                    float ny = Mathf.Abs(y - cy) / (h * 0.5f);
                    byte a = (byte)(Mathf.Clamp01(1f - (nx + ny - 1f) * (w * 0.25f)) * 255);
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildDiamondBorderTex(int w, int h, float thickness)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float t = thickness / (w * 0.5f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs(x - cx) / (w * 0.5f);
                    float ny = Mathf.Abs(y - cy) / (h * 0.5f);
                    float d = nx + ny;
                    float outer = Mathf.Clamp01(1f - (d - 1f) * (w * 0.25f));
                    float inner = Mathf.Clamp01(((1f - t) - d) * (w * 0.25f));
                    byte a = (byte)(Mathf.Clamp01(outer - inner) * 255);
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── 메뉴 ───────────────────────────────────────────────────────
        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var w = GetWindow<WorkplaceSetupWindow>("Workplace Setup");
            w.minSize = new Vector2(400f, 700f);
        }

        // ── GUI 진입 ───────────────────────────────────────────────────
        private void OnGUI()
        {
            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
            DrawPrefabSection();
            if (_prefab != null)
            {
                Divider();
                DrawToolbar();
                Divider();
                DrawCanvas();
                Divider();
                DrawVisualOffsetSection();
                Divider();
                DrawTileSetSection();
                if (_isWorkplace)
                {
                    Divider();
                    DrawInteractPointSection();
                }
                Divider();
                DrawSaveSection();
            }
            EditorGUILayout.EndScrollView();
        }

        // ── 프리팹 섹션 ────────────────────────────────────────────────
        private void DrawPrefabSection()
        {
            EditorGUILayout.LabelField("Workplace Setup", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            var prev = _prefab;
            _prefab = (TycoonObject)EditorGUILayout.ObjectField(
                "Prefab", _prefab, typeof(TycoonObject), false);
            if (_prefab != prev && _prefab != null) LoadFromPrefab();
            if (GUILayout.Button("불러오기", GUILayout.Width(60)) && _prefab != null)
                LoadFromPrefab();
            if (GUILayout.Button("선택 불러오기", GUILayout.Width(90)))
            {
                var go     = Selection.activeGameObject ?? (Selection.activeObject as GameObject);
                var tycoon = go != null ? go.GetComponent<TycoonObject>() : null;
                if (tycoon != null) { _prefab = tycoon; LoadFromPrefab(); }
                else EditorUtility.DisplayDialog(
                    "알림", "Project 창에서 TycoonObject 프리팹을 선택하세요.", "확인");
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_prefabPath))
                EditorGUILayout.LabelField(_prefabPath, EditorStyles.miniLabel);

            if (_prefab != null)
            {
                var preview = AssetPreview.GetAssetPreview(_prefab.gameObject);
                if (preview == null)
                {
                    if (AssetPreview.IsLoadingAssetPreview(_prefab.gameObject.GetInstanceID()))
                    { EditorGUILayout.LabelField("프리뷰 로딩 중...", EditorStyles.miniLabel); Repaint(); }
                }
                else
                {
                    Rect r = GUILayoutUtility.GetRect(80f, 80f);
                    r.x = (EditorGUIUtility.currentViewWidth - 80f) * 0.5f;
                    GUI.DrawTexture(r, preview, ScaleMode.ScaleToFit);
                }
            }
        }

        // ── 툴바 ───────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.LabelField("페인트 모드", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                ToolBtn("Blocked", _paintMode == PaintMode.Blocked, ColBlocked,
                    () => _paintMode = PaintMode.Blocked);

                if (_isWorkplace)
                {
                    for (int i = 0; i < _roles.Count; i++)
                    {
                        int captured = i;
                        var role     = _roles[i];
                        bool active  = _paintMode == PaintMode.Role && _activeRoleIdx == i;
                        Color rc     = _colorCache.TryGetValue(role.GetHashCode(), out var cc)
                                       ? cc : Color.cyan;
                        ToolBtn(role.name, active, rc, () =>
                        {
                            _paintMode     = PaintMode.Role;
                            _activeRoleIdx = captured;
                        });
                    }
                }

                ToolBtn("지우개", _paintMode == PaintMode.Eraser,
                    new Color(1f, 0.85f, 0.25f),
                    () => _paintMode = PaintMode.Eraser);

                GUI.backgroundColor = Color.white;
            }

            string hint = _paintMode == PaintMode.Eraser
                ? "좌클릭/드래그: 모든 데이터 제거  |  우클릭 드래그: 패닝"
                : _isWorkplace && _paintMode == PaintMode.Role && _roles.Count == 0
                    ? "InteractRoleSO 에셋 없음 (Create > Tycoon > Workplace > InteractRole)"
                    : "좌클릭/드래그: 토글  |  Shift+드래그: 스프라이트 이동  |  우클릭 드래그: 패닝";
            EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);
        }

        private void ToolBtn(string label, bool active, Color activeCol, System.Action onClick)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = active ? activeCol : new Color(0.28f, 0.28f, 0.30f);
            if (GUILayout.Button(label, GUILayout.Height(24))) onClick();
            GUI.backgroundColor = prev;
        }

        // ── InteractPoint 요약 섹션 ────────────────────────────────────
        private void DrawInteractPointSection()
        {
            EditorGUILayout.LabelField("InteractPoints", EditorStyles.boldLabel);
            if (_roles.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "프로젝트에 InteractRoleSO가 없습니다.", MessageType.Info);
                return;
            }
            foreach (var role in _roles)
            {
                int cnt = _rolePoints.TryGetValue(role, out var set) ? set.Count : 0;
                EditorGUILayout.LabelField($"  {role.name}: {cnt}개", EditorStyles.miniLabel);
            }
            if (GUILayout.Button("Role 새로고침", GUILayout.Width(100)))
            {
                RefreshRoles();
                BuildColorPalette();
                Repaint();
            }
        }

        // ── 프리팹 로드 ────────────────────────────────────────────────
        private void LoadFromPrefab()
        {
            _isDragPainting   = false;
            _isPanning        = false;
            _isDraggingSprite = false;
            _lastDragCell     = new Vector2Int(int.MinValue, int.MinValue);

            _prefabPath    = AssetDatabase.GetAssetPath(_prefab);
            _tileSetData   = _prefab.TileSetData;
            _isWalkable    = _tileSetData?.IsWalkable ?? true;
            _isWorkplace   = _prefab is Workplace;

            _blocked.Clear();
            if (_tileSetData?.BlockedOffsets != null)
                foreach (var v in _tileSetData.BlockedOffsets)
                    _blocked.Add(v);

            var visualRoot = _prefab.GetComponentInChildren<TycoonVisualRoot>();
            _hasVisualRoot = visualRoot != null;
            _visualOffset  = _hasVisualRoot ? visualRoot.transform.localPosition : Vector3.zero;

            var rootTr = _hasVisualRoot ? visualRoot.transform : _prefab.transform;
            _spriteLayers.Clear();
            foreach (var sr in rootTr.GetComponentsInChildren<SpriteRenderer>()
                                     .OrderBy(s => s.sortingLayerID).ThenBy(s => s.sortingOrder))
            {
                if (sr.sprite == null) continue;
                Vector3 rel = sr.transform.position - rootTr.position;
                _spriteLayers.Add(new SpriteLayer
                    { Sprite = sr.sprite, LocalOffset = new Vector2(rel.x, rel.y) });
            }

            RefreshRoles();
            BuildColorPalette();
            LoadInteractPointsFromPrefab();

            _panOffset = Vector2.zero;
            Repaint();
        }

        // ── Role 스캔 (프로젝트 전체) ──────────────────────────────────
        private void RefreshRoles()
        {
            _roles.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:InteractRoleSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var role = AssetDatabase.LoadAssetAtPath<InteractRoleSO>(path);
                if (role != null) _roles.Add(role);
            }
        }

        // ── 색상 팔레트 빌드 ───────────────────────────────────────────
        private void BuildColorPalette()
        {
            _colorCache.Clear();
            foreach (var role in _roles)
                _colorCache[role.GetHashCode()] = role.GizmoColor;

            _cellRoleMap.Clear();
            foreach (var kvp in _rolePoints)
                foreach (var cell in kvp.Value)
                {
                    if (!_cellRoleMap.TryGetValue(cell, out var list))
                        _cellRoleMap[cell] = list = new List<InteractRoleSO>();
                    list.Add(kvp.Key);
                }

            foreach (var kvp in _cellRoleMap)
            {
                if (kvp.Value.Count < 2) continue;
                int h = CombineRoleHash(kvp.Value);
                if (_colorCache.ContainsKey(h)) continue;
                _colorCache[h] = BlendColors(kvp.Value);
            }
        }

        private static int CombineRoleHash(List<InteractRoleSO> roles)
        {
            int h = 17;
            foreach (var r in roles.OrderBy(r => r.GetHashCode()))
                h = h * 31 + r.GetHashCode();
            return h;
        }

        private Color BlendColors(List<InteractRoleSO> roles)
        {
            var avg = Color.black;
            foreach (var r in roles)
                avg += _colorCache.TryGetValue(r.GetHashCode(), out var c) ? c : Color.cyan;
            avg   /= roles.Count;
            avg.a  = 0.9f;
            return avg;
        }

        // ── 셀 색상 조회 ───────────────────────────────────────────────
        private (Color fill, Color border) GetCellColor(Vector2Int cell)
        {
            if (_blocked.Contains(cell))
                return (ColBlocked, ColBlockedB);

            if (!_cellRoleMap.TryGetValue(cell, out var rolesOnCell) || rolesOnCell.Count == 0)
                return (ColEmpty, ColBorderEmpty);

            Color baseColor;
            if (rolesOnCell.Count == 1)
            {
                baseColor = _colorCache.TryGetValue(rolesOnCell[0].GetHashCode(), out var c)
                            ? c : Color.cyan;
            }
            else
            {
                int h = CombineRoleHash(rolesOnCell);
                if (!_colorCache.TryGetValue(h, out baseColor))
                {
                    baseColor      = BlendColors(rolesOnCell);
                    _colorCache[h] = baseColor;
                }
            }

            var border = baseColor;
            border.r   = Mathf.Min(border.r + 0.2f, 1f);
            border.g   = Mathf.Min(border.g + 0.2f, 1f);
            border.b   = Mathf.Min(border.b + 0.2f, 1f);
            border.a   = 1f;
            return (baseColor, border);
        }

        // ── 페인트 셀 ──────────────────────────────────────────────────
        private void PaintCell(Vector2Int coord, bool erasing)
        {
            switch (_paintMode)
            {
                case PaintMode.Blocked:
                    if (coord == Vector2Int.zero) return;
                    if (erasing)
                    {
                        _blocked.Remove(coord);
                    }
                    else
                    {
                        foreach (var set in _rolePoints.Values) set.Remove(coord);
                        _blocked.Add(coord);
                    }
                    break;

                case PaintMode.Role:
                    if (_roles.Count == 0 || _activeRoleIdx >= _roles.Count) return;
                    var role = _roles[_activeRoleIdx];
                    if (!_rolePoints.TryGetValue(role, out var roleSet))
                    {
                        roleSet            = new HashSet<Vector2Int>();
                        _rolePoints[role]  = roleSet;
                    }
                    if (erasing)
                    {
                        roleSet.Remove(coord);
                    }
                    else
                    {
                        _blocked.Remove(coord);
                        roleSet.Add(coord);
                    }
                    break;

                case PaintMode.Eraser:
                    _blocked.Remove(coord);
                    foreach (var set in _rolePoints.Values) set.Remove(coord);
                    break;
            }
            BuildColorPalette();
            Repaint();
        }

        private bool IsErasing(Vector2Int coord)
        {
            if (_paintMode == PaintMode.Blocked) return _blocked.Contains(coord);
            if (_paintMode == PaintMode.Eraser)  return false;
            if (_roles.Count == 0 || _activeRoleIdx >= _roles.Count) return false;
            var role = _roles[_activeRoleIdx];
            return _rolePoints.TryGetValue(role, out var s) && s.Contains(coord);
        }

        // ── InteractPoint 로드 ─────────────────────────────────────────
        private void LoadInteractPointsFromPrefab()
        {
            _rolePoints.Clear();
            if (!_isWorkplace || string.IsNullOrEmpty(_prefabPath)) return;

            var contents = PrefabUtility.LoadPrefabContents(_prefabPath);
            try
            {
                var workplace = contents.GetComponent<Workplace>();
                if (workplace == null) return;

                var so   = new SerializedObject(workplace);
                var prop = so.FindProperty("_interactPoints");
                if (prop == null || !prop.isArray) return;

                for (int i = 0; i < prop.arraySize; i++)
                {
                    var elem   = prop.GetArrayElementAtIndex(i);
                    var offset = elem.FindPropertyRelative("Offset").vector2IntValue;
                    var role   = elem.FindPropertyRelative("Role").objectReferenceValue
                                 as InteractRoleSO;
                    if (role == null) continue;

                    if (!_rolePoints.TryGetValue(role, out var set))
                    {
                        set               = new HashSet<Vector2Int>();
                        _rolePoints[role] = set;
                    }
                    set.Add(offset);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }

            BuildColorPalette();
        }

        // ── 저장 섹션 ─────────────────────────────────────────────────
        private void DrawSaveSection()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.35f, 0.80f, 0.45f);
            if (GUILayout.Button("저장", GUILayout.Height(28)))
                SaveToPrefab(_prefabPath);
            GUI.backgroundColor = new Color(0.55f, 0.75f, 1f);
            if (GUILayout.Button("다른 이름으로 저장...", GUILayout.Height(28)))
                SaveAs();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void SaveToPrefab(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("오류", "프리팹 경로가 없습니다.", "확인");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var tileSet = contents.GetComponent<TycoonObject>()?.TileSetData;
                if (tileSet != null)
                {
                    tileSet.BlockedOffsets = _blocked.ToArray();
                    tileSet.IsWalkable     = _isWalkable;
                    EditorUtility.SetDirty(tileSet);
                }

                var visualRoot = contents.GetComponentInChildren<TycoonVisualRoot>();
                if (visualRoot != null)
                    visualRoot.transform.localPosition = _visualOffset;

                var workplace = contents.GetComponent<Workplace>();
                if (workplace != null)
                {
                    var so   = new SerializedObject(workplace);
                    var prop = so.FindProperty("_interactPoints");
                    if (prop != null && prop.isArray)
                    {
                        prop.ClearArray();
                        int idx = 0;
                        foreach (var kvp in _rolePoints)
                        {
                            if (kvp.Key == null) continue;
                            foreach (var cell in kvp.Value
                                         .OrderBy(v => v.y).ThenBy(v => v.x))
                            {
                                prop.InsertArrayElementAtIndex(idx);
                                var elem = prop.GetArrayElementAtIndex(idx);
                                elem.FindPropertyRelative("Offset").vector2IntValue    = cell;
                                elem.FindPropertyRelative("Role").objectReferenceValue = kvp.Key;
                                idx++;
                            }
                        }
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(workplace);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }

            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(_prefab);
            EditorUtility.DisplayDialog("저장 완료", $"저장됨:\n{path}", "확인");
        }

        private void SaveAs()
        {
            string newPath = EditorUtility.SaveFilePanelInProject(
                "프리팹 저장 위치 선택", "NewWorkplace", "prefab", "저장할 경로를 선택하세요.");
            if (string.IsNullOrEmpty(newPath)) return;

            if (!string.IsNullOrEmpty(_prefabPath))
                AssetDatabase.CopyAsset(_prefabPath, newPath);

            _prefabPath = newPath;
            SaveToPrefab(_prefabPath);
            _prefab = AssetDatabase.LoadAssetAtPath<TycoonObject>(newPath);
            LoadFromPrefab();
        }

        // ── 캔버스 ─────────────────────────────────────────────────────
        private void DrawCanvas()
        {
            EditorGUILayout.LabelField("타일 뷰 범위", EditorStyles.miniBoldLabel);
            DrawRangeRow("X", ref _vMinX, ref _vMaxX);
            DrawRangeRow("Y", ref _vMinY, ref _vMaxY);

            Rect canvasRect  = GUILayoutUtility.GetRect(0, CanvasH);
            canvasRect.x     = 0;
            canvasRect.width = EditorGUIUtility.currentViewWidth - 4f;
            EditorGUI.DrawRect(canvasRect, new Color(0.13f, 0.13f, 0.16f, 1f));

            Event e        = Event.current;
            bool  inCanvas = canvasRect.Contains(e.mousePosition);
            Rect  local    = new Rect(0, 0, canvasRect.width, canvasRect.height);
            bool  shiftHeld = e.shift;

            if (inCanvas && e.type == EventType.ScrollWheel)
            {
                float prev = _zoom;
                _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.05f, 0.3f, 3f);
                Vector2 ml = e.mousePosition - new Vector2(
                    canvasRect.x + canvasRect.width  * 0.5f,
                    canvasRect.y + canvasRect.height * 0.5f);
                _panOffset = ml + (_panOffset - ml) * (_zoom / prev);
                e.Use(); Repaint();
            }

            if (inCanvas && e.type == EventType.MouseDown && e.button == 1)
            {
                _isPanning      = true;
                _panStartMouse  = e.mousePosition;
                _panStartOffset = _panOffset;
                e.Use();
            }
            if (_isPanning)
            {
                if (e.type == EventType.MouseDrag && e.button == 1)
                { _panOffset = _panStartOffset + (e.mousePosition - _panStartMouse); e.Use(); Repaint(); }
                if (e.type == EventType.MouseUp && e.button == 1)
                { _isPanning = false; e.Use(); }
            }

            if (inCanvas && e.type == EventType.MouseDown && e.button == 0
                && shiftHeld && _spriteLayers.Count > 0)
            {
                _isDraggingSprite      = true;
                _spriteDragStart       = e.mousePosition;
                _spriteDragStartOffset = _visualOffset;
                e.Use();
            }
            if (_isDraggingSprite)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    Vector2 delta       = e.mousePosition - _spriteDragStart;
                    float   unitToPixel = TileBase * _zoom / _cellSize;
                    _visualOffset = _spriteDragStartOffset + new Vector3(
                        delta.x / unitToPixel,
                        -delta.y / unitToPixel,
                        0f);
                    e.Use(); Repaint();
                }
                if (e.type == EventType.MouseUp && e.button == 0)
                { _isDraggingSprite = false; e.Use(); }
            }

            if (inCanvas && e.type == EventType.MouseDown && e.button == 0 && !shiftHeld)
            {
                Vector2 lm = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                if (ScreenToGrid(lm, local, out Vector2Int hv))
                {
                    _dragErasing    = IsErasing(hv);
                    _isDragPainting = true;
                    _lastDragCell   = hv;
                    PaintCell(hv, _dragErasing);
                }
                e.Use();
            }
            if (_isDragPainting)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    Vector2 lm = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                    if (ScreenToGrid(lm, local, out Vector2Int hv) && hv != _lastDragCell)
                    { PaintCell(hv, _dragErasing); _lastDragCell = hv; }
                    Repaint(); e.Use();
                }
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    _isDragPainting = false;
                    _lastDragCell   = new Vector2Int(int.MinValue, int.MinValue);
                    e.Use();
                }
            }

            if (!_isPanning && !_isDraggingSprite &&
                (e.type == EventType.MouseMove ||
                 (e.type == EventType.MouseDrag && e.button == 0)))
            {
                _hovered = inCanvas
                    && ScreenToGrid(
                        e.mousePosition - new Vector2(canvasRect.x, canvasRect.y),
                        local, out Vector2Int h)
                    ? h : new Vector2Int(int.MinValue, int.MinValue);
                Repaint();
            }

            if (e.type == EventType.Repaint)
            {
                GUI.BeginClip(canvasRect);

                int tileFontSize = Mathf.RoundToInt(Mathf.Clamp(9f * _zoom, 7f, 12f));
                if (_tileLabelStyle == null)
                {
                    _tileLabelStyle = new GUIStyle(EditorStyles.label)
                        { alignment = TextAnchor.MiddleCenter };
                    _tileLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.65f);
                }
                _tileLabelStyle.fontSize = tileFontSize;

                for (int gy = _vMinY; gy <= _vMaxY; gy++)
                    for (int gx = _vMinX; gx <= _vMaxX; gx++)
                    {
                        var     coord = new Vector2Int(gx, gy);
                        Vector2 ctr   = GridToScreen(gx, gy, local);
                        if (ctr.x < -TileW || ctr.x > local.width  + TileW ||
                            ctr.y < -TileH || ctr.y > local.height + TileH) continue;

                        bool isOrig = coord == Vector2Int.zero;
                        bool isHov  = coord == _hovered && !isOrig;

                        Color fill, border;
                        if (isOrig) { fill = ColOrigin; border = ColOriginBorder; }
                        else        (fill, border) = GetCellColor(coord);

                        DrawDiamond(ctr, fill, border);

                        if (isHov)
                        {
                            Color hc = _paintMode == PaintMode.Eraser
                                       ? ColHoverErase : ColHoverBlock;
                            DrawDiamond(ctr, hc, Color.clear);
                        }

                        if (TileW >= 28f)
                        {
                            string lbl = isOrig ? "0,0" : $"{gx},{gy}";
                            GUI.Label(
                                new Rect(ctr.x - TileW * 0.5f, ctr.y - TileH * 0.5f,
                                         TileW, TileH),
                                lbl, _tileLabelStyle);
                        }
                    }

                foreach (var layer in _spriteLayers)
                    DrawSpriteOnCanvas(layer.Sprite, layer.LocalOffset, local);

                DrawAnchorMarker(local);

                string hintText = _isDraggingSprite
                    ? "Shift+드래그: 스프라이트 이동 중..."
                    : "좌클릭: 토글  |  Shift+드래그: 스프라이트 이동  |  우클릭 드래그: 패닝  |  스크롤: 줌";
                GUI.Label(new Rect(8, 6, 420, 18), hintText, _hintLabelStyle);

                GUI.EndClip();
            }
        }

        // ── 스프라이트 렌더 ────────────────────────────────────────────
        private void DrawSpriteOnCanvas(Sprite sprite, Vector2 localOffset, Rect canvas)
        {
            var tex    = sprite.texture;
            var tr     = sprite.textureRect;
            var uvRect = new Rect(
                tr.x / tex.width,     tr.y / tex.height,
                tr.width / tex.width, tr.height / tex.height);

            float unitToPixel = TileBase * _zoom / _cellSize;
            float w    = tr.width  / sprite.pixelsPerUnit * unitToPixel;
            float h    = tr.height / sprite.pixelsPerUnit * unitToPixel;
            float pivX = sprite.pivot.x / sprite.pixelsPerUnit * unitToPixel;
            float pivY = sprite.pivot.y / sprite.pixelsPerUnit * unitToPixel;

            Vector2 diamondCenter = GridToScreen(0, 0, canvas);
            Vector2 bottomVertex  = new Vector2(diamondCenter.x, diamondCenter.y + TileH * 0.5f);

            float ox = (_visualOffset.x + localOffset.x) * unitToPixel;
            float oy = -(_visualOffset.y + localOffset.y) * unitToPixel;

            var drawRect = new Rect(
                bottomVertex.x + ox - pivX,
                bottomVertex.y + oy - (h - pivY),
                w, h);

            var prev  = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTextureWithTexCoords(drawRect, tex, uvRect, true);
            GUI.color = prev;
        }

        // ── 앵커 마커 ──────────────────────────────────────────────────
        private void DrawAnchorMarker(Rect canvas)
        {
            Vector2 center       = GridToScreen(0, 0, canvas);
            Vector2 bottomVertex = new Vector2(center.x, center.y + TileH * 0.5f);
            const float r        = 5f;

            var prev  = GUI.color;
            GUI.color = new Color(1f, 0.9f, 0.3f, 0.9f);
            GUI.DrawTexture(
                new Rect(bottomVertex.x - r,   bottomVertex.y - 1f, r * 2f, 2f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(bottomVertex.x - 1f, bottomVertex.y - r,   2f, r * 2f),
                Texture2D.whiteTexture);
            GUI.color = prev;
        }

        // ── 좌표 변환 ──────────────────────────────────────────────────
        private Vector2 GridToScreen(int gx, int gy, Rect canvas)
        {
            float midGx  = (_vMinX + _vMaxX) * 0.5f;
            float midGy  = (_vMinY + _vMaxY) * 0.5f;
            float pivotX = canvas.x + canvas.width  * 0.5f
                           - (midGx - midGy) * (TileW * 0.5f) + _panOffset.x;
            float pivotY = canvas.y + canvas.height * 0.5f
                           + (midGx + midGy) * (TileH * 0.5f) + _panOffset.y;
            return new Vector2(
                pivotX + (gx - gy) * (TileW * 0.5f),
                pivotY - (gx + gy) * (TileH * 0.5f));
        }

        private bool ScreenToGrid(Vector2 mouse, Rect canvas, out Vector2Int cell)
        {
            float best = float.MaxValue;
            cell  = default;
            bool found = false;
            for (int gy = _vMinY; gy <= _vMaxY; gy++)
                for (int gx = _vMinX; gx <= _vMaxX; gx++)
                {
                    Vector2 ctr = GridToScreen(gx, gy, canvas);
                    float dx    = Mathf.Abs(mouse.x - ctr.x) / (TileW * 0.5f);
                    float dy    = Mathf.Abs(mouse.y - ctr.y) / (TileH * 0.5f);
                    if (dx + dy <= 1f)
                    {
                        float d = (mouse - ctr).sqrMagnitude;
                        if (d < best)
                        { best = d; cell = new Vector2Int(gx, gy); found = true; }
                    }
                }
            return found;
        }

        private void DrawDiamond(Vector2 c, Color fill, Color border)
        {
            float hw  = TileW * 0.5f, hh = TileH * 0.5f;
            var   r   = new Rect(c.x - hw, c.y - hh, TileW, TileH);
            var   pc  = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(r, _diamondFillTex);
            if (border.a > 0.01f)
            {
                GUI.color = border;
                GUI.DrawTexture(r, _diamondBorderTex);
            }
            GUI.color = pc;
        }

        // ── Visual Offset 섹션 ─────────────────────────────────────────
        private void DrawVisualOffsetSection()
        {
            EditorGUILayout.LabelField(
                "Visual Offset  (TycoonVisualRoot localPosition)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            Vector3 newOff = EditorGUILayout.Vector3Field("Offset", _visualOffset);
            if (EditorGUI.EndChangeCheck()) { _visualOffset = newOff; Repaint(); }
            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            { _visualOffset = Vector3.zero; Repaint(); }
            if (!_hasVisualRoot)
                EditorGUILayout.HelpBox(
                    "이 프리팹에 TycoonVisualRoot 컴포넌트가 없습니다.\n" +
                    "비주얼 자식 GO에 컴포넌트를 부착하세요.", MessageType.Warning);

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Cell Size (world units)", GUILayout.Width(160));
                EditorGUI.BeginChangeCheck();
                float newSize = EditorGUILayout.FloatField(_cellSize, GUILayout.Width(60));
                if (EditorGUI.EndChangeCheck() && newSize > 0f) { _cellSize = newSize; Repaint(); }
                if (GUILayout.Button("기본값 (0.5)", GUILayout.Width(90)))
                { _cellSize = 0.5f; Repaint(); }
            }
        }

        // ── TileSet 섹션 ───────────────────────────────────────────────
        private void DrawTileSetSection()
        {
            EditorGUILayout.LabelField("TileSet  (BlockedOffsets)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _isWalkable = EditorGUILayout.Toggle("IsWalkable", _isWalkable);
            if (EditorGUI.EndChangeCheck()) Repaint();

            string blkText = _blocked.Count == 0
                ? "BlockedOffsets: (없음)"
                : "BlockedOffsets: " + string.Join("  ",
                    _blocked.OrderBy(v => v.y).ThenBy(v => v.x)
                            .Select(v => $"({v.x},{v.y})"));
            EditorGUILayout.LabelField(blkText, EditorStyles.miniLabel);

            if (_tileSetData == null)
                EditorGUILayout.HelpBox(
                    "TycoonObject에 TileSetData SO가 없습니다. BlockedOffsets 저장 불가.",
                    MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            var pc    = GUI.color;
            GUI.color = new Color(1f, 0.6f, 0.6f, 1f);
            if (GUILayout.Button("점유 초기화", GUILayout.Width(80)))
                if (EditorUtility.DisplayDialog(
                    "초기화", "모든 점유 타일을 삭제하시겠습니까?", "삭제", "취소"))
                { _blocked.Clear(); Repaint(); }
            GUI.color = pc;
            EditorGUILayout.EndHorizontal();
        }

        // ── 범위 행 ────────────────────────────────────────────────────
        private void DrawRangeRow(string axis, ref int min, ref int max)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    axis, EditorStyles.miniBoldLabel, GUILayout.Width(14));
                GUILayout.Label("음:", EditorStyles.miniLabel, GUILayout.Width(24));
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(17))
                    && min > -RangeMax) { min--; Repaint(); }
                GUILayout.Label(min.ToString(), EditorStyles.miniLabel, GUILayout.Width(20));
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(17))
                    && min < -RangeMin) { min++; Repaint(); }
                GUILayout.Space(10);
                GUILayout.Label("양:", EditorStyles.miniLabel, GUILayout.Width(24));
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(17))
                    && max > RangeMin) { max--; Repaint(); }
                GUILayout.Label(max.ToString(), EditorStyles.miniLabel, GUILayout.Width(20));
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(17))
                    && max < RangeMax) { max++; Repaint(); }
                GUILayout.FlexibleSpace();
            }
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────
        private static void Divider()
        {
            GUILayout.Space(4);
            EditorGUI.DrawRect(
                GUILayoutUtility.GetRect(0, 1f), new Color(1f, 1f, 1f, 0.08f));
            GUILayout.Space(4);
        }
    }
}
#endif
