using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BBJ.GridSystem.Objects.Editor
{
    [CustomEditor(typeof(ObjectData))]
    public class ObstacleDataEditor : UnityEditor.Editor
    {
        // ──────────────────────────────────────────────
        // 렌더 상수
        // ──────────────────────────────────────────────
        private const float CanvasHeight = 320f;
        private const float TileSizeBase = 48f;   // zoom=1 일 때 타일 가로 픽셀
        private const float ZoomMin = 0.4f;
        private const float ZoomMax = 2.0f;
        private const float ZoomStep = 0.1f;
        private const int RangeMin = 1;
        private const int RangeMax = 8;

        // ──────────────────────────────────────────────
        // 컬러 팔레트
        // ──────────────────────────────────────────────
        private static readonly Color ColEmpty = new Color(0.22f, 0.24f, 0.28f, 1.00f);
        private static readonly Color ColBorderEmpty = new Color(0.35f, 0.38f, 0.44f, 1.00f);
        private static readonly Color ColOrigin = new Color(0.98f, 0.78f, 0.46f, 0.95f);
        private static readonly Color ColOriginBorder = new Color(0.93f, 0.62f, 0.09f, 1.00f);
        private static readonly Color ColOriginText = new Color(0.30f, 0.15f, 0.01f, 1.00f);
        private static readonly Color ColBlocked = new Color(0.80f, 0.22f, 0.22f, 0.90f);
        private static readonly Color ColBlockedBorder = new Color(1.00f, 0.40f, 0.40f, 1.00f);
        private static readonly Color ColInteract = new Color(0.18f, 0.70f, 0.60f, 0.85f);
        private static readonly Color ColInteractBorder = new Color(0.40f, 1.00f, 0.85f, 1.00f);
        private static readonly Color ColHoverBlock = new Color(1.00f, 0.45f, 0.45f, 0.35f);
        private static readonly Color ColHoverInteract = new Color(0.40f, 1.00f, 0.85f, 0.35f);
        private static readonly Color ColHoverConflict = new Color(1.00f, 0.80f, 0.10f, 0.45f);
        private static readonly Color ColText = new Color(1.00f, 1.00f, 1.00f, 0.80f);
        private static readonly Color ColPanHint = new Color(1.00f, 1.00f, 1.00f, 0.30f);

        // ──────────────────────────────────────────────
        // 브러시 타입
        // ──────────────────────────────────────────────
        private enum BrushMode { Blocked, Interact }
        private BrushMode _brush = BrushMode.Blocked;

        // ──────────────────────────────────────────────
        // 에디터 전용 상태
        // ──────────────────────────────────────────────
        private int _viewMinX = -3, _viewMaxX = 3;
        private int _viewMinY = -3, _viewMaxY = 3;
        private Vector2 _panOffset = Vector2.zero;
        private float _zoom = 1.0f;

        // 패닝
        private bool _isPanning = false;
        private Vector2 _panStartMouse;
        private Vector2 _panStartOffset;

        // 드래그 페인팅
        private bool _isDragPainting = false;
        private bool _dragErasing = false;        // 드래그 시작 시점의 동작(추가/제거)
        private Vector2Int _lastDragCell = new Vector2Int(int.MinValue, int.MinValue);

        // SO 데이터 캐시
        private HashSet<Vector2Int> _blocked = new HashSet<Vector2Int>();
        private HashSet<Vector2Int> _interact = new HashSet<Vector2Int>();
        private Vector2Int _hovered = new Vector2Int(int.MinValue, int.MinValue);

        // ──────────────────────────────────────────────
        // 파생 값
        // ──────────────────────────────────────────────
        private float TileW => TileSizeBase * _zoom;
        private float TileH => TileSizeBase * 0.5f * _zoom;  // 아이소 비율 1:2
        private int ViewCols => _viewMaxX - _viewMinX + 1;
        private int ViewRows => _viewMaxY - _viewMinY + 1;

        private void OnEnable() => LoadFromSO();

        // ──────────────────────────────────────────────
        // SO ↔ HashSet 동기화
        // ──────────────────────────────────────────────
        private void LoadFromSO()
        {
            var so = (ObjectData)target;
            _blocked.Clear();
            if (so.BlockedOffsets != null) foreach (var v in so.BlockedOffsets) _blocked.Add(v);
            _interact.Clear();
            if (so.InteractOffsets != null) foreach (var v in so.InteractOffsets) _interact.Add(v);
        }

        private void SaveToSO()
        {
            var so = (ObjectData)target;
            Undo.RecordObject(so, "Edit Obstacle Tile");
            so.BlockedOffsets = _blocked.ToArray();
            so.InteractOffsets = _interact.ToArray();
            EditorUtility.SetDirty(so);
        }

        // ──────────────────────────────────────────────
        // 좌표 변환
        //   screen.x = pivot - (gx - gy) * TileW/2
        //   screen.y = pivot + (gx + gy) * TileH/2
        //   ↙ = -X (gx 감소 → 왼쪽 아래)
        //   ↘ = -Y (gy 감소 → 오른쪽 아래)
        // ──────────────────────────────────────────────
        private Vector2 GridToScreen(int gx, int gy, Rect canvas)
        {
            // ↙ = -X : gx 감소 → screen.x 감소, screen.y 증가(아래)
            // ↘ = -Y : gy 감소 → screen.x 증가, screen.y 증가(아래)
            // screen.x = pivot + (gx - gy) * TileW/2
            // screen.y = pivot - (gx + gy) * TileH/2
            float midGx = (_viewMinX + _viewMaxX) * 0.5f;
            float midGy = (_viewMinY + _viewMaxY) * 0.5f;

            float pivotX = canvas.x + canvas.width * 0.5f
                           - (midGx - midGy) * (TileW * 0.5f)
                           + _panOffset.x;
            float pivotY = canvas.y + canvas.height * 0.5f
                           + (midGx + midGy) * (TileH * 0.5f)
                           + _panOffset.y;

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

        // ──────────────────────────────────────────────
        // 타일 페인트 로직 (클릭 & 드래그 공통)
        // ──────────────────────────────────────────────

        /// <summary>
        /// dragErasing=true  → 해당 타일을 제거
        /// dragErasing=false → 해당 타일을 추가 (겹침 방지 포함)
        /// 반환값: 실제로 변경이 있었으면 true
        /// </summary>
        private bool PaintTile(Vector2Int coord, bool erasing)
        {
            if (coord == Vector2Int.zero) return false;

            var primary = _brush == BrushMode.Blocked ? _blocked : _interact;
            var secondary = _brush == BrushMode.Blocked ? _interact : _blocked;

            if (erasing)
            {
                if (!primary.Contains(coord)) return false;
                primary.Remove(coord);
                return true;
            }
            else
            {
                if (primary.Contains(coord)) return false;   // 이미 있음
                if (secondary.Contains(coord)) return false;   // 겹침 불가
                primary.Add(coord);
                return true;
            }
        }

        // ──────────────────────────────────────────────
        // 마름모 그리기
        // ──────────────────────────────────────────────
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

        // ──────────────────────────────────────────────
        // 뷰 범위 컨트롤 한 행
        // ──────────────────────────────────────────────
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

        // ──────────────────────────────────────────────
        // OnInspectorGUI
        // ──────────────────────────────────────────────
        public override void OnInspectorGUI()
        {
            // 기본 필드
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Prefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IsWalkable"));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("타일 오프셋 에디터 (쿼터뷰)", EditorStyles.boldLabel);

            // ── 브러시 선택 툴바 ─────────────────────
            EditorGUILayout.Space(4);
            Rect toolbarBg = GUILayoutUtility.GetRect(0, 44f);
            EditorGUI.DrawRect(toolbarBg, new Color(0.10f, 0.10f, 0.13f, 1f));

            float tbPad = 6f;
            float btnW = (toolbarBg.width - tbPad * 3f) * 0.5f;
            float btnH = toolbarBg.height - tbPad * 2f;
            Rect btnBlocked = new Rect(toolbarBg.x + tbPad, toolbarBg.y + tbPad, btnW, btnH);
            Rect btnInteract = new Rect(toolbarBg.x + tbPad * 2f + btnW, toolbarBg.y + tbPad, btnW, btnH);

            if (_brush == BrushMode.Blocked) EditorGUI.DrawRect(btnBlocked, new Color(0.80f, 0.22f, 0.22f, 0.35f));
            if (_brush == BrushMode.Interact) EditorGUI.DrawRect(btnInteract, new Color(0.18f, 0.70f, 0.60f, 0.35f));

            var bStyle = new GUIStyle(EditorStyles.miniButton)
            { fontSize = 11, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

            GUI.color = _brush == BrushMode.Blocked
                ? new Color(1.0f, 0.60f, 0.60f, 1f)
                : new Color(0.65f, 0.38f, 0.38f, 1f);
            if (GUI.Button(btnBlocked, "■  점유 (Blocked)", bStyle))
                _brush = BrushMode.Blocked;

            GUI.color = _brush == BrushMode.Interact
                ? new Color(0.55f, 1.0f, 0.88f, 1f)
                : new Color(0.30f, 0.60f, 0.52f, 1f);
            if (GUI.Button(btnInteract, "◆  상호작용 (Interact)", bStyle))
                _brush = BrushMode.Interact;

            GUI.color = Color.white;

            // 브러시 안내
            EditorGUILayout.Space(2);
            string brushDesc = _brush == BrushMode.Blocked
                ? "● 점유 브러시 — 이동 불가 타일"
                : "● 상호작용 브러시 — 접근·사용 가능 타일 (이동 가능)";
            Color brushCol = _brush == BrushMode.Blocked
                ? new Color(1f, 0.6f, 0.6f, 1f) : new Color(0.5f, 1f, 0.88f, 1f);
            GUILayout.Label(brushDesc, new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = brushCol } });

            EditorGUILayout.Space(6);

            // ── 뷰 범위 컨트롤 ───────────────────────
            EditorGUILayout.LabelField("그리드 뷰 범위  (에디터 전용)", EditorStyles.miniBoldLabel);
            DrawRangeRow("X", ref _viewMinX, ref _viewMaxX);
            DrawRangeRow("Y", ref _viewMinY, ref _viewMaxY);

            var outBlocked = OutOfView(_blocked);
            var outInteract = OutOfView(_interact);
            if (outBlocked.Count > 0 || outInteract.Count > 0)
            {
                string msg = "";
                if (outBlocked.Count > 0) msg += $"점유 밖: {string.Join(", ", outBlocked.Select(v => $"({v.x},{v.y})"))}\n";
                if (outInteract.Count > 0) msg += $"상호작용 밖: {string.Join(", ", outInteract.Select(v => $"({v.x},{v.y})"))}";
                EditorGUILayout.HelpBox(msg.Trim() + "\n데이터는 SO에 유지됩니다.", MessageType.Warning);
            }

            // ── 줌 컨트롤 ────────────────────────────
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

            // ── 고정 높이 캔버스 ─────────────────────
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

            // ── 이벤트 처리 ──────────────────────────
            Event e = Event.current;
            bool inCanvas = canvasRect.Contains(e.mousePosition);

            // 로컬 캔버스 (BeginClip 이후 좌표계용)
            Rect localCanvas = new Rect(0, 0, canvasRect.width, canvasRect.height);

            // 스크롤 휠 줌
            if (inCanvas && e.type == EventType.ScrollWheel)
            {
                float prev = _zoom;
                _zoom = Mathf.Clamp(_zoom - e.delta.y * ZoomStep * 0.5f, ZoomMin, ZoomMax);
                Vector2 ml = e.mousePosition - new Vector2(canvasRect.x + canvasRect.width * 0.5f,
                                                           canvasRect.y + canvasRect.height * 0.5f);
                _panOffset = ml + (_panOffset - ml) * (_zoom / prev);
                e.Use(); Repaint();
            }

            // 우클릭 드래그 = 패닝
            if (inCanvas && e.type == EventType.MouseDown && e.button == 1)
            {
                _isPanning = true; _panStartMouse = e.mousePosition; _panStartOffset = _panOffset;
                e.Use();
            }
            if (_isPanning)
            {
                if (e.type == EventType.MouseDrag && e.button == 1)
                { _panOffset = _panStartOffset + (e.mousePosition - _panStartMouse); e.Use(); Repaint(); }
                if (e.type == EventType.MouseUp && e.button == 1)
                { _isPanning = false; e.Use(); }
            }

            // ── 좌클릭 / 드래그 페인팅 ───────────────
            if (inCanvas && e.type == EventType.MouseDown && e.button == 0)
            {
                // 로컬 좌표로 변환해서 hit test
                Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                if (ScreenToGrid(localMouse, localCanvas, out Vector2Int hv) && hv != Vector2Int.zero)
                {
                    // 시작 타일이 이미 현재 브러시 타입으로 칠해져 있으면 → 지우기 모드
                    var primary = _brush == BrushMode.Blocked ? _blocked : _interact;
                    _dragErasing = primary.Contains(hv);

                    if (PaintTile(hv, _dragErasing))
                        SaveToSO();

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
                        bool changed = PaintTile(hv, _dragErasing);
                        if (changed) SaveToSO();
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

            // 호버 갱신 (패닝/드래그 중이 아닐 때만)
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

            // ── 타일 렌더 ────────────────────────────
            if (e.type == EventType.Repaint)
            {
                GUI.BeginClip(canvasRect);

                // 페인터 알고리즘: screen.y∝(gx+gy), 합이 작은 것(위/뒤)부터 → gy/gx 오름차순
                for (int gy = _viewMinY; gy <= _viewMaxY; gy++)
                    for (int gx = _viewMinX; gx <= _viewMaxX; gx++)
                    {
                        var coord = new Vector2Int(gx, gy);
                        bool isOrig = coord == Vector2Int.zero;
                        bool isBlk = _blocked.Contains(coord);
                        bool isInter = _interact.Contains(coord);
                        bool isHov = coord == _hovered;
                        Vector2 ctr = GridToScreen(gx, gy, localCanvas);

                        // 화면 밖 컬링
                        if (ctr.x < -TileW || ctr.x > localCanvas.width + TileW ||
                            ctr.y < -TileH || ctr.y > localCanvas.height + TileH) continue;

                        // 타일 색상
                        Color fill, border;
                        if (isOrig) { fill = ColOrigin; border = ColOriginBorder; }
                        else if (isBlk) { fill = ColBlocked; border = ColBlockedBorder; }
                        else if (isInter) { fill = ColInteract; border = ColInteractBorder; }
                        else { fill = ColEmpty; border = ColBorderEmpty; }

                        DrawDiamond(ctr, fill, border);

                        // 호버 오버레이
                        if (isHov && !isOrig)
                        {
                            bool conflict = (_brush == BrushMode.Blocked && isInter) ||
                                            (_brush == BrushMode.Interact && isBlk);
                            Color hoverCol = conflict ? ColHoverConflict
                                           : _brush == BrushMode.Blocked ? ColHoverBlock
                                           : ColHoverInteract;
                            DrawDiamond(ctr, hoverCol, Color.clear);
                        }

                        // 라벨 (타일이 충분히 클 때)
                        if (TileW >= 28f)
                        {
                            string lbl = isOrig ? "0,0" : $"{gx},{gy}";
                            Color tc = isOrig ? ColOriginText : ColText;
                            DrawLabel(ctr, lbl, tc);
                        }
                    }

                // 캔버스 좌상단 브러시 표시
                {
                    bool isBlocked = _brush == BrushMode.Blocked;
                    string tag = _isDragPainting
                        ? (_dragErasing ? (isBlocked ? "지우는 중 (점유)" : "지우는 중 (상호작용)")
                                        : (isBlocked ? "그리는 중 (점유)" : "그리는 중 (상호작용)"))
                        : (isBlocked ? "브러시: 점유" : "브러시: 상호작용");
                    Color tagCol = isBlocked ? new Color(1f, 0.6f, 0.6f, 0.9f) : new Color(0.5f, 1f, 0.88f, 0.9f);
                    GUI.Label(new Rect(8, 6, 220, 18), tag,
                        new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, normal = { textColor = tagCol } });
                }

                // 패닝 힌트
                if (_isPanning)
                    GUI.Label(new Rect(0, 4, localCanvas.width - 6, 20), "패닝 중...",
                        new GUIStyle(EditorStyles.miniLabel)
                        { alignment = TextAnchor.UpperRight, normal = { textColor = ColPanHint } });

                // 좌표축 범례 (우하단)
                DrawAxisLegend(localCanvas);

                GUI.EndClip();
            }

            // 조작 안내
            EditorGUILayout.Space(2);
            GUILayout.Label(
                "우클릭 드래그: 패닝   |   스크롤: 줌   |   좌클릭/드래그: 타일 페인팅",
                new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 1f, 1f, 0.3f) } });

            // 데이터 요약
            EditorGUILayout.Space(6);
            DrawDataSummary();

            // 버튼
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.color = new Color(1f, 0.6f, 0.6f, 1f);
                if (GUILayout.Button("점유 초기화", GUILayout.Width(80)))
                    if (EditorUtility.DisplayDialog("초기화", "모든 점유 타일을 삭제하시겠습니까?", "삭제", "취소"))
                    { _blocked.Clear(); SaveToSO(); }

                GUI.color = new Color(0.5f, 1f, 0.88f, 1f);
                if (GUILayout.Button("상호작용 초기화", GUILayout.Width(100)))
                    if (EditorUtility.DisplayDialog("초기화", "모든 상호작용 타일을 삭제하시겠습니까?", "삭제", "취소"))
                    { _interact.Clear(); SaveToSO(); }

                GUI.color = Color.white;
                if (GUILayout.Button("뷰 리셋", GUILayout.Width(60)))
                { _viewMinX = _viewMinY = -3; _viewMaxX = _viewMaxY = 3; _zoom = 1f; _panOffset = Vector2.zero; Repaint(); }
                GUILayout.FlexibleSpace();
            }
        }

        // ──────────────────────────────────────────────
        // 축 방향 범례 (캔버스 우하단)
        // ──────────────────────────────────────────────
        private void DrawAxisLegend(Rect canvas)
        {
            float ox = canvas.width - 80f;
            float oy = canvas.height - 52f;
            float len = 36f;

            // ↘ = -X 축  (gx 증가 → screen.x 증가 → 오른쪽아래)
            Vector2 xEnd = new Vector2(ox + len * 0.707f, oy + len * 0.354f);
            Handles.color = new Color(1f, 0.40f, 0.40f, 0.85f);
            Handles.DrawLine(new Vector3(ox, oy), new Vector3(xEnd.x, xEnd.y));
            GUI.Label(new Rect(xEnd.x + 2, xEnd.y - 8, 24, 16), "-X",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.5f, 0.5f, 0.85f) } });

            // ↙ = -Y 축  (gy 증가 → screen.x 감소 → 왼쪽아래)
            Vector2 yEnd = new Vector2(ox - len * 0.707f, oy + len * 0.354f);
            Handles.color = new Color(0.40f, 1f, 0.75f, 0.85f);
            Handles.DrawLine(new Vector3(ox, oy), new Vector3(yEnd.x, yEnd.y));
            GUI.Label(new Rect(yEnd.x - 26, yEnd.y - 8, 24, 16), "-Y",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 1f, 0.85f, 0.85f) } });
        }

        // ──────────────────────────────────────────────
        // 데이터 요약
        // ──────────────────────────────────────────────
        private void DrawDataSummary()
        {
            Color prev = GUI.color;

            GUI.color = new Color(1f, 0.6f, 0.6f, 1f);
            string blkText = _blocked.Count == 0 ? "BlockedOffsets: (없음)"
                : "BlockedOffsets: " + string.Join("  ", _blocked.OrderBy(v => v.y).ThenBy(v => v.x).Select(v => $"({v.x},{v.y})"));
            EditorGUILayout.LabelField(blkText, EditorStyles.miniLabel);

            GUI.color = new Color(0.5f, 1f, 0.88f, 1f);
            string intText = _interact.Count == 0 ? "InteractOffsets: (없음)"
                : "InteractOffsets: " + string.Join("  ", _interact.OrderBy(v => v.y).ThenBy(v => v.x).Select(v => $"({v.x},{v.y})"));
            EditorGUILayout.LabelField(intText, EditorStyles.miniLabel);

            GUI.color = prev;
        }
    }
}