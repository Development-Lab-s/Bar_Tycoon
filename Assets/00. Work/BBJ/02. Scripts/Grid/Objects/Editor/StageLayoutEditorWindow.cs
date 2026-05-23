#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using BBJ.GridSystem.Objects;
using BBJ.WorkplaceSystem;
using UnityEditor;
using UnityEngine;

namespace BBJ.GridSystem.Objects.Editor
{
    public class StageLayoutEditorWindow : EditorWindow
    {
        // ── 상수 ──────────────────────────────────────────────────────────
        private const string MenuPath = "Tools/Stage Layout Editor";
        private const float  TileBase = 48f;
        private const float  SidebarW = 214f;
        private const float  CanvasH  = 460f;

        // ── 레이어 ────────────────────────────────────────────────────────
        private class LayerState
        {
            public StageLayoutSO Layout;
            public bool          Visible = true;
            public bool          Dirty;
            public Color         Col;
        }
        private readonly List<LayerState> _layers    = new();
        private int                       _activeIdx = -1;

        private static readonly Color[] LayerPalette =
        {
            new Color(0.30f, 0.70f, 1.00f, 0.90f),
            new Color(0.35f, 1.00f, 0.55f, 0.90f),
            new Color(1.00f, 0.55f, 0.25f, 0.90f),
            new Color(0.90f, 0.30f, 0.90f, 0.90f),
            new Color(1.00f, 0.90f, 0.25f, 0.90f),
        };

        // ── 오브젝트 팔레트 ───────────────────────────────────────────────
        private readonly List<ObjectDataSO> _palette = new();
        private int     _selectedPaletteIdx = -1;
        private bool    _flipX;
        private Vector2 _paletteScroll;

        // ── 그리드 설정 ───────────────────────────────────────────────────
        private int   _gridW    = 12;
        private int   _gridH    = 12;
        private float _cellSize = 0.5f;

        // ── 페인트 모드 ───────────────────────────────────────────────────
        private bool _eraseMode;

        // ── 캔버스 상태 ───────────────────────────────────────────────────
        private Vector2    _panOffset;
        private float      _zoom = 1f;
        private bool       _isPanning;
        private Vector2    _panStart, _panStartOff;
        private bool       _isDragPainting;
        private bool       _dragErasing;
        private Vector2Int _lastDragCell = new(int.MinValue, int.MinValue);
        private Vector2Int _hovered      = new(int.MinValue, int.MinValue);

        // ── 텍스처 / 스타일 ───────────────────────────────────────────────
        private Texture2D _fillTex;
        private Texture2D _borderTex;
        private GUIStyle  _hintStyle;
        private GUIStyle  _tileLabelStyle;

        // ── 스크롤 ────────────────────────────────────────────────────────
        private Vector2 _layerScroll;

        // ── 파생값 ────────────────────────────────────────────────────────
        private float TileW => TileBase * _zoom;
        private float TileH => TileBase * 0.5f * _zoom;

        // ── 고정 색상 ─────────────────────────────────────────────────────
        private static readonly Color ColEmpty       = new(0.18f, 0.20f, 0.23f, 1f);
        private static readonly Color ColEmptyBorder = new(0.28f, 0.31f, 0.38f, 1f);
        private static readonly Color ColOrigin      = new(0.98f, 0.82f, 0.46f, 0.80f);
        private static readonly Color ColOriginB     = new(0.93f, 0.65f, 0.09f, 1f);
        private static readonly Color ColHover       = new(1.00f, 1.00f, 1.00f, 0.18f);
        private static readonly Color ColErase       = new(1.00f, 0.28f, 0.28f, 0.28f);

        // ── 메뉴 ──────────────────────────────────────────────────────────
        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var w = GetWindow<StageLayoutEditorWindow>("Stage Layout Editor");
            w.minSize = new Vector2(720f, 680f);
        }

