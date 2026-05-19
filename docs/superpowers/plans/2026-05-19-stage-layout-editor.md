# StageLayoutSO Visualization Editor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a `CustomEditor` for `StageLayoutSO` that renders an isometric grid in the Inspector with AssetPreview thumbnails per cell, entry selection, editable fields, and Add/Delete support.

**Architecture:** Single `StageLayoutEditor` class (IMGUI `CustomEditor`) in the existing `Grid/Objects/Editor/` folder. Reuses the same diamond coordinate system and `Handles.DrawSolidRectangleWithOutline` pattern from `ObstacleDataEditor`. Bounding box is auto-computed from entries each frame; pan/zoom is additive on top.

**Tech Stack:** Unity IMGUI (`UnityEditor`, `Handles`, `GUILayoutUtility`), `AssetPreview` for prefab thumbnails, `Undo`/`EditorUtility.SetDirty` for mutations.

---

## File Map

| Action | Path |
|---|---|
| **Create** | `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/Editor/StageLayoutEditor.cs` |

No other files are modified.

---

## Task 1: Scaffold — Empty Class with State Fields

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/Editor/StageLayoutEditor.cs`

- [ ] **Step 1: Create the file**

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BBJ.GridSystem.Objects;

namespace BBJ.GridSystem.Objects.Editor
{
    [CustomEditor(typeof(StageLayoutSO))]
    public class StageLayoutEditor : UnityEditor.Editor
    {
        // ── 렌더 상수 ───────────────────────────────────
        private const float CanvasHeight = 300f;
        private const float TileSizeBase = 48f;
        private const int   BoundPad     = 1;

        // ── 컬러 팔레트 ─────────────────────────────────
        private static readonly Color ColEmpty          = new Color(0.22f, 0.24f, 0.28f, 1.00f);
        private static readonly Color ColBorderEmpty    = new Color(0.35f, 0.38f, 0.44f, 1.00f);
        private static readonly Color ColOccupied       = new Color(0.25f, 0.45f, 0.65f, 0.85f);
        private static readonly Color ColOccupiedBorder = new Color(0.45f, 0.70f, 1.00f, 1.00f);
        private static readonly Color ColSelected       = new Color(1.00f, 0.85f, 0.15f, 0.90f);
        private static readonly Color ColSelectedBorder = new Color(1.00f, 1.00f, 0.00f, 1.00f);

        // ── 에디터 전용 상태 ────────────────────────────
        private int    _selectedIndex = -1;
        private Dictionary<ObjectData, Texture2D> _previewCache = new Dictionary<ObjectData, Texture2D>();

        private Vector2 _panOffset     = Vector2.zero;
        private float   _zoom          = 1f;
        private bool    _isPanning     = false;
        private Vector2 _panStartMouse;
        private Vector2 _panStartOffset;

        // ── 뷰 범위 (ComputeBounds가 매 프레임 갱신) ───
        private int _viewMinX, _viewMaxX, _viewMinY, _viewMaxY;

        // ── 파생 값 ─────────────────────────────────────
        private float TileW => TileSizeBase * _zoom;
        private float TileH => TileSizeBase * 0.5f * _zoom;

        private void OnDisable()
        {
            _previewCache.Clear();
            _selectedIndex = -1;
        }

        public override void OnInspectorGUI()
        {
            var layout = (StageLayoutSO)target;
            EditorGUILayout.LabelField("StageLayoutSO Visualizer — WIP", EditorStyles.boldLabel);
        }
    }
}
```

- [ ] **Step 2: Verify it compiles in Unity**

  Unity Console에 에러 없이 컴파일 완료되는지 확인.  
  `Assets/00. Work/BBJ/05. SO/Layout/StageLayout.asset`을 Inspector에서 열었을 때 "StageLayoutSO Visualizer — WIP" 레이블이 표시되면 성공.

---

## Task 2: Grid Canvas — Diamond Cells, Pan/Zoom, Auto-fit

**Files:**
- Modify: `StageLayoutEditor.cs`

- [ ] **Step 1: Add coordinate helper methods**

  `OnDisable()` 아래에 다음 세 메서드를 추가한다.

```csharp
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
```

- [ ] **Step 2: Add `DrawGridCanvas` method**

  `DrawDiamond` 아래에 추가한다.

