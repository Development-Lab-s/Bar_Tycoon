using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BBJ.GridSystem.Objects;
using BBJ.WorkplaceSystem;

namespace BBJ.GridSystem.Objects.Editor
{
    [CustomEditor(typeof(StageLayoutSO))]
    public class StageLayoutEditor : UnityEditor.Editor
    {
        // ──────────────────────────────────────────────
        // 렌더 상수
        // ──────────────────────────────────────────────
        private const float CanvasHeight = 300f;
        private const float TileSizeBase = 48f;
        private const int   BoundPad     = 1;

        // ──────────────────────────────────────────────
        // 컬러 팔레트
        // ──────────────────────────────────────────────
        private static readonly Color ColEmpty          = new Color(0.22f, 0.24f, 0.28f, 1.00f);
        private static readonly Color ColBorderEmpty    = new Color(0.35f, 0.38f, 0.44f, 1.00f);
        private static readonly Color ColOccupied       = new Color(0.25f, 0.45f, 0.65f, 0.85f);
        private static readonly Color ColOccupiedBorder = new Color(0.45f, 0.70f, 1.00f, 1.00f);
        private static readonly Color ColSelected        = new Color(1.00f, 0.85f, 0.15f, 0.90f);
        private static readonly Color ColSelectedBorder  = new Color(1.00f, 1.00f, 0.00f, 1.00f);
        private static readonly Color ColBlocked         = new Color(0.55f, 0.30f, 0.20f, 0.75f);
        private static readonly Color ColBlockedBorder   = new Color(0.85f, 0.50f, 0.30f, 1.00f);

        // ──────────────────────────────────────────────
        // 에디터 전용 상태
        // ──────────────────────────────────────────────
        private int    _selectedIndex = -1;
        private Dictionary<ObjectDataSO, Texture2D> _previewCache = new Dictionary<ObjectDataSO, Texture2D>();

        private Vector2 _panOffset      = Vector2.zero;
        private float   _zoom           = 1f;
        private bool    _isPanning;
        private bool    _rightClickMoved;
        private Vector2 _panStartMouse;
        private Vector2 _panStartOffset;

        private List<ObjectDataSO>  _paletteItems         = new List<ObjectDataSO>();
        private int               _selectedPaletteIndex = -1;
        private Vector2           _paletteScroll        = Vector2.zero;

        // ──────────────────────────────────────────────
        // 뷰 범위 (ComputeBounds가 매 프레임 갱신)
        // ──────────────────────────────────────────────
        private int _viewMinX, _viewMaxX, _viewMinY, _viewMaxY;

        // ──────────────────────────────────────────────
        // 파생 값
        // ──────────────────────────────────────────────
        private float TileW => TileSizeBase * _zoom;
        private float TileH => TileSizeBase * 0.5f * _zoom;

        private void OnEnable()
        {
            var layout = (StageLayoutSO)target;
            AssetPreview.SetPreviewTextureCacheSize(layout.entries.Count + 16);
        }

        private void OnDisable()
        {
            _previewCache.Clear();
            _selectedIndex = -1;
        }

        private static TileSetData GetTileSetData(ObjectDataSO od)
        {
            if (od?.WorkplacePrefab == null) return null;
            return od.WorkplacePrefab.GetComponent<TycoonObject>()?.TileSetData;
        }

        private void ComputeBounds(List<PlacedObstacleEntry> entries)
        {
            if (entries.Count == 0)
            {
                _viewMinX = _viewMinY = -2;
                _viewMaxX = _viewMaxY = 2;
                return;
            }
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var en in entries)
            {
                if (en.cellIndex.x < minX) minX = en.cellIndex.x;
                if (en.cellIndex.x > maxX) maxX = en.cellIndex.x;
                if (en.cellIndex.y < minY) minY = en.cellIndex.y;
                if (en.cellIndex.y > maxY) maxY = en.cellIndex.y;
                if (en.obstacleData == null) continue;
                var tsd = GetTileSetData(en.obstacleData);
                if (tsd?.BlockedOffsets == null) continue;
                foreach (var offset in tsd.BlockedOffsets)
                {
                    int bx = en.cellIndex.x + offset.x, by = en.cellIndex.y + offset.y;
                    if (bx < minX) minX = bx;
                    if (bx > maxX) maxX = bx;
                    if (by < minY) minY = by;
                    if (by > maxY) maxY = by;
                }
            }
            _viewMinX = minX - BoundPad;
            _viewMaxX = maxX + BoundPad;
            _viewMinY = minY - BoundPad;
            _viewMaxY = maxY + BoundPad;
        }

        private Vector2 GridToScreen(int gx, int gy, Rect canvas)
        {
            float midGx = (_viewMinX + _viewMaxX) * 0.5f;
            float midGy = (_viewMinY + _viewMaxY) * 0.5f;
            float pivotX = canvas.x + canvas.width  * 0.5f - (midGx - midGy) * (TileW * 0.5f) + _panOffset.x;
            float pivotY = canvas.y + canvas.height * 0.5f + (midGx + midGy) * (TileH * 0.5f) + _panOffset.y;
            return new Vector2(
                pivotX + (gx - gy) * (TileW * 0.5f),
                pivotY - (gx + gy) * (TileH * 0.5f)
            );
        }

        private bool ScreenToGrid(Vector2 mouse, Rect canvas, out Vector2Int cell)
        {
            float bestDist = float.MaxValue;
            cell = default;
            bool found = false;
            for (int gy = _viewMinY; gy <= _viewMaxY; gy++)
                for (int gx = _viewMinX; gx <= _viewMaxX; gx++)
                {
                    Vector2 center = GridToScreen(gx, gy, canvas);
                    float dx = Mathf.Abs(mouse.x - center.x) / (TileW * 0.5f);
                    float dy = Mathf.Abs(mouse.y - center.y) / (TileH * 0.5f);
                    if (dx + dy <= 1.0f)
                    {
                        float dist = (mouse - center).sqrMagnitude;
                        if (dist < bestDist) { bestDist = dist; cell = new Vector2Int(gx, gy); found = true; }
                    }
                }
            return found;
        }

        private void DrawDiamond(Vector2 c, Color fill, Color border)
        {
            float hw = TileW * 0.5f, hh = TileH * 0.5f;
            Handles.DrawSolidRectangleWithOutline(new Vector3[]
            {
                new Vector3(c.x,      c.y - hh),
                new Vector3(c.x + hw, c.y      ),
                new Vector3(c.x,      c.y + hh),
                new Vector3(c.x - hw, c.y      ),
            }, fill, border);
        }

        private Texture2D GetPreview(ObjectDataSO data)
        {
            if (data == null || data.WorkplacePrefab == null) return null;
            if (_previewCache.TryGetValue(data, out Texture2D tex) && tex != null) return tex;
            tex = AssetPreview.GetAssetPreview(data.WorkplacePrefab);
            if (tex != null) _previewCache[data] = tex;
            return tex;
        }

        private static Sprite GetSprite(ObjectDataSO data) => data?.Icon;

