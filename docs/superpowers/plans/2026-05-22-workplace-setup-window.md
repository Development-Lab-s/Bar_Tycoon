# Workplace Setup Window Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tools 메뉴에서 열 수 있는 EditorWindow를 만들어 Workplace 프리팹의 TileSet(BlockedOffsets)과 비주얼 자식 GO의 localPosition(visual offset)을 편집하고, 프리팹으로 저장/불러올 수 있게 한다.

**Architecture:** 마커 컴포넌트 `TycoonVisualRoot`으로 비주얼 자식 GO를 식별한다. `WorkplaceSetupWindow`(EditorWindow)에서 프리팹을 ObjectField로 로드하여 TileSetData와 TycoonVisualRoot의 localPosition을 읽어오고, 아이소메트릭 캔버스에서 타일 점유 편집 + 스프라이트 미리보기를 제공한다. 저장 시 `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset` 패턴으로 프리팹 에셋을 직접 수정한다.

**Tech Stack:** Unity Editor API, PrefabUtility, SerializedObject, Handles, GUI/GUILayout

---

## File Map

| Action | Path | Role |
|--------|------|------|
| **Create** | `Assets/00. Work/BBJ/02. Scripts/Workplace/TycoonVisualRoot.cs` | 비주얼 GO 식별 마커 컴포넌트 |
| **Create** | `Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceSetupWindow.cs` | Tool 창 본체 |

---

## Task 1: TycoonVisualRoot 마커 컴포넌트

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Workplace/TycoonVisualRoot.cs`

- [ ] **Step 1: 파일 작성**

```csharp
using UnityEngine;

namespace BBJ.WorkplaceSystem
{
    /// <summary>
    /// WorkplaceSetupWindow가 비주얼 자식 GO를 식별하기 위한 마커 컴포넌트.
    /// 런타임 로직 없음. Workplace 프리팹의 비주얼 루트 GO에만 부착.
    /// </summary>
    public class TycoonVisualRoot : MonoBehaviour { }
}
```

- [ ] **Step 2: Unity에서 컴파일 확인**

Unity 콘솔에 에러 없음 확인. 기존 Workplace 프리팹 자식 중 비주얼 GO에 `TycoonVisualRoot` 컴포넌트를 수동으로 부착한다.

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/TycoonVisualRoot.cs"
git commit -m "feat: TycoonVisualRoot 마커 컴포넌트 추가"
```

---