```csharp
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

    Event e = Event.current;
    bool  inCanvas   = canvasRect.Contains(e.mousePosition);
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

    // 우클릭 패닝
    if (inCanvas && e.type == EventType.MouseDown && e.button == 1)
    { _isPanning = true; _panStartMouse = e.mousePosition; _panStartOffset = _panOffset; e.Use(); }
    if (_isPanning)
    {
        if (e.type == EventType.MouseDrag && e.button == 1)
        { _panOffset = _panStartOffset + (e.mousePosition - _panStartMouse); e.Use(); Repaint(); }
        if (e.type == EventType.MouseUp && e.button == 1)
        { _isPanning = false; e.Use(); }
    }

    // 좌클릭 선택 (Task 3에서 구현 — 여기선 자리만 잡음)
    if (inCanvas && e.type == EventType.MouseDown && e.button == 0)
    {
        Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
        if (ScreenToGrid(localMouse, localCanvas, out Vector2Int cell))
        {
            int idx = layout.entries.FindIndex(en => en.cellIndex == cell);
            _selectedIndex = idx; // -1 if empty cell
        }
        else _selectedIndex = -1;
        e.Use(); Repaint();
    }

    // 렌더
    if (e.type == EventType.Repaint)
    {
        GUI.BeginClip(canvasRect);

        for (int gy = _viewMinY; gy <= _viewMaxY; gy++)
            for (int gx = _viewMinX; gx <= _viewMaxX; gx++)
            {
                Vector2 ctr = GridToScreen(gx, gy, localCanvas);
                if (ctr.x < -TileW || ctr.x > localCanvas.width  + TileW ||
                    ctr.y < -TileH || ctr.y > localCanvas.height + TileH) continue;

                var coord    = new Vector2Int(gx, gy);
                int entryIdx = layout.entries.FindIndex(en => en.cellIndex == coord);
                bool isOccupied = entryIdx >= 0;
                bool isSelected = isOccupied && entryIdx == _selectedIndex;

                Color fill   = isSelected ? ColSelected   : isOccupied ? ColOccupied   : ColEmpty;
                Color border = isSelected ? ColSelectedBorder : isOccupied ? ColOccupiedBorder : ColBorderEmpty;
                DrawDiamond(ctr, fill, border);
            }

        // 캔버스 좌상단 hint
        GUI.Label(new Rect(8, 6, 200, 18), $"entries: {layout.entries.Count}",
            new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 1f, 1f, 0.5f) } });

        if (_isPanning)
            GUI.Label(new Rect(0, 4, localCanvas.width - 6, 20), "패닝 중...",
                new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.UpperRight, normal = { textColor = new Color(1f, 1f, 1f, 0.3f) } });

        GUI.EndClip();
    }

    GUILayout.Label("우클릭 드래그: 패닝   |   스크롤: 줌   |   좌클릭: 선택",
        new GUIStyle(EditorStyles.miniLabel)
        { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 1f, 1f, 0.3f) } });
}
```

- [ ] **Step 3: Wire into `OnInspectorGUI`**

  기존 `OnInspectorGUI` 전체를 다음으로 교체한다.

```csharp
public override void OnInspectorGUI()
{
    var layout = (StageLayoutSO)target;
    ComputeBounds(layout.entries);
    DrawGridCanvas(layout);
}
```

- [ ] **Step 4: Verify in Unity**

  `StageLayout.asset` Inspector에서:
  - 다크 배경 캔버스(300px)가 보인다.
  - 70개 항목 위치에 파란색 다이아몬드 셀이 표시된다.
  - 빈 셀은 회색.
  - 우클릭 드래그로 패닝, 스크롤로 줌이 동작한다.
  - 좌클릭 시 해당 셀이 노란색으로 하이라이트된다.

---

## Task 3: AssetPreview Thumbnails in Cells

**Files:**
- Modify: `StageLayoutEditor.cs`

- [ ] **Step 1: Add `GetPreview` helper**

  `DrawDiamond` 아래에 추가한다.

```csharp
private Texture2D GetPreview(ObjectData data)
{
    if (data == null || data.Prefab == null) return null;
    if (_previewCache.TryGetValue(data, out Texture2D tex) && tex != null) return tex;
    tex = AssetPreview.GetAssetPreview(data.Prefab);
    if (tex != null) _previewCache[data] = tex;
    return tex;
}
```

- [ ] **Step 2: Draw thumbnail inside each occupied cell**

  `DrawGridCanvas` 내부 렌더 루프에서 `DrawDiamond` 호출 직후, `GUI.EndClip()` 이전에 아래를 추가한다.  
  (루프 안에서 `DrawDiamond` 호출 바로 뒤에 삽입)

```csharp
// 썸네일 — 점유된 셀에만
if (isOccupied)
{
    var data = layout.entries[entryIdx].obstacleData;
    Texture2D preview = GetPreview(data);
    if (preview != null)
    {
        float hw = TileW * 0.42f;
        float hh = TileH * 0.42f;
        GUI.DrawTexture(new Rect(ctr.x - hw, ctr.y - hh, hw * 2f, hh * 2f),
                        preview, ScaleMode.ScaleToFit);
    }
}
```

- [ ] **Step 3: Trigger repaint while previews are loading**

  `DrawGridCanvas` 호출 뒤 (`OnInspectorGUI` 안)에 추가한다.

```csharp
// 아직 로딩 중인 프리뷰가 있으면 계속 Repaint
if (layout.entries.Any(en => en.obstacleData != null
                           && en.obstacleData.Prefab != null
                           && GetPreview(en.obstacleData) == null))
    Repaint();
```

