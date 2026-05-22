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
        private const float CanvasH = 320f;
        private const float TileBase = 48f;
        private const int RangeMin = 1;
        private const int RangeMax = 8;

        // ── 프리팹 상태 ────────────────────────────────────────────────
        private TycoonObject _prefab;
        private string _prefabPath;
        private TileSetData _tileSetData;

        // ── 편집 데이터 ────────────────────────────────────────────────
        private bool _isWalkable;
        private HashSet<Vector2Int> _blocked = new();
        private Vector3 _visualOffset;
        private bool _hasVisualRoot;

        // ── 캔버스 상태 ────────────────────────────────────────────────
        private Vector2 _panOffset;
        private float _zoom = 1f;
        private bool _isPanning;
        private Vector2 _panStartMouse, _panStartOffset;
        private bool _isDragPainting;
        private bool _dragErasing;
        private Vector2Int _lastDragCell = new(int.MinValue, int.MinValue);
        private Vector2Int _hovered = new(int.MinValue, int.MinValue);

        // ── 뷰 범위 ────────────────────────────────────────────────────
        private int _vMinX = -3, _vMaxX = 3, _vMinY = -3, _vMaxY = 3;

        // ── 스크롤 ─────────────────────────────────────────────────────
        private Vector2 _mainScroll;

        // ── 파생값 ─────────────────────────────────────────────────────
        private float TileW => TileBase * _zoom;
        private float TileH => TileBase * 0.5f * _zoom;

        // ── 색상 ───────────────────────────────────────────────────────
        private static readonly Color ColEmpty = new(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Color ColBorderEmpty = new(0.35f, 0.38f, 0.44f, 1f);
        private static readonly Color ColOrigin = new(0.98f, 0.78f, 0.46f, 0.95f);
        private static readonly Color ColOriginBorder = new(0.93f, 0.62f, 0.09f, 1f);
        private static readonly Color ColBlocked = new(0.80f, 0.22f, 0.22f, 0.90f);
        private static readonly Color ColBlockedB = new(1.00f, 0.40f, 0.40f, 1f);
        private static readonly Color ColHover = new(1.00f, 0.45f, 0.45f, 0.35f);
        private Texture2D _diamondFillTex;
        private Texture2D _diamondBorderTex;

        // ── 스프라이트 프리뷰 ──────────────────────────────────────────
        private Sprite _previewSprite;

        // ── GUIStyle 캐시 ──────────────────────────────────────────────
        private GUIStyle _tileLabelStyle;
        private GUIStyle _hintLabelStyle;

        // ── 생명주기 ───────────────────────────────────────────────────
        private void OnEnable()
        {
            const int W = 64, H = 32;
            _diamondFillTex = BuildDiamondFillTex(W, H);
            _diamondBorderTex = BuildDiamondBorderTex(W, H, 2.5f);

            _hintLabelStyle = new GUIStyle(EditorStyles.miniLabel);
            _hintLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
        }

        private void OnDisable()
        {
            DestroyImmediate(_diamondFillTex);
            DestroyImmediate(_diamondBorderTex);
            _diamondFillTex = _diamondBorderTex = null;
        }

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
            w.minSize = new Vector2(380f, 620f);
        }

        // ── GUI 진입 ───────────────────────────────────────────────────
        private void OnGUI()
        {
            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
            DrawPrefabSection();
            if (_prefab != null)
            {
                Divider();
                DrawCanvas();
                Divider();
                DrawVisualOffsetSection();
                Divider();
                DrawTileSetSection();
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
            _prefab = (TycoonObject)EditorGUILayout.ObjectField("Prefab", _prefab, typeof(TycoonObject), false);
            if (_prefab != prev && _prefab != null) LoadFromPrefab();
            if (GUILayout.Button("불러오기", GUILayout.Width(70)) && _prefab != null) LoadFromPrefab();
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_prefabPath))
                EditorGUILayout.LabelField(_prefabPath, EditorStyles.miniLabel);

            if (_prefab != null)
            {
                var go = _prefab.gameObject;
                var preview = AssetPreview.GetAssetPreview(go);
                if (preview == null)
                {
                    if (AssetPreview.IsLoadingAssetPreview(go.GetInstanceID()))
                    {
                        EditorGUILayout.LabelField("프리뷰 로딩 중...", EditorStyles.miniLabel);
                        Repaint();
                    }
                }
                else
                {
                    Rect r = GUILayoutUtility.GetRect(80f, 80f);
                    r.x = (EditorGUIUtility.currentViewWidth - 80f) * 0.5f;
                    GUI.DrawTexture(r, preview, ScaleMode.ScaleToFit);
                }
            }
        }

        // ── 프리팹 로드 ────────────────────────────────────────────────
        private void LoadFromPrefab()
        {
            _prefabPath = AssetDatabase.GetAssetPath(_prefab);
            _tileSetData = _prefab.TileSetData;
            _isWalkable = _tileSetData?.IsWalkable ?? true;

            _blocked.Clear();
            if (_tileSetData?.BlockedOffsets != null)
                foreach (var v in _tileSetData.BlockedOffsets)
                    _blocked.Add(v);

            var visualRoot = _prefab.GetComponentInChildren<TycoonVisualRoot>();
            _hasVisualRoot = visualRoot != null;
            _visualOffset = _hasVisualRoot ? visualRoot.transform.localPosition : Vector3.zero;

            var srSource = _hasVisualRoot ? (Component)visualRoot : _prefab;
            var sr = srSource.GetComponentInChildren<SpriteRenderer>();
            _previewSprite = sr?.sprite;

            _panOffset = Vector2.zero;
            Repaint();
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
                    tileSet.IsWalkable = _isWalkable;
                    EditorUtility.SetDirty(tileSet);
                }

                var visualRoot = contents.GetComponentInChildren<TycoonVisualRoot>();
                if (visualRoot != null)
                    visualRoot.transform.localPosition = _visualOffset;

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

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
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────
        private static void Divider()
        {
            GUILayout.Space(4);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1f), new Color(1f, 1f, 1f, 0.08f));
            GUILayout.Space(4);
        }

        // ── 플레이스홀더 (Task 3, 4, 5에서 채움) ──────────────────────
        private void DrawCanvas()
        {
            EditorGUILayout.LabelField("타일 뷰 범위", EditorStyles.miniBoldLabel);
            DrawRangeRow("X", ref _vMinX, ref _vMaxX);
            DrawRangeRow("Y", ref _vMinY, ref _vMaxY);

            Rect canvasRect = GUILayoutUtility.GetRect(0, CanvasH);
            canvasRect.x = 0;
            canvasRect.width = EditorGUIUtility.currentViewWidth - 4f;
            EditorGUI.DrawRect(canvasRect, new Color(0.13f, 0.13f, 0.16f, 1f));

            Event e = Event.current;
            bool inCanvas = canvasRect.Contains(e.mousePosition);
            Rect local = new Rect(0, 0, canvasRect.width, canvasRect.height);

            // 스크롤 줌
            if (inCanvas && e.type == EventType.ScrollWheel)
            {
                float prev = _zoom;
                _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.05f, 0.3f, 3f);
                Vector2 ml = e.mousePosition - new Vector2(canvasRect.x + canvasRect.width * 0.5f,
                                                            canvasRect.y + canvasRect.height * 0.5f);
                _panOffset = ml + (_panOffset - ml) * (_zoom / prev);
                e.Use(); Repaint();
            }

            // 패닝 (우클릭 드래그)
            if (inCanvas && e.type == EventType.MouseDown && e.button == 1)
            { _isPanning = true; _panStartMouse = e.mousePosition; _panStartOffset = _panOffset; e.Use(); }
            if (_isPanning)
            {
                if (e.type == EventType.MouseDrag && e.button == 1)
                { _panOffset = _panStartOffset + (e.mousePosition - _panStartMouse); e.Use(); Repaint(); }
                if (e.type == EventType.MouseUp && e.button == 1)
                { _isPanning = false; e.Use(); }
            }

            // 타일 페인팅 (좌클릭 / 드래그)
            if (inCanvas && e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 lm = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                if (ScreenToGrid(lm, local, out Vector2Int hv) && hv != Vector2Int.zero)
                {
                    _dragErasing = _blocked.Contains(hv);
                    _isDragPainting = true;
                    _lastDragCell = hv;
                    PaintTile(hv, _dragErasing);
                }
                e.Use();
            }
            if (_isDragPainting)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    Vector2 lm = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                    if (ScreenToGrid(lm, local, out Vector2Int hv) && hv != _lastDragCell)
                    { PaintTile(hv, _dragErasing); _lastDragCell = hv; }
                    Repaint(); e.Use();
                }
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    _isDragPainting = false;
                    _lastDragCell = new Vector2Int(int.MinValue, int.MinValue);
                    e.Use();
                }
            }

            // 호버
            if (!_isPanning && (e.type == EventType.MouseMove ||
                (e.type == EventType.MouseDrag && e.button == 0)))
            {
                _hovered = inCanvas
                    && ScreenToGrid(e.mousePosition - new Vector2(canvasRect.x, canvasRect.y), local, out Vector2Int h)
                    ? h : new Vector2Int(int.MinValue, int.MinValue);
                Repaint();
            }

            // 렌더
            if (e.type == EventType.Repaint)
            {
                GUI.BeginClip(canvasRect);

                if (_previewSprite != null)
                    DrawSpriteOnCanvas(_previewSprite, local);

                int tileFontSize = Mathf.RoundToInt(Mathf.Clamp(9f * _zoom, 7f, 12f));
                if (_tileLabelStyle == null)
                {
                    _tileLabelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                    _tileLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.65f);
                }
                _tileLabelStyle.fontSize = tileFontSize;

                for (int gy = _vMinY; gy <= _vMaxY; gy++)
                    for (int gx = _vMinX; gx <= _vMaxX; gx++)
                    {
                        var coord = new Vector2Int(gx, gy);
                        Vector2 ctr = GridToScreen(gx, gy, local);
                        if (ctr.x < -TileW || ctr.x > local.width + TileW ||
                            ctr.y < -TileH || ctr.y > local.height + TileH) continue;

                        bool isOrig = coord == Vector2Int.zero;
                        bool isBlk = _blocked.Contains(coord);
                        bool isHov = coord == _hovered && !isOrig;

                        Color fill, border;
                        if (isOrig) { fill = ColOrigin; border = ColOriginBorder; }
                        else if (isBlk) { fill = ColBlocked; border = ColBlockedB; }
                        else { fill = ColEmpty; border = ColBorderEmpty; }

                        DrawDiamond(ctr, fill, border);
                        if (isHov) DrawDiamond(ctr, ColHover, Color.clear);

                        if (TileW >= 28f)
                        {
                            string lbl = isOrig ? "0,0" : $"{gx},{gy}";
                            GUI.Label(new Rect(ctr.x - TileW * 0.5f, ctr.y - TileH * 0.5f, TileW, TileH), lbl, _tileLabelStyle);
                        }
                    }

                GUI.Label(new Rect(8, 6, 300, 18),
                    "좌클릭: 점유 토글  |  우클릭 드래그: 패닝  |  스크롤: 줌",
                    _hintLabelStyle);

                GUI.EndClip();
            }
        }

        private Vector2 GridToScreen(int gx, int gy, Rect canvas)
        {
            float midGx = (_vMinX + _vMaxX) * 0.5f;
            float midGy = (_vMinY + _vMaxY) * 0.5f;
            float pivotX = canvas.x + canvas.width * 0.5f - (midGx - midGy) * (TileW * 0.5f) + _panOffset.x;
            float pivotY = canvas.y + canvas.height * 0.5f + (midGx + midGy) * (TileH * 0.5f) + _panOffset.y;
            return new Vector2(
                pivotX + (gx - gy) * (TileW * 0.5f),
                pivotY - (gx + gy) * (TileH * 0.5f));
        }

        private bool ScreenToGrid(Vector2 mouse, Rect canvas, out Vector2Int cell)
        {
            float best = float.MaxValue;
            cell = default;
            bool found = false;
            for (int gy = _vMinY; gy <= _vMaxY; gy++)
                for (int gx = _vMinX; gx <= _vMaxX; gx++)
                {
                    Vector2 ctr = GridToScreen(gx, gy, canvas);
                    float dx = Mathf.Abs(mouse.x - ctr.x) / (TileW * 0.5f);
                    float dy = Mathf.Abs(mouse.y - ctr.y) / (TileH * 0.5f);
                    if (dx + dy <= 1f)
                    {
                        float d = (mouse - ctr).sqrMagnitude;
                        if (d < best) { best = d; cell = new Vector2Int(gx, gy); found = true; }
                    }
                }
            return found;
        }

        private void DrawDiamond(Vector2 c, Color fill, Color border)
        {
            float hw = TileW * 0.5f, hh = TileH * 0.5f;
            var r = new Rect(c.x - hw, c.y - hh, TileW, TileH);
            var pc = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(r, _diamondFillTex);
            if (border.a > 0.01f)
            {
                GUI.color = border;
                GUI.DrawTexture(r, _diamondBorderTex);
            }
            GUI.color = pc;
        }

        private void DrawSpriteOnCanvas(Sprite sprite, Rect canvas)
        {
            var tex = sprite.texture;
            var tr = sprite.textureRect;
            var uvRect = new Rect(tr.x / tex.width, tr.y / tex.height,
                                  tr.width / tex.width, tr.height / tex.height);

            float unitToPixel = TileBase * _zoom;
            float w = tr.width / sprite.pixelsPerUnit * unitToPixel;
            float h = tr.height / sprite.pixelsPerUnit * unitToPixel;

            // sprite.pivot: pixels from bottom-left of textureRect
            float pivX = sprite.pivot.x / sprite.pixelsPerUnit * unitToPixel;
            float pivY = sprite.pivot.y / sprite.pixelsPerUnit * unitToPixel;

            // visual offset in canvas pixels (canvas Y is inverted)
            float ox = _visualOffset.x * unitToPixel;
            float oy = -_visualOffset.y * unitToPixel;

            Vector2 origin = GridToScreen(0, 0, canvas);

            var drawRect = new Rect(
                origin.x + ox - pivX,
                origin.y + oy - (h - pivY),
                w, h);

            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTextureWithTexCoords(drawRect, tex, uvRect, true);
            GUI.color = prev;
        }

        private void PaintTile(Vector2Int coord, bool erasing)
        {
            if (coord == Vector2Int.zero) return;
            if (erasing) _blocked.Remove(coord); else _blocked.Add(coord);
            Repaint();
        }

        private void DrawRangeRow(string axis, ref int min, ref int max)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(axis, EditorStyles.miniBoldLabel, GUILayout.Width(14));
                GUILayout.Label("음:", EditorStyles.miniLabel, GUILayout.Width(24));
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(17)) && min > -RangeMax) { min--; Repaint(); }
                GUILayout.Label(min.ToString(), EditorStyles.miniLabel, GUILayout.Width(20));
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(17)) && min < -RangeMin) { min++; Repaint(); }
                GUILayout.Space(10);
                GUILayout.Label("양:", EditorStyles.miniLabel, GUILayout.Width(24));
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(17)) && max > RangeMin) { max--; Repaint(); }
                GUILayout.Label(max.ToString(), EditorStyles.miniLabel, GUILayout.Width(20));
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(17)) && max < RangeMax) { max++; Repaint(); }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawVisualOffsetSection()
        {
            EditorGUILayout.LabelField("Visual Offset  (TycoonVisualRoot localPosition)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Vector3 newOff = EditorGUILayout.Vector3Field("Offset", _visualOffset);
            if (EditorGUI.EndChangeCheck())
            {
                _visualOffset = newOff;
                Repaint();
            }

            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                _visualOffset = Vector3.zero;
                Repaint();
            }

            if (!_hasVisualRoot)
                EditorGUILayout.HelpBox(
                    "이 프리팹에 TycoonVisualRoot 컴포넌트가 없습니다.\n" +
                    "비주얼 자식 GO에 컴포넌트를 부착하세요.",
                    MessageType.Warning);
        }
        private void DrawTileSetSection()
        {
            EditorGUILayout.LabelField("TileSet  (BlockedOffsets)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _isWalkable = EditorGUILayout.Toggle("IsWalkable", _isWalkable);
            if (EditorGUI.EndChangeCheck()) Repaint();

            string blkText = _blocked.Count == 0
                ? "BlockedOffsets: (없음)"
                : "BlockedOffsets: " + string.Join("  ",
                    _blocked.OrderBy(v => v.y).ThenBy(v => v.x).Select(v => $"({v.x},{v.y})"));
            EditorGUILayout.LabelField(blkText, EditorStyles.miniLabel);

            if (_tileSetData == null)
                EditorGUILayout.HelpBox(
                    "이 프리팹의 TycoonObject에 TileSetData SO가 없습니다.\n저장 시 TileSet 변경이 반영되지 않습니다.",
                    MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            var pc = GUI.color;
            GUI.color = new Color(1f, 0.6f, 0.6f, 1f);
            if (GUILayout.Button("점유 초기화", GUILayout.Width(80)))
                if (EditorUtility.DisplayDialog("초기화", "모든 점유 타일을 삭제하시겠습니까?", "삭제", "취소"))
                { _blocked.Clear(); Repaint(); }
            GUI.color = pc;
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