## Task 2: WorkplaceSetupWindow — 뼈대 + 프리팹 로드

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceSetupWindow.cs`

- [ ] **Step 1: 파일 작성 (뼈대 + 로드)**

```csharp
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
        private const string MenuPath    = "Tools/Workplace Setup";
        private const float  CanvasH     = 320f;
        private const float  TileBase    = 48f;
        private const int    RangeMin    = 1;
        private const int    RangeMax    = 8;

        // ── 프리팹 상태 ────────────────────────────────────────────────
        private TycoonObject _prefab;
        private string       _prefabPath;
        private TileSetData  _tileSetData;

        // ── 편집 데이터 ────────────────────────────────────────────────
        private bool                   _isWalkable;
        private HashSet<Vector2Int>    _blocked     = new();
        private Vector3                _visualOffset;
        private Sprite                 _sprite;

        // ── 캔버스 상태 ────────────────────────────────────────────────
        private Vector2 _panOffset;
        private float   _zoom            = 1f;
        private bool    _isPanning;
        private Vector2 _panStartMouse, _panStartOffset;
        private bool       _isDragPainting;
        private bool       _dragErasing;
        private Vector2Int _lastDragCell = new(int.MinValue, int.MinValue);
        private Vector2Int _hovered      = new(int.MinValue, int.MinValue);

        // ── 뷰 범위 ────────────────────────────────────────────────────
        private int _vMinX = -3, _vMaxX = 3, _vMinY = -3, _vMaxY = 3;

        // ── 스크롤 ─────────────────────────────────────────────────────
        private Vector2 _mainScroll;

        // ── 파생값 ─────────────────────────────────────────────────────
        private float TileW => TileBase * _zoom;
        private float TileH => TileBase * 0.5f * _zoom;
        private float Scale => TileW * 2f; // 1 world unit → canvas px

        // ── 색상 ───────────────────────────────────────────────────────
        private static readonly Color ColEmpty        = new(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Color ColBorderEmpty  = new(0.35f, 0.38f, 0.44f, 1f);
        private static readonly Color ColOrigin       = new(0.98f, 0.78f, 0.46f, 0.95f);
        private static readonly Color ColOriginBorder = new(0.93f, 0.62f, 0.09f, 1f);
        private static readonly Color ColBlocked      = new(0.80f, 0.22f, 0.22f, 0.90f);
        private static readonly Color ColBlockedB     = new(1.00f, 0.40f, 0.40f, 1f);
        private static readonly Color ColHover        = new(1.00f, 0.45f, 0.45f, 0.35f);
        private static readonly Vector3[] _verts      = new Vector3[4];

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
        }

        // ── 프리팹 로드 ────────────────────────────────────────────────
        private void LoadFromPrefab()
        {
            _prefabPath  = AssetDatabase.GetAssetPath(_prefab);
            _tileSetData = _prefab.TileSetData;
            _isWalkable  = _tileSetData?.IsWalkable ?? true;

            _blocked.Clear();
            if (_tileSetData?.BlockedOffsets != null)
                foreach (var v in _tileSetData.BlockedOffsets)
                    _blocked.Add(v);

            var visualRoot = _prefab.GetComponentInChildren<TycoonVisualRoot>();
            _visualOffset  = visualRoot != null ? visualRoot.transform.localPosition : Vector3.zero;

            // SpriteRenderer는 TycoonVisualRoot 자식에서 우선 탐색
            var sr = visualRoot?.GetComponentInChildren<SpriteRenderer>()
                  ?? _prefab.GetComponentInChildren<SpriteRenderer>();
            _sprite = sr?.sprite;

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
                // TileSetData 수정
                var tileSet = contents.GetComponent<TycoonObject>()?.TileSetData;
                if (tileSet != null)
                {
                    tileSet.BlockedOffsets = _blocked.ToArray();
                    tileSet.IsWalkable     = _isWalkable;
                    EditorUtility.SetDirty(tileSet);
                }

                // 비주얼 오프셋 수정
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

            // 새로 저장한 프리팹을 _prefab에도 반영
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
        private void DrawCanvas() { GUILayoutUtility.GetRect(0, CanvasH); /* Task 3에서 구현 */ }
        private void DrawVisualOffsetSection() { }
        private void DrawTileSetSection() { }
    }
}
#endif
```

- [ ] **Step 2: Unity에서 컴파일 확인 및 메뉴 열기**

`Tools > Workplace Setup` 메뉴가 생기고 창이 열리는지 확인. Prefab ObjectField에 Workplace 프리팹 드래그 시 `_prefabPath`가 콘솔에 나오면 OK (아직 빈 창).

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceSetupWindow.cs"
git commit -m "feat: WorkplaceSetupWindow 뼈대 + 프리팹 로드 구현"
```

---

## Task 3: 아이소메트릭 캔버스 — 타일 그리기 + 페인팅