        private void DrawSpriteAtCell(ObjectDataSO data, Vector2 cellCenter)
        {
            if (data == null) return;
            Sprite s = GetSprite(data);
            if (s == null) return;

            Bounds b = s.bounds;
            float scale = TileW * 2f;
            float sw = b.size.x * scale;
            float sh = b.size.y * scale;

            // 마름모 아래 꼭짓점 = 스프라이트 pivot 기준점
            Vector2 pivot = new Vector2(cellCenter.x, cellCenter.y + TileH * 0.5f);
            Rect drawRect = new Rect(pivot.x + b.min.x * scale, pivot.y - b.max.y * scale, sw, sh);

            Texture2D tex = s.texture;
            Rect uv = new Rect(
                s.textureRect.x / tex.width,
                s.textureRect.y / tex.height,
                s.textureRect.width  / tex.width,
                s.textureRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(drawRect, tex, uv, true);
        }

        private void DrawGridCanvas(StageLayoutSO layout)
        {
            Rect canvasRect = GUILayoutUtility.GetRect(0, CanvasHeight);
            canvasRect.x     = 0;
            canvasRect.width = EditorGUIUtility.currentViewWidth - 4f;
            EditorGUI.DrawRect(canvasRect, new Color(0.13f, 0.13f, 0.16f, 1f));
            Handles.DrawSolidRectangleWithOutline(
                new[] {
                    new Vector3(canvasRect.xMin, canvasRect.yMin),
                    new Vector3(canvasRect.xMax, canvasRect.yMin),
                    new Vector3(canvasRect.xMax, canvasRect.yMax),
                    new Vector3(canvasRect.xMin, canvasRect.yMax),
                }, Color.clear, new Color(1f, 1f, 1f, 0.12f));

            Event e         = Event.current;
            bool  inCanvas  = canvasRect.Contains(e.mousePosition);
            Rect  localCanvas = new Rect(0, 0, canvasRect.width, canvasRect.height);

            // 스크롤 줌
            if (inCanvas && e.type == EventType.ScrollWheel)
            {
                float prev = _zoom;
                _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.05f, 0.2f, 3f);
                Vector2 ml = e.mousePosition - new Vector2(canvasRect.x + canvasRect.width * 0.5f,
                                                           canvasRect.y + canvasRect.height * 0.5f);
                _panOffset = ml + (_panOffset - ml) * (_zoom / prev);
                e.Use(); Repaint();
            }

            // 우클릭: 드래그=패닝, 탭=항목 삭제
            if (inCanvas && e.type == EventType.MouseDown && e.button == 1)
            { _panStartMouse = e.mousePosition; _panStartOffset = _panOffset; _rightClickMoved = false; e.Use(); }
            if (e.type == EventType.MouseDrag && e.button == 1)
            {
                if (!_rightClickMoved && (e.mousePosition - _panStartMouse).sqrMagnitude > 9f)
                { _rightClickMoved = true; _isPanning = true; }
                if (_isPanning)
                { _panOffset = _panStartOffset + (e.mousePosition - _panStartMouse); e.Use(); Repaint(); }
            }
            if (e.type == EventType.MouseUp && e.button == 1)
            {
                if (!_rightClickMoved && inCanvas)
                {
                    Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                    if (ScreenToGrid(localMouse, localCanvas, out Vector2Int delCell))
                    {
                        int idx = layout.entries.FindIndex(en => en.cellIndex == delCell);
                        if (idx >= 0)
                        {
                            Undo.RecordObject(layout, "Delete Entry");
                            _previewCache.Remove(layout.entries[idx].obstacleData);
                            layout.entries.RemoveAt(idx);
                            if (_selectedIndex == idx) _selectedIndex = -1;
                            else if (_selectedIndex > idx) _selectedIndex--;
                            EditorUtility.SetDirty(layout);
                            Repaint();
                        }
                    }
                }
                _isPanning = false;
                e.Use();
            }

            // 좌클릭 선택 / 배치
            if (inCanvas && e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                if (ScreenToGrid(localMouse, localCanvas, out Vector2Int cell))
                {
                    int idx = layout.entries.FindIndex(en => en.cellIndex == cell);
                    if (_selectedPaletteIndex >= 0)
                    {
                        Undo.RecordObject(layout, "Place Entry");
                        layout.entries.Add(new PlacedObstacleEntry
                        {
                            cellIndex    = cell,
                            obstacleData = _paletteItems[_selectedPaletteIndex]
                        });
                        _selectedIndex = layout.entries.Count - 1;
                        EditorUtility.SetDirty(layout);
                    }
                    else
                    {
                        _selectedIndex = idx;
                    }
                }
                else _selectedIndex = -1;
                e.Use(); Repaint();
            }

            // Delete/Backspace 키로 선택 항목 삭제
            if (e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) &&
                _selectedIndex >= 0 && _selectedIndex < layout.entries.Count)
            {
                Undo.RecordObject(layout, "Delete Entry");
                _previewCache.Remove(layout.entries[_selectedIndex].obstacleData);
                layout.entries.RemoveAt(_selectedIndex);
                _selectedIndex = -1;
                EditorUtility.SetDirty(layout);
                e.Use(); Repaint();
            }