- [ ] **Step 4: Set preview cache size in `OnEnable`**

  `OnDisable` 바로 위에 추가한다.

```csharp
private void OnEnable()
{
    var layout = (StageLayoutSO)target;
    AssetPreview.SetPreviewTextureCacheSize(layout.entries.Count + 16);
}
```

- [ ] **Step 5: Verify in Unity**

  Inspector를 열면 각 파란/노란 다이아몬드 셀 위에 프리팹 썸네일(흰 배경 미리보기)이 표시된다.  
  처음 열 때 잠깐 비어 있다가 자동으로 채워지면 정상이다.

---

## Task 4: Detail Panel — AssetPreview 128px + 편집 필드 + Delete

**Files:**
- Modify: `StageLayoutEditor.cs`

- [ ] **Step 1: Add `DrawDetailPanel` method**

  `DrawGridCanvas` 아래에 추가한다.

```csharp
private void DrawDetailPanel(StageLayoutSO layout)
{
    EditorGUILayout.Space(8);
    EditorGUILayout.LabelField("선택된 항목", EditorStyles.boldLabel);

    var entry = layout.entries[_selectedIndex];

    // ── AssetPreview 128×128 ────────────────────────
    if (entry.obstacleData != null && entry.obstacleData.Prefab != null)
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

    // ── cellIndex 필드 ──────────────────────────────
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

    // ── obstacleData 필드 ───────────────────────────
    EditorGUI.BeginChangeCheck();
    var newData = (ObjectData)EditorGUILayout.ObjectField(
        "obstacleData", entry.obstacleData, typeof(ObjectData), false);
    if (EditorGUI.EndChangeCheck())
    {
        Undo.RecordObject(layout, "Edit obstacleData");
        _previewCache.Remove(entry.obstacleData);
        PlacedObstacleEntry modified = entry;
        modified.obstacleData = newData;
        layout.entries[_selectedIndex] = modified;
        EditorUtility.SetDirty(layout);
    }

    // ── Delete 버튼 ─────────────────────────────────
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
```

- [ ] **Step 2: Wire into `OnInspectorGUI`**

  `OnInspectorGUI` 전체를 다음으로 교체한다.

```csharp
public override void OnInspectorGUI()
{
    var layout = (StageLayoutSO)target;
    ComputeBounds(layout.entries);
    DrawGridCanvas(layout);

    if (_selectedIndex >= 0 && _selectedIndex < layout.entries.Count)
        DrawDetailPanel(layout);

    if (layout.entries.Any(en => en.obstacleData != null
                               && en.obstacleData.Prefab != null
                               && GetPreview(en.obstacleData) == null))
        Repaint();
}
```

  (Task 3 Step 3에서 넣은 `Repaint()` 호출은 이제 이 블록으로 통합되었으므로 기존 것은 제거한다.)

- [ ] **Step 3: Verify in Unity**

  - 셀을 좌클릭하면 아래에 Detail Panel이 펼쳐진다.
  - 프리팹 AssetPreview 128×128이 표시된다.
  - `cellIndex` 수정 시 그리드 썸네일이 즉시 이동한다.
  - `obstacleData` 변경 시 썸네일이 바뀐다.
  - Delete 클릭 시 항목이 사라지고 패널이 닫힌다.
  - Ctrl+Z(Undo)로 모든 변경이 되돌아간다.

---

## Task 5: Toolbar — Add Entry + Entry Count

**Files:**
- Modify: `StageLayoutEditor.cs`

- [ ] **Step 1: Add `DrawToolbar` method**

  `DrawDetailPanel` 아래에 추가한다.

```csharp
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
```

- [ ] **Step 2: Wire into `OnInspectorGUI`**

  `OnInspectorGUI` 전체를 최종 형태로 교체한다.

```csharp
public override void OnInspectorGUI()
{
    var layout = (StageLayoutSO)target;
    ComputeBounds(layout.entries);
    DrawGridCanvas(layout);

    if (_selectedIndex >= 0 && _selectedIndex < layout.entries.Count)
        DrawDetailPanel(layout);

    DrawToolbar(layout);

    if (layout.entries.Any(en => en.obstacleData != null
                               && en.obstacleData.Prefab != null
                               && GetPreview(en.obstacleData) == null))
        Repaint();
}
```

- [ ] **Step 3: Verify in Unity**

  - "Add Entry" 버튼 클릭 시 `cellIndex=(0,0)`, `obstacleData=null`인 항목이 추가되고 자동 선택된다.
  - 우하단 `entries: N` 카운트가 실시간으로 업데이트된다.
  - 새 항목의 `cellIndex`를 Detail Panel에서 수정하면 그리드 위에 썸네일(없으면 파란 다이아몬드)이 해당 위치로 이동한다.
  - Ctrl+Z로 추가 작업이 되돌아간다.
