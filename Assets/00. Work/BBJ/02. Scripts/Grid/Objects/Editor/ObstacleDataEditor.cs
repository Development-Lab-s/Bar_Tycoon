using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BBJ.GridSystem.Objects.Editor
{
    [CustomEditor(typeof(TileSetData))]
    public class ObstacleDataEditor : UnityEditor.Editor
    {
        private const float CanvasHeight = 320f;
        private const float TileSizeBase = 48f;
        private const float ZoomMin = 0.4f;
        private const float ZoomMax = 2.0f;
        private const float ZoomStep = 0.1f;
        private const int RangeMin = 1;
        private const int RangeMax = 8;

        private static readonly Color ColEmpty        = new Color(0.22f, 0.24f, 0.28f, 1.00f);
        private static readonly Color ColBorderEmpty  = new Color(0.35f, 0.38f, 0.44f, 1.00f);
        private static readonly Color ColOrigin       = new Color(0.98f, 0.78f, 0.46f, 0.95f);
        private static readonly Color ColOriginBorder = new Color(0.93f, 0.62f, 0.09f, 1.00f);
        private static readonly Color ColOriginText   = new Color(0.30f, 0.15f, 0.01f, 1.00f);
        private static readonly Color ColBlocked      = new Color(0.80f, 0.22f, 0.22f, 0.90f);
        private static readonly Color ColBlockedBorder= new Color(1.00f, 0.40f, 0.40f, 1.00f);
        private static readonly Color ColHoverBlock   = new Color(1.00f, 0.45f, 0.45f, 0.35f);
        private static readonly Color ColText         = new Color(1.00f, 1.00f, 1.00f, 0.80f);
        private static readonly Color ColPanHint      = new Color(1.00f, 1.00f, 1.00f, 0.30f);

        private int _viewMinX = -3, _viewMaxX = 3;
        private int _viewMinY = -3, _viewMaxY = 3;
        private Vector2 _panOffset = Vector2.zero;
        private float _zoom = 1.0f;

        private bool    _isPanning;
        private Vector2 _panStartMouse;
        private Vector2 _panStartOffset;

        private bool       _isDragPainting;
        private bool       _dragErasing;
        private Vector2Int _lastDragCell = new Vector2Int(int.MinValue, int.MinValue);

        private HashSet<Vector2Int> _blocked = new HashSet<Vector2Int>();
        private Vector2Int _hovered = new Vector2Int(int.MinValue, int.MinValue);

        private float TileW => TileSizeBase * _zoom;
        private float TileH => TileSizeBase * 0.5f * _zoom;

        private void OnEnable() { LoadFromSO(); }

        private void LoadFromSO()
        {
            var so = (TileSetData)target;
            _blocked.Clear();
            if (so.BlockedOffsets != null)
                foreach (var v in so.BlockedOffsets) _blocked.Add(v);
        }

        private void SaveToSO()
        {
            var so = (TileSetData)target;
            Undo.RecordObject(so, "Edit Obstacle Tile");
            so.BlockedOffsets = _blocked.ToArray();
            EditorUtility.SetDirty(so);
        }

        private Vector2 GridToScreen(int gx, int gy, Rect canvas)
        {
            float midGx = (_viewMinX + _viewMaxX) * 0.5f;
            float midGy = (_viewMinY + _viewMaxY) * 0.5f;
            float pivotX = canvas.x + canvas.width * 0.5f - (midGx - midGy) * (TileW * 0.5f) + _panOffset.x;
            float pivotY = canvas.y + canvas.height * 0.5f + (midGx + midGy) * (TileH * 0.5f) + _panOffset.y;
            return new Vector2(pivotX + (gx - gy) * (TileW * 0.5f), pivotY - (gx + gy) * (TileH * 0.5f));
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

        private bool PaintTile(Vector2Int coord, bool erasing)
        {
            if (coord == Vector2Int.zero) return false;
            if (erasing)
            {
                if (!_blocked.Contains(coord)) return false;
                _blocked.Remove(coord);
                return true;
            }
            else
            {
                if (_blocked.Contains(coord)) return false;
                _blocked.Add(coord);
                return true;
            }
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

        private void DrawLabel(Vector2 c, string text, Color color)
        {
            int size = Mathf.RoundToInt(Mathf.Clamp(9f * _zoom, 7f, 12f));
            GUI.Label(
                new Rect(c.x - TileW * 0.5f, c.y - TileH * 0.5f, TileW, TileH),
                text,
                new GUIStyle(EditorStyles.label)
                {
                    fontSize = size,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = color }
                });
        }

        private void DrawRangeRow(string axisLabel, ref int refMin, ref int refMax)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(axisLabel, EditorStyles.miniBoldLabel, GUILayout.Width(14));
                GUILayout.Label("음:", EditorStyles.miniLabel, GUILayout.Width(24));
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(17)) && refMin > -RangeMax) { refMin--; Repaint(); }
                GUILayout.Label(refMin.ToString(), EditorStyles.miniLabel, GUILayout.Width(20));
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(17)) && refMin < -RangeMin) { refMin++; Repaint(); }
                GUILayout.Space(10);
                GUILayout.Label("양:", EditorStyles.miniLabel, GUILayout.Width(24));
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(17)) && refMax > RangeMin) { refMax--; Repaint(); }
                GUILayout.Label(refMax.ToString(), EditorStyles.miniLabel, GUILayout.Width(20));
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(17)) && refMax < RangeMax) { refMax++; Repaint(); }
                GUILayout.FlexibleSpace();
                GUILayout.Label($"범위 {refMin} ~ {refMax}", EditorStyles.miniLabel, GUILayout.Width(72));
            }
        }

        private List<Vector2Int> OutOfView(HashSet<Vector2Int> set) =>
            set.Where(v => v.x < _viewMinX || v.x > _viewMaxX ||
                           v.y < _viewMinY || v.y > _viewMaxY).ToList();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IsWalkable"));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("점유 오프셋 에디터 (쿼터뷰)", EditorStyles.boldLabel);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("그리드 뷰 범위  (에디터 전용)", EditorStyles.miniBoldLabel);
            DrawRangeRow("X", ref _viewMinX, ref _viewMaxX);
            DrawRangeRow("Y", ref _viewMinY, ref _viewMaxY);

            var outBlocked = OutOfView(_blocked);
            if (outBlocked.Count > 0)
            {
                string msg = $"점유 밖: {string.Join(", ", outBlocked.Select(v => $"({v.x},{v.y})"))}";
                EditorGUILayout.HelpBox(msg + "\n데이터는 SO에 유지됩니다.", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("줌", EditorStyles.miniBoldLabel, GUILayout.Width(24));
                if (GUILayout.Button("-", GUILayout.Width(22), GUILayout.Height(17)))
                { _zoom = Mathf.Max(ZoomMin, _zoom - ZoomStep); Repaint(); }
                EditorGUILayout.LabelField($"{_zoom * 100f:F0}%", EditorStyles.miniLabel, GUILayout.Width(36));
                if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(17)))
                { _zoom = Mathf.Min(ZoomMax, _zoom + ZoomStep); Repaint(); }
                GUILayout.Space(8);
                if (GUILayout.Button("뷰 초기화", GUILayout.Width(70), GUILayout.Height(17)))
                { _zoom = 1f; _panOffset = Vector2.zero; Repaint(); }
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(6);

            Rect canvasRect = GUILayoutUtility.GetRect(0, CanvasHeight);
            canvasRect.x = 0;
            canvasRect.width = EditorGUIUtility.currentViewWidth - 4f;
            EditorGUI.DrawRect(canvasRect, new Color(0.13f, 0.13f, 0.16f, 1f));
            Handles.DrawSolidRectangleWithOutline(
                new[] {
                    new Vector3(canvasRect.xMin, canvasRect.yMin),
                    new Vector3(canvasRect.xMax, canvasRect.yMin),
                    new Vector3(canvasRect.xMax, canvasRect.yMax),
                    new Vector3(canvasRect.xMin, canvasRect.yMax),
                }, Color.clear, new Color(1f, 1f, 1f, 0.12f));

            Event e = Event.current;
            bool inCanvas = canvasRect.Contains(e.mousePosition);
            Rect localCanvas = new Rect(0, 0, canvasRect.width, canvasRect.height);

            if (inCanvas && e.type == EventType.ScrollWheel)
            {
                float prev = _zoom;
                _zoom = Mathf.Clamp(_zoom - e.delta.y * ZoomStep * 0.5f, ZoomMin, ZoomMax);
                Vector2 ml = e.mousePosition - new Vector2(canvasRect.x + canvasRect.width * 0.5f,
                                                           canvasRect.y + canvasRect.height * 0.5f);
                _panOffset = ml + (_panOffset - ml) * (_zoom / prev);
                e.Use(); Repaint();
            }

            if (inCanvas && e.type == EventType.MouseDown && e.button == 1)
            { _isPanning = true; _panStartMouse = e.mousePosition; _panStartOffset = _panOffset; e.Use(); }
            if (_isPanning)
            {
                if (e.type == EventType.MouseDrag && e.button == 1)
                { _panOffset = _panStartOffset + (e.mousePosition - _panStartMouse); e.Use(); Repaint(); }
                if (e.type == EventType.MouseUp && e.button == 1)
                { _isPanning = false; e.Use(); }
            }

            if (inCanvas && e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                if (ScreenToGrid(localMouse, localCanvas, out Vector2Int hv) && hv != Vector2Int.zero)
                {
                    _dragErasing = _blocked.Contains(hv);
                    if (PaintTile(hv, _dragErasing)) SaveToSO();
                    _isDragPainting = true;
                    _lastDragCell = hv;
                }
                e.Use();
            }

            if (_isDragPainting)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                    if (ScreenToGrid(localMouse, localCanvas, out Vector2Int hv) && hv != _lastDragCell)
                    {
                        if (PaintTile(hv, _dragErasing)) SaveToSO();
                        _lastDragCell = hv;
                    }
                    Repaint(); e.Use();
                }
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    _isDragPainting = false;
                    _lastDragCell = new Vector2Int(int.MinValue, int.MinValue);
                    e.Use();
                }
            }

            if (!_isPanning && (e.type == EventType.MouseMove || (e.type == EventType.MouseDrag && e.button == 0)))
            {
                if (inCanvas)
                {
                    Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                    if (!ScreenToGrid(localMouse, localCanvas, out _hovered))
                        _hovered = new Vector2Int(int.MinValue, int.MinValue);
                }
                else _hovered = new Vector2Int(int.MinValue, int.MinValue);
                Repaint();
            }

            if (e.type == EventType.Repaint)
            {
                GUI.BeginClip(canvasRect);

                for (int gy = _viewMinY; gy <= _viewMaxY; gy++)
                    for (int gx = _viewMinX; gx <= _viewMaxX; gx++)
                    {
                        var coord  = new Vector2Int(gx, gy);
                        bool isOrig = coord == Vector2Int.zero;
                        bool isBlk  = _blocked.Contains(coord);
                        bool isHov  = coord == _hovered;
                        Vector2 ctr = GridToScreen(gx, gy, localCanvas);

                        if (ctr.x < -TileW || ctr.x > localCanvas.width + TileW ||
                            ctr.y < -TileH || ctr.y > localCanvas.height + TileH) continue;

                        Color fill, border;
                        if (isOrig)     { fill = ColOrigin;  border = ColOriginBorder; }
                        else if (isBlk) { fill = ColBlocked; border = ColBlockedBorder; }
                        else            { fill = ColEmpty;   border = ColBorderEmpty; }

                        DrawDiamond(ctr, fill, border);

                        if (isHov && !isOrig)
                            DrawDiamond(ctr, ColHoverBlock, Color.clear);

                        if (TileW >= 28f)
                        {
                            string lbl = isOrig ? "0,0" : $"{gx},{gy}";
                            Color tc = isOrig ? ColOriginText : ColText;
                            DrawLabel(ctr, lbl, tc);
                        }
                    }

                {
                    string tag = _isDragPainting
                        ? (_dragErasing ? "지우는 중 (점유)" : "그리는 중 (점유)")
                        : "브러시: 점유";
                    GUI.Label(new Rect(8, 6, 220, 18), tag,
                        new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.6f, 0.6f, 0.9f) } });
                }

                if (_isPanning)
                    GUI.Label(new Rect(0, 4, localCanvas.width - 6, 20), "패닝 중...",
                        new GUIStyle(EditorStyles.miniLabel)
                        { alignment = TextAnchor.UpperRight, normal = { textColor = ColPanHint } });

                DrawAxisLegend(localCanvas);
                GUI.EndClip();
            }

            EditorGUILayout.Space(2);
            GUILayout.Label(
                "우클릭 드래그: 패닝   |   스크롤: 줌   |   좌클릭/드래그: 타일 페인팅",
                new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 1f, 1f, 0.3f) } });

            EditorGUILayout.Space(6);

            GUI.color = new Color(1f, 0.6f, 0.6f, 1f);
            string blkText = _blocked.Count == 0 ? "BlockedOffsets: (없음)"
                : "BlockedOffsets: " + string.Join("  ", _blocked.OrderBy(v => v.y).ThenBy(v => v.x).Select(v => $"({v.x},{v.y})"));
            EditorGUILayout.LabelField(blkText, EditorStyles.miniLabel);
            GUI.color = Color.white;

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.color = new Color(1f, 0.6f, 0.6f, 1f);
                if (GUILayout.Button("점유 초기화", GUILayout.Width(80)))
                    if (EditorUtility.DisplayDialog("초기화", "모든 점유 타일을 삭제하시겠습니까?", "삭제", "취소"))
                    { _blocked.Clear(); SaveToSO(); }

                GUI.color = Color.white;
                if (GUILayout.Button("뷰 리셋", GUILayout.Width(60)))
                { _viewMinX = _viewMinY = -3; _viewMaxX = _viewMaxY = 3; _zoom = 1f; _panOffset = Vector2.zero; Repaint(); }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawAxisLegend(Rect canvas)
        {
            float ox = canvas.width - 80f;
            float oy = canvas.height - 52f;
            float len = 36f;
            Vector2 xEnd = new Vector2(ox + len * 0.707f, oy + len * 0.354f);
            Handles.color = new Color(1f, 0.40f, 0.40f, 0.85f);
            Handles.DrawLine(new Vector3(ox, oy), new Vector3(xEnd.x, xEnd.y));
            GUI.Label(new Rect(xEnd.x + 2, xEnd.y - 8, 24, 16), "-X",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.5f, 0.5f, 0.85f) } });
            Vector2 yEnd = new Vector2(ox - len * 0.707f, oy + len * 0.354f);
            Handles.color = new Color(0.40f, 1f, 0.75f, 0.85f);
            Handles.DrawLine(new Vector3(ox, oy), new Vector3(yEnd.x, yEnd.y));
            GUI.Label(new Rect(yEnd.x - 26, yEnd.y - 8, 24, 16), "-Y",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 1f, 0.85f, 0.85f) } });
        }
    }
}