            // 렌더
            if (e.type == EventType.Repaint)
            {
                GUI.BeginClip(canvasRect);

                var entryMap = new Dictionary<Vector2Int, int>(layout.entries.Count);
                for (int i = 0; i < layout.entries.Count; i++)
                    entryMap[layout.entries[i].cellIndex] = i;

                var blockedSet = new HashSet<Vector2Int>();
                foreach (var en in layout.entries)
                {
                    if (en.obstacleData == null) continue;
                    var entryTsd = GetTileSetData(en.obstacleData);
                    if (entryTsd?.BlockedOffsets == null) continue;
                    foreach (var offset in entryTsd.BlockedOffsets)
                        blockedSet.Add(en.cellIndex + offset);
                }

                for (int gy = _viewMinY; gy <= _viewMaxY; gy++)
                    for (int gx = _viewMinX; gx <= _viewMaxX; gx++)
                    {
                        Vector2 ctr = GridToScreen(gx, gy, localCanvas);
                        if (ctr.x < -TileW || ctr.x > localCanvas.width  + TileW ||
                            ctr.y < -TileH || ctr.y > localCanvas.height + TileH) continue;

                        var coord       = new Vector2Int(gx, gy);
                        bool isOccupied = entryMap.TryGetValue(coord, out int entryIdx);
                        bool isSelected = isOccupied && entryIdx == _selectedIndex;
                        bool isBlocked  = !isOccupied && blockedSet.Contains(coord);

                        Color fill   = isSelected ? ColSelected       : isOccupied ? ColOccupied       : isBlocked ? ColBlocked       : ColEmpty;
                        Color border = isSelected ? ColSelectedBorder : isOccupied ? ColOccupiedBorder : isBlocked ? ColBlockedBorder : ColBorderEmpty;
                        DrawDiamond(ctr, fill, border);

                        if (isOccupied)
                            DrawSpriteAtCell(layout.entries[entryIdx].obstacleData, ctr);
                    }

                GUI.Label(new Rect(8, 6, 200, 18), $"entries: {layout.entries.Count}",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 1f, 1f, 0.5f) } });

                if (_isPanning)
                    GUI.Label(new Rect(0, 4, localCanvas.width - 6, 20), "패닝 중...",
                        new GUIStyle(EditorStyles.miniLabel)
                        { alignment = TextAnchor.UpperRight, normal = { textColor = new Color(1f, 1f, 1f, 0.3f) } });

                GUI.EndClip();
            }

            GUILayout.Label("우클릭 드래그: 패닝   |   스크롤: 줌   |   좌클릭: 선택/배치",
                new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 1f, 1f, 0.3f) } });
        }

        private void DrawDetailPanel(StageLayoutSO layout)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("선택된 항목", EditorStyles.boldLabel);

            var entry = layout.entries[_selectedIndex];

            // AssetPreview 128×128
            if (entry.obstacleData != null && entry.obstacleData.WorkplacePrefab != null)
            {
                Texture2D preview = GetPreview(entry.obstacleData);
                if (preview != null)
                {
                    Rect pr = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
                    GUI.DrawTexture(pr, preview, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUILayout.Label("[프리뷰 로딩 중...]",
                        new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 1f, 1f, 0.4f) } },
                        GUILayout.Height(32));
                    Repaint();
                }
            }
            else
            {
                EditorGUI.DrawRect(
                    GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128)),
                    new Color(0.2f, 0.2f, 0.2f, 1f));
            }

            // cellIndex 필드
            EditorGUI.BeginChangeCheck();
            Vector2Int newCell = EditorGUILayout.Vector2IntField("cellIndex", entry.cellIndex);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(layout, "Edit cellIndex");
                PlacedObstacleEntry modified = entry;
                modified.cellIndex = newCell;
                layout.entries[_selectedIndex] = modified;
                EditorUtility.SetDirty(layout);
            }

            // obstacleData 필드
            EditorGUI.BeginChangeCheck();
            var newData = (ObjectDataSO)EditorGUILayout.ObjectField(
                "obstacleData", entry.obstacleData, typeof(ObjectDataSO), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(layout, "Edit obstacleData");
                _previewCache.Remove(entry.obstacleData);
                PlacedObstacleEntry modified = entry;
                modified.obstacleData = newData;
                layout.entries[_selectedIndex] = modified;
                EditorUtility.SetDirty(layout);
            }

            // Delete 버튼
            EditorGUILayout.Space(4);
            GUI.color = new Color(1f, 0.5f, 0.5f, 1f);
            if (GUILayout.Button("Delete", GUILayout.Width(80)))
            {
                Undo.RecordObject(layout, "Delete Entry");
                _previewCache.Remove(layout.entries[_selectedIndex].obstacleData);
                layout.entries.RemoveAt(_selectedIndex);
                _selectedIndex = -1;
                EditorUtility.SetDirty(layout);
            }
            GUI.color = Color.white;
        }

        private void DrawToolbar(StageLayoutSO layout)
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.color = new Color(0.5f, 1f, 0.7f, 1f);
                if (GUILayout.Button("Add Entry", GUILayout.Width(90)))
                {
                    Undo.RecordObject(layout, "Add Entry");
                    layout.entries.Add(new PlacedObstacleEntry
                    {
                        cellIndex    = Vector2Int.zero,
                        obstacleData = null
                    });
                    _selectedIndex = layout.entries.Count - 1;
                    EditorUtility.SetDirty(layout);
                }
                GUI.color = Color.white;
                GUILayout.FlexibleSpace();
                GUILayout.Label($"entries: {layout.entries.Count}", EditorStyles.miniLabel);
            }
        }

        private void DrawPalette(StageLayoutSO layout)
        {
            EditorGUILayout.Space(6);

            // ── Find 버튼 툴바 ────────────────────────────
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find ObjectData", GUILayout.Width(120)))
                {
                    _paletteItems.Clear();
                    _selectedPaletteIndex = -1;
                    string[] guids = AssetDatabase.FindAssets("t:ObjectDataSO");
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        var data = AssetDatabase.LoadAssetAtPath<ObjectDataSO>(path);
                        if (data != null) _paletteItems.Add(data);
                    }
                    AssetPreview.SetPreviewTextureCacheSize(
                        layout.entries.Count + _paletteItems.Count + 16);
                }
                GUILayout.Label($"found: {_paletteItems.Count}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
            }

            if (_paletteItems.Count == 0) return;

            // ── 썸네일 스크롤 영역 ────────────────────────
            const float itemW    = 72f;
            const float areaH    = 88f;

            Rect scrollArea = GUILayoutUtility.GetRect(0, areaH);
            scrollArea.x     = 0;
            scrollArea.width = EditorGUIUtility.currentViewWidth - 4f;

            float contentW    = _paletteItems.Count * itemW;
            Rect  contentRect = new Rect(0, 0, Mathf.Max(contentW, scrollArea.width), areaH);

            _paletteScroll = GUI.BeginScrollView(scrollArea, _paletteScroll, contentRect, true, false);

            for (int i = 0; i < _paletteItems.Count; i++)
            {
                float x         = i * itemW + 4f;
                Rect  thumbRect = new Rect(x, 4f, 64f, 64f);
                Rect  labelRect = new Rect(x, 70f, 64f, 14f);
                Rect  hitRect   = new Rect(x, 4f, 64f, 80f);

                EditorGUI.DrawRect(thumbRect, new Color(0.18f, 0.18f, 0.22f, 1f));

                Texture2D preview = GetPreview(_paletteItems[i]);
                if (preview != null)
                    GUI.DrawTexture(thumbRect, preview, ScaleMode.ScaleToFit);

                if (i == _selectedPaletteIndex)
                {
                    float bw = 2f;
                    EditorGUI.DrawRect(new Rect(thumbRect.x,         thumbRect.y,         thumbRect.width,  bw), ColSelectedBorder);
                    EditorGUI.DrawRect(new Rect(thumbRect.x,         thumbRect.yMax - bw, thumbRect.width,  bw), ColSelectedBorder);
                    EditorGUI.DrawRect(new Rect(thumbRect.x,         thumbRect.y,         bw, thumbRect.height), ColSelectedBorder);
                    EditorGUI.DrawRect(new Rect(thumbRect.xMax - bw, thumbRect.y,         bw, thumbRect.height), ColSelectedBorder);
                }

                GUI.Label(labelRect, _paletteItems[i].name,
                    new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });

                if (GUI.Button(hitRect, GUIContent.none, GUIStyle.none))
                {
                    _selectedPaletteIndex = (i == _selectedPaletteIndex) ? -1 : i;
                    Repaint();
                }
            }

            GUI.EndScrollView();
        }

        public override void OnInspectorGUI()
        {
            var layout = (StageLayoutSO)target;
            ComputeBounds(layout.entries);
            DrawGridCanvas(layout);
            DrawPalette(layout);

            if (_selectedIndex >= 0 && _selectedIndex < layout.entries.Count)
                DrawDetailPanel(layout);

            DrawToolbar(layout);

            bool needsRepaint = false;
            foreach (var en in layout.entries)
            {
                if (en.obstacleData != null && en.obstacleData.WorkplacePrefab != null
                    && GetPreview(en.obstacleData) == null)
                    needsRepaint = true;
            }
            foreach (var item in _paletteItems)
            {
                if (item != null && item.WorkplacePrefab != null && GetPreview(item) == null)
                    needsRepaint = true;
            }
            if (needsRepaint) Repaint();
        }
    }
}