`DrawCanvas()` 플레이스홀더를 실제 구현으로 교체한다.

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceSetupWindow.cs`

- [ ] **Step 1: DrawCanvas + 그리드 헬퍼 구현**

`DrawCanvas()` 메서드를 아래로 교체하고, `GridToScreen`, `ScreenToGrid`, `DrawDiamond`, `PaintTile` 헬퍼를 클래스에 추가한다.

```csharp
private void DrawCanvas()
{
    // 뷰 범위 조절 Row
    EditorGUILayout.LabelField("타일 뷰 범위", EditorStyles.miniBoldLabel);
    DrawRangeRow("X", ref _vMinX, ref _vMaxX);
    DrawRangeRow("Y", ref _vMinY, ref _vMaxY);

    Rect canvasRect = GUILayoutUtility.GetRect(0, CanvasH);
    canvasRect.x     = 0;
    canvasRect.width = EditorGUIUtility.currentViewWidth - 4f;
    EditorGUI.DrawRect(canvasRect, new Color(0.13f, 0.13f, 0.16f, 1f));

    Event e       = Event.current;
    bool inCanvas = canvasRect.Contains(e.mousePosition);
    Rect local    = new Rect(0, 0, canvasRect.width, canvasRect.height);

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
            _dragErasing    = _blocked.Contains(hv);
            _isDragPainting = true;
            _lastDragCell   = hv;
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
            _lastDragCell   = new Vector2Int(int.MinValue, int.MinValue);
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

        for (int gy = _vMinY; gy <= _vMaxY; gy++)
            for (int gx = _vMinX; gx <= _vMaxX; gx++)
            {
                var coord = new Vector2Int(gx, gy);
                Vector2 ctr = GridToScreen(gx, gy, local);
                if (ctr.x < -TileW || ctr.x > local.width  + TileW ||
                    ctr.y < -TileH || ctr.y > local.height + TileH) continue;

                bool isOrig = coord == Vector2Int.zero;
                bool isBlk  = _blocked.Contains(coord);
                bool isHov  = coord == _hovered && !isOrig;

                Color fill, border;
                if (isOrig)    { fill = ColOrigin;  border = ColOriginBorder; }
                else if (isBlk){ fill = ColBlocked; border = ColBlockedB; }
                else           { fill = ColEmpty;   border = ColBorderEmpty; }

                DrawDiamond(ctr, fill, border);
                if (isHov) DrawDiamond(ctr, ColHover, Color.clear);

                if (TileW >= 28f)
                {
                    string lbl = isOrig ? "0,0" : $"{gx},{gy}";
                    GUI.Label(new Rect(ctr.x - TileW * 0.5f, ctr.y - TileH * 0.5f, TileW, TileH), lbl,
                        new GUIStyle(EditorStyles.label)
                        {
                            fontSize  = Mathf.RoundToInt(Mathf.Clamp(9f * _zoom, 7f, 12f)),
                            alignment = TextAnchor.MiddleCenter,
                            normal    = { textColor = new Color(1f, 1f, 1f, 0.65f) }
                        });
                }
            }

        // 스프라이트 미리보기 (Task 4에서 추가)
        if (_sprite != null)
            DrawSpriteAtOffset(local);

        GUI.Label(new Rect(8, 6, 300, 18),
            "좌클릭: 점유 토글  |  우클릭 드래그: 패닝  |  스크롤: 줌",
            new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 1f, 1f, 0.35f) } });

        GUI.EndClip();
    }
}

// ── 그리드 수학 ───────────────────────────────────────────────────────

private Vector2 GridToScreen(int gx, int gy, Rect canvas)
{
    float midGx  = (_vMinX + _vMaxX) * 0.5f;
    float midGy  = (_vMinY + _vMaxY) * 0.5f;
    float pivotX = canvas.x + canvas.width  * 0.5f - (midGx - midGy) * (TileW * 0.5f) + _panOffset.x;
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
    _verts[0] = new Vector3(c.x,      c.y - hh);
    _verts[1] = new Vector3(c.x + hw, c.y      );
    _verts[2] = new Vector3(c.x,      c.y + hh);
    _verts[3] = new Vector3(c.x - hw, c.y      );
    Handles.DrawSolidRectangleWithOutline(_verts, fill, border);
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
```

> `DrawSpriteAtOffset` 메서드는 Task 4에서 추가한다. 지금은 `if (_sprite != null) DrawSpriteAtOffset(local);` 줄이 컴파일 안 되므로 주석 처리한다.

- [ ] **Step 2: Unity에서 확인**

Workplace 프리팹 로드 후 캔버스에 아이소메트릭 그리드가 보이는지, 좌클릭으로 blocked 타일이 빨갛게 토글되는지, 패닝/줌이 동작하는지 확인.

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceSetupWindow.cs"
git commit -m "feat: WorkplaceSetupWindow — 아이소메트릭 캔버스 타일 편집"
```

---

## Task 4: 스프라이트 미리보기 + Visual Offset 섹션

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceSetupWindow.cs`

- [ ] **Step 1: DrawSpriteAtOffset 구현 (Repaint 블록 안에서 호출)**

클래스에 아래 메서드를 추가한다:

```csharp
/// <summary>
/// 스프라이트를 아이소메트릭 캔버스에 그린다.
/// 원점 타일(0,0)의 마름모 아래 꼭짓점을 스프라이트 피벗으로 삼고,
/// _visualOffset (world units)을 Scale 배 픽셀로 변환해 이동한다.
/// </summary>
private void DrawSpriteAtOffset(Rect local)
{
    // 원점(0,0) 마름모 아래 꼭짓점
    Vector2 origin = GridToScreen(0, 0, local);
    Vector2 pivot  = new Vector2(origin.x, origin.y + TileH * 0.5f);

    // world offset → canvas px
    // X축: 수평, Y축: 수직 (GUI 좌표계 Y는 아래가 +)
    pivot.x += _visualOffset.x * Scale;
    pivot.y -= _visualOffset.y * Scale;

    Bounds b  = _sprite.bounds;
    float  sw = b.size.x * Scale;
    float  sh = b.size.y * Scale;
    Rect drawRect = new Rect(
        pivot.x + b.min.x * Scale,
        pivot.y - b.max.y * Scale,
        sw, sh);

    Texture2D tex = _sprite.texture;
    Rect uv = new Rect(
        _sprite.textureRect.x      / tex.width,
        _sprite.textureRect.y      / tex.height,
        _sprite.textureRect.width  / tex.width,
        _sprite.textureRect.height / tex.height);

    GUI.DrawTextureWithTexCoords(drawRect, tex, uv, alphaBlend: true);
}
```

그리고 Task 3에서 주석 처리했던 `DrawSpriteAtOffset(local);` 호출을 주석 해제한다.

- [ ] **Step 2: DrawVisualOffsetSection 구현**

`DrawVisualOffsetSection()` 플레이스홀더를 아래로 교체한다:

```csharp
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

    if (_prefab.GetComponentInChildren<TycoonVisualRoot>() == null)
        EditorGUILayout.HelpBox(
            "이 프리팹에 TycoonVisualRoot 컴포넌트가 없습니다.\n" +
            "비주얼 자식 GO에 컴포넌트를 부착하세요.",
            MessageType.Warning);
}
```

- [ ] **Step 3: Unity에서 확인**

- 프리팹에 TycoonVisualRoot가 있고 SpriteRenderer가 있으면 스프라이트가 캔버스에 그려짐
- Visual Offset X/Y를 바꾸면 캔버스 스프라이트가 이동함
- TycoonVisualRoot 없으면 HelpBox 경고 표시됨

- [ ] **Step 4: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceSetupWindow.cs"
git commit -m "feat: WorkplaceSetupWindow — 스프라이트 미리보기 + visual offset 편집"
```