        // ── 생명주기 ──────────────────────────────────────────────────────
        private void OnEnable()
        {
            BuildTextures();
            RefreshPalette();
            _hintStyle = new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(1f, 1f, 1f, 0.35f) } };
        }

        private void OnDisable()
        {
            DestroyImmediate(_fillTex);
            DestroyImmediate(_borderTex);
            _fillTex = _borderTex = null;
        }

        // ── 텍스처 빌드 ───────────────────────────────────────────────────
        private void BuildTextures()
        {
            const int W = 64, H = 32;
            _fillTex   = MakeDiamondFill(W, H);
            _borderTex = MakeDiamondBorder(W, H, 2.5f);
        }

        private static Texture2D MakeDiamondFill(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var px  = new Color32[w * h];
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs(x - cx) / (w * 0.5f);
                    float ny = Mathf.Abs(y - cy) / (h * 0.5f);
                    byte  a  = (byte)(Mathf.Clamp01(1f - (nx + ny - 1f) * (w * 0.25f)) * 255);
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        private static Texture2D MakeDiamondBorder(int w, int h, float thickness)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var px  = new Color32[w * h];
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float t  = thickness / (w * 0.5f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx    = Mathf.Abs(x - cx) / (w * 0.5f);
                    float ny    = Mathf.Abs(y - cy) / (h * 0.5f);
                    float d     = nx + ny;
                    float outer = Mathf.Clamp01(1f - (d - 1f) * (w * 0.25f));
                    float inner = Mathf.Clamp01(((1f - t) - d) * (w * 0.25f));
                    byte  a     = (byte)(Mathf.Clamp01(outer - inner) * 255);
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        // ── 팔레트 스캔 ───────────────────────────────────────────────────
        private void RefreshPalette()
        {
            _palette.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:ObjectDataSO"))
            {
                var obj = AssetDatabase.LoadAssetAtPath<ObjectDataSO>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (obj != null) _palette.Add(obj);
            }
            _palette.Sort((a, b) =>
                string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.Ordinal));
        }

        // ── OnGUI ─────────────────────────────────────────────────────────
        private void OnGUI()
        {
            if (_hintStyle == null || _fillTex == null) OnEnable();

            DrawTopBar();
            Divider();
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
            {
                DrawLayerSidebar();
                DrawMainArea();
            }
        }

        // ── 상단 툴바 ─────────────────────────────────────────────────────
        private void DrawTopBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Grid", EditorStyles.toolbarButton,
                    GUILayout.Width(28));

                EditorGUI.BeginChangeCheck();
                int newW = EditorGUILayout.IntField(_gridW, GUILayout.Width(36));
                EditorGUILayout.LabelField("×", GUILayout.Width(12));
                int newH = EditorGUILayout.IntField(_gridH, GUILayout.Width(36));
                if (EditorGUI.EndChangeCheck())
                {
                    _gridW = Mathf.Clamp(newW, 1, 60);
                    _gridH = Mathf.Clamp(newH, 1, 60);
                    Repaint();
                }

                GUILayout.Space(12);
                EditorGUILayout.LabelField("Cell", EditorStyles.toolbarButton,
                    GUILayout.Width(28));
                EditorGUI.BeginChangeCheck();
                float newCell = EditorGUILayout.FloatField(_cellSize, GUILayout.Width(44));
                if (EditorGUI.EndChangeCheck() && newCell > 0f)
                { _cellSize = newCell; Repaint(); }
                if (GUILayout.Button("0.5", EditorStyles.toolbarButton, GUILayout.Width(28)))
                { _cellSize = 0.5f; Repaint(); }

                GUILayout.FlexibleSpace();

                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = _eraseMode ? new Color(1f, 0.40f, 0.40f) : Color.white;
                if (GUILayout.Button("지우개", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    _eraseMode = !_eraseMode;
                GUI.backgroundColor = prevBg;

                GUILayout.Space(6);
                GUI.backgroundColor = new Color(0.35f, 0.80f, 0.45f);
                if (GUILayout.Button("전체 저장", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    SaveAll();
                GUI.backgroundColor = Color.white;
            }
        }

        // ── 레이어 사이드바 ───────────────────────────────────────────────
        private void DrawLayerSidebar()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(SidebarW),
                                                      GUILayout.ExpandHeight(true)))
            {
                // 레이어 목록
                EditorGUILayout.LabelField("레이어", EditorStyles.boldLabel);

                float listH = Mathf.Clamp(_layers.Count * 26f + 8f, 40f, 200f);
                _layerScroll = EditorGUILayout.BeginScrollView(_layerScroll,
                    GUILayout.Height(listH));
                for (int i = 0; i < _layers.Count; i++)
                    DrawLayerRow(i);
                EditorGUILayout.EndScrollView();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("추가", GUILayout.Height(20)))
                        AddExistingLayout();
                    if (GUILayout.Button("새로 만들기", GUILayout.Height(20)))
                        CreateNewLayout();
                }

                Divider();

                // 오브젝트 팔레트
                EditorGUILayout.LabelField("오브젝트 팔레트", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("새로고침", GUILayout.Width(70), GUILayout.Height(18)))
                    { RefreshPalette(); _selectedPaletteIdx = -1; }
                    _flipX = EditorGUILayout.ToggleLeft("FlipX", _flipX, GUILayout.Width(58));
                }

                _paletteScroll = EditorGUILayout.BeginScrollView(
                    _paletteScroll, GUILayout.ExpandHeight(true));
                DrawPalette();
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawLayerRow(int i)
        {
            var  layer  = _layers[i];
            bool active = i == _activeIdx;

            var bg = GUI.backgroundColor;
            GUI.backgroundColor = active ? new Color(0.50f, 0.72f, 1f) : Color.white;
            using (new EditorGUILayout.HorizontalScope("Button", GUILayout.Height(22)))
            {
                GUI.backgroundColor = bg;

                layer.Visible = GUILayout.Toggle(layer.Visible, GUIContent.none,
                    GUILayout.Width(16));

                string label = layer.Dirty ? layer.Layout.name + " *" : layer.Layout.name;
                var    ns    = new GUIStyle(EditorStyles.label)
                    { normal = { textColor = active ? Color.white : new Color(0.85f, 0.85f, 0.85f) } };
                if (GUILayout.Button(label, ns, GUILayout.ExpandWidth(true)))
                    _activeIdx = i;

                // 색상 마커
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = layer.Col;
                GUILayout.Box(GUIContent.none, GUILayout.Width(14), GUILayout.Height(14));
                GUI.backgroundColor = prevBg;

                if (GUILayout.Button("✕", GUILayout.Width(18), GUILayout.Height(18)))
                {
                    _layers.RemoveAt(i);
                    if (_activeIdx >= _layers.Count) _activeIdx = _layers.Count - 1;
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawPalette()
        {
            if (_palette.Count == 0)
            {
                EditorGUILayout.HelpBox("ObjectDataSO 에셋 없음\n" +
                    "(Create > GridSystem > ObjectIconData)", MessageType.Info);
                return;
            }

            for (int i = 0; i < _palette.Count; i++)
            {
                var  item = _palette[i];
                bool sel  = i == _selectedPaletteIdx;

                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = sel
                    ? new Color(0.35f, 0.65f, 1f)
                    : new Color(0.22f, 0.23f, 0.26f);

                if (GUILayout.Button(item.DisplayName ?? item.name,
                    GUILayout.Height(26), GUILayout.ExpandWidth(true)))
                {
                    _selectedPaletteIdx = sel ? -1 : i;
                    _eraseMode          = false;
                    Repaint();
                }
                GUI.backgroundColor = prevBg;
            }
        }

        // ── 메인 영역 ─────────────────────────────────────────────────────
        private void DrawMainArea()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true),
                                                      GUILayout.ExpandHeight(true)))
            {
                DrawCanvas();
                Divider();
                DrawStatusBar();
            }
        }

        private void DrawStatusBar()
        {
            if (_activeIdx < 0 || _activeIdx >= _layers.Count)
            {
                EditorGUILayout.LabelField("레이어를 선택하거나 추가하세요.",
                    EditorStyles.miniLabel);
                return;
            }
            var layer  = _layers[_activeIdx];
            var selObj = _selectedPaletteIdx >= 0 && _selectedPaletteIdx < _palette.Count
                ? _palette[_selectedPaletteIdx].DisplayName : "(없음)";
            EditorGUILayout.LabelField(
                $"레이어: {layer.Layout.name}  |  항목: {layer.Layout.entries.Count}  |  " +
                $"선택 오브젝트: {selObj}  |  FlipX: {_flipX}",
                EditorStyles.miniLabel);
        }

        // ── 캔버스 ────────────────────────────────────────────────────────
        private void DrawCanvas()
        {
            Rect canvasRect = GUILayoutUtility.GetRect(0, CanvasH, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(canvasRect, new Color(0.12f, 0.13f, 0.15f, 1f));

            var  e        = Event.current;
            bool inCanvas = canvasRect.Contains(e.mousePosition);
            var  local    = new Rect(0, 0, canvasRect.width, canvasRect.height);

            // 줌 (스크롤)
            if (inCanvas && e.type == EventType.ScrollWheel)
            {
                float prev = _zoom;
                _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.05f, 0.15f, 3f);
                var ml = e.mousePosition - new Vector2(
                    canvasRect.x + canvasRect.width  * 0.5f,
                    canvasRect.y + canvasRect.height * 0.5f);
                _panOffset = ml + (_panOffset - ml) * (_zoom / prev);
                e.Use(); Repaint();
            }

            // 패닝 (우클릭 드래그)
            if (inCanvas && e.type == EventType.MouseDown && e.button == 1)
            { _isPanning = true; _panStart = e.mousePosition; _panStartOff = _panOffset; e.Use(); }
            if (_isPanning)
            {
                if (e.type == EventType.MouseDrag && e.button == 1)
                { _panOffset = _panStartOff + (e.mousePosition - _panStart); e.Use(); Repaint(); }
                if (e.type == EventType.MouseUp && e.button == 1)
                { _isPanning = false; e.Use(); }
            }

            // 페인팅 (좌클릭)
            if (inCanvas && e.type == EventType.MouseDown && e.button == 0)
            {
                var lm = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                if (ScreenToGrid(lm, local, out var hv))
                {
                    _dragErasing    = _eraseMode;
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
                    var lm = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                    if (ScreenToGrid(lm, local, out var hv) && hv != _lastDragCell)
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

            // 호버
            if (!_isPanning && (e.type == EventType.MouseMove ||
                                 (e.type == EventType.MouseDrag && e.button == 0)))
            {
                var lm = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                var newHov = inCanvas && ScreenToGrid(lm, local, out var h)
                    ? h : new Vector2Int(int.MinValue, int.MinValue);
                if (newHov != _hovered) { _hovered = newHov; Repaint(); }
            }

            if (e.type != EventType.Repaint) return;

            GUI.BeginClip(canvasRect);

            // 빈 그리드 (0,0 ~ gridW, gridH)
            for (int gy = 0; gy <= _gridH; gy++)
                for (int gx = 0; gx <= _gridW; gx++)
                {
                    var ctr = GridToScreen(gx, gy, local);
                    if (IsOutOfView(ctr, local)) continue;

                    if (gx == 0 && gy == 0)
                        DrawDiamond(ctr, ColOrigin, ColOriginB);
                    else
                        DrawDiamond(ctr, ColEmpty, ColEmptyBorder);
                }

            // 레이어 엔트리 렌더 (비활성 → 활성 순)
            foreach (var layer in _layers.Where(l => l.Visible && l != ActiveLayer()))
                DrawLayerEntries(layer, local, 0.50f);
            if (ActiveLayer() is { Visible: true } al)
                DrawLayerEntries(al, local, 1f);

            // 셀 좌표 레이블
            if (TileW >= 28f)
            {
                if (_tileLabelStyle == null)
                {
                    _tileLabelStyle = new GUIStyle(EditorStyles.label)
                        { alignment = TextAnchor.MiddleCenter, fontSize = 8 };
                    _tileLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.40f);
                }
                for (int gy = 0; gy <= _gridH; gy++)
                    for (int gx = 0; gx <= _gridW; gx++)
                    {
                        var ctr = GridToScreen(gx, gy, local);
                        if (IsOutOfView(ctr, local)) continue;
                        GUI.Label(new Rect(ctr.x - TileW * 0.5f, ctr.y - TileH * 0.5f,
                            TileW, TileH), $"{gx},{gy}", _tileLabelStyle);
                    }
            }

            // 호버 오버레이
            if (_hovered.x != int.MinValue)
            {
                var ctr = GridToScreen(_hovered.x, _hovered.y, local);
                DrawDiamond(ctr, _eraseMode ? ColErase : ColHover, Color.clear);

                // 선택된 오브젝트 footprint 미리보기
                if (!_eraseMode && _selectedPaletteIdx >= 0 &&
                    _selectedPaletteIdx < _palette.Count)
                    DrawGhostFootprint(_palette[_selectedPaletteIdx], _hovered, local);
            }

            // 힌트 텍스트
            GUI.Label(new Rect(8, 6, 480, 18),
                "좌클릭/드래그: 배치  |  우클릭 드래그: 패닝  |  스크롤: 줌  |  " +
                "지우개 모드에서 좌클릭: 삭제", _hintStyle);

            GUI.EndClip();
        }

        // ── 레이어 렌더 ───────────────────────────────────────────────────
        private void DrawLayerEntries(LayerState layer, Rect local, float alpha)
        {
            Color fill   = layer.Col; fill.a   = alpha * 0.72f;
            Color border = layer.Col;
            border.r = Mathf.Min(border.r + 0.22f, 1f);
            border.g = Mathf.Min(border.g + 0.22f, 1f);
            border.b = Mathf.Min(border.b + 0.22f, 1f);
            border.a = alpha;

            Color dimFill   = fill;   dimFill.a   *= 0.45f;
            Color dimBorder = border; dimBorder.a *= 0.45f;

            foreach (var entry in layer.Layout.entries)
            {
                var ctr = GridToScreen(entry.cellIndex.x, entry.cellIndex.y, local);

                // 루트 셀
                if (!IsOutOfView(ctr, local))
                    DrawDiamond(ctr, fill, border);

                // BlockedOffsets
                var tileSet = entry.obstacleData?.WorkplacePrefab?.TileSetData;
                if (tileSet?.BlockedOffsets != null)
                {
                    foreach (var off in tileSet.BlockedOffsets)
                    {
                        var applied = entry.flipX ? new Vector2Int(-off.x, off.y) : off;
                        var oCtr    = GridToScreen(
                            entry.cellIndex.x + applied.x,
                            entry.cellIndex.y + applied.y, local);
                        if (!IsOutOfView(oCtr, local))
                            DrawDiamond(oCtr, dimFill, dimBorder);
                    }
                }

                // 아이콘 (루트 셀 위)
                if (TileW >= 22f && !IsOutOfView(ctr, local))
                    DrawEntryIcon(entry.obstacleData, ctr);
            }
        }

        private void DrawGhostFootprint(ObjectDataSO data, Vector2Int coord, Rect local)
        {
            var ghostFill   = new Color(1f, 1f, 1f, 0.18f);
            var ghostBorder = new Color(1f, 1f, 1f, 0.50f);
            var ctr = GridToScreen(coord.x, coord.y, local);
            DrawDiamond(ctr, ghostFill, ghostBorder);

            var tileSet = data?.WorkplacePrefab?.TileSetData;
            if (tileSet?.BlockedOffsets == null) return;
            foreach (var off in tileSet.BlockedOffsets)
            {
                var applied = _flipX ? new Vector2Int(-off.x, off.y) : off;
                var oCtr    = GridToScreen(coord.x + applied.x, coord.y + applied.y, local);
                DrawDiamond(oCtr, new Color(1f, 1f, 1f, 0.10f), new Color(1f, 1f, 1f, 0.35f));
            }
        }

        private void DrawEntryIcon(ObjectDataSO data, Vector2 ctr)
        {
            var icon = data?.Icon;
            if (icon == null) return;

            var tex = icon.texture;
            var tr  = icon.textureRect;
            var uv  = new Rect(tr.x / tex.width, tr.y / tex.height,
                               tr.width / tex.width, tr.height / tex.height);

            float s   = TileW * 0.52f;
            var   r   = new Rect(ctr.x - s * 0.5f, ctr.y - s * 0.5f, s, s);
            var   pc  = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.92f);
            GUI.DrawTextureWithTexCoords(r, tex, uv, true);
            GUI.color = pc;
        }

        // ── 페인트 ────────────────────────────────────────────────────────
        private void PaintCell(Vector2Int coord, bool erasing)
        {
            if (_activeIdx < 0 || _activeIdx >= _layers.Count) return;
            var layer = _layers[_activeIdx];

            if (erasing)
            {
                int removed = layer.Layout.entries.RemoveAll(e => e.cellIndex == coord);
                if (removed > 0) { layer.Dirty = true; }
                return;
            }

            if (_selectedPaletteIdx < 0 || _selectedPaletteIdx >= _palette.Count) return;
            var data = _palette[_selectedPaletteIdx];

            layer.Layout.entries.RemoveAll(e => e.cellIndex == coord);
            layer.Layout.entries.Add(new PlacedObstacleEntry
            {
                cellIndex    = coord,
                obstacleData = data,
                flipX        = _flipX,
            });
            layer.Dirty = true;
        }

        // ── 저장 ──────────────────────────────────────────────────────────
        private void SaveAll()
        {
            bool any = false;
            foreach (var layer in _layers.Where(l => l.Dirty))
            {
                EditorUtility.SetDirty(layer.Layout);
                layer.Dirty = false;
                any = true;
            }
            if (!any) { EditorUtility.DisplayDialog("저장", "변경 사항이 없습니다.", "확인"); return; }
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("저장 완료", "모든 레이어 저장 완료.", "확인");
        }

        private void AddExistingLayout()
        {
            var path = EditorUtility.OpenFilePanelWithFilters(
                "StageLayoutSO 선택", "Assets",
                new[] { "ScriptableObject", "asset", "All Files", "*" });
            if (string.IsNullOrEmpty(path)) return;

            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);

            var so = AssetDatabase.LoadAssetAtPath<StageLayoutSO>(path);
            if (so == null)
            { EditorUtility.DisplayDialog("오류", "StageLayoutSO 파일이 아닙니다.", "확인"); return; }
            if (_layers.Any(l => l.Layout == so))
            { EditorUtility.DisplayDialog("중복", "이미 추가된 레이어입니다.", "확인"); return; }

            _layers.Add(new LayerState
            {
                Layout  = so,
                Col     = LayerPalette[_layers.Count % LayerPalette.Length],
                Visible = true,
            });
            _activeIdx = _layers.Count - 1;
            Repaint();
        }

        private void CreateNewLayout()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "새 StageLayout 저장", "StageLayout", "asset", "저장 경로를 선택하세요.");
            if (string.IsNullOrEmpty(path)) return;

            var so = CreateInstance<StageLayoutSO>();
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();

            _layers.Add(new LayerState
            {
                Layout  = so,
                Col     = LayerPalette[_layers.Count % LayerPalette.Length],
                Visible = true,
            });
            _activeIdx = _layers.Count - 1;
            Repaint();
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────────
        private LayerState ActiveLayer() =>
            _activeIdx >= 0 && _activeIdx < _layers.Count ? _layers[_activeIdx] : null;

        private bool IsOutOfView(Vector2 ctr, Rect local) =>
            ctr.x < -TileW || ctr.x > local.width  + TileW ||
            ctr.y < -TileH || ctr.y > local.height + TileH;

        // ── 좌표 변환 ─────────────────────────────────────────────────────
        // (0,0)을 canvas 하단 기준으로 고정
        private Vector2 GridToScreen(int gx, int gy, Rect canvas)
        {
            float anchorX = canvas.x + canvas.width  * 0.5f + _panOffset.x;
            float anchorY = canvas.y + canvas.height - TileH + _panOffset.y;
            return new Vector2(
                anchorX + (gx - gy) * (TileW * 0.5f),
                anchorY - (gx + gy) * (TileH * 0.5f));
        }

        private bool ScreenToGrid(Vector2 mouse, Rect canvas, out Vector2Int cell)
        {
            float best = float.MaxValue;
            cell  = default;
            bool found = false;
            for (int gy = 0; gy <= _gridH; gy++)
                for (int gx = 0; gx <= _gridW; gx++)
                {
                    var   ctr = GridToScreen(gx, gy, canvas);
                    float dx  = Mathf.Abs(mouse.x - ctr.x) / (TileW * 0.5f);
                    float dy  = Mathf.Abs(mouse.y - ctr.y) / (TileH * 0.5f);
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
            var   r  = new Rect(c.x - hw, c.y - hh, TileW, TileH);
            var   pc = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(r, _fillTex);
            if (border.a > 0.01f)
            {
                GUI.color = border;
                GUI.DrawTexture(r, _borderTex);
            }
            GUI.color = pc;
        }

        private static void Divider()
        {
            GUILayout.Space(3);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1f), new Color(1f, 1f, 1f, 0.07f));
            GUILayout.Space(3);
        }
    }
}
#endif