---

## Task 5: TileSet 섹션 + 저장 완성

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceSetupWindow.cs`

- [ ] **Step 1: DrawTileSetSection 구현**

`DrawTileSetSection()` 플레이스홀더를 아래로 교체한다:

```csharp
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
    GUI.color = new Color(1f, 0.6f, 0.6f, 1f);
    if (GUILayout.Button("점유 초기화", GUILayout.Width(80)))
        if (EditorUtility.DisplayDialog("초기화", "모든 점유 타일을 삭제하시겠습니까?", "삭제", "취소"))
        { _blocked.Clear(); Repaint(); }
    GUI.color = Color.white;
    EditorGUILayout.EndHorizontal();
}
```

- [ ] **Step 2: 전체 동작 확인 (저장 → 재로드)**

1. Workplace 프리팹을 창에 로드
2. 타일 몇 개 페인팅, Visual Offset 수정
3. "저장" 클릭
4. "불러오기" 클릭하여 재로드
5. 편집 내용이 그대로 반영되어 있는지 확인 (BlockedOffsets 배열, localPosition)

- [ ] **Step 3: 다른 이름으로 저장 확인**

1. "다른 이름으로 저장..." 클릭
2. 새 경로에 .prefab 파일이 생성되는지 확인
3. 새 프리팹 로드 시 동일한 데이터가 있는지 확인

- [ ] **Step 4: 최종 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceSetupWindow.cs"
git commit -m "feat: WorkplaceSetupWindow — TileSet 섹션 + 저장 완성"
```

---

## Spec 커버리지 체크

| 요구사항 | 태스크 |
|---------|--------|
| Tools 메뉴에서 창 열기 | Task 2 |
| 프리팹 로드 (불러오기) | Task 2 |
| TileSet(BlockedOffsets) 편집 | Task 3 |
| 비주얼 자식 GO 마커 컴포넌트 | Task 1 |
| Visual Offset 편집 | Task 4 |
| 스프라이트 미리보기 | Task 4 |
| 프리팹 저장 | Task 5 |
| 다른 이름으로 저장 | Task 5 |
| TileSetData 없을 때 경고 | Task 5 |
| TycoonVisualRoot 없을 때 경고 | Task 4 |
