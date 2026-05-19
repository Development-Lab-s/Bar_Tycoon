# StageLayoutSO Palette + Click Placement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an ObjectData palette and click-to-place workflow to `StageLayoutEditor` so designers can select an obstacle from the palette and click an empty grid cell to place it.

**Architecture:** All changes are in one file (`StageLayoutEditor.cs`). Tasks 2 and 3 touch non-overlapping methods and are dispatched as parallel code-generation agents whose outputs are applied in Task 4. Task 1 (state fields) must complete before Tasks 2/3 begin.

**Tech Stack:** Unity IMGUI, `AssetDatabase.FindAssets`, `GUI.BeginScrollView`, existing `GetPreview` / `Undo` / `EditorUtility.SetDirty` patterns.

---

## File Map

| Action | Path |
|---|---|
| **Modify** | `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/Editor/StageLayoutEditor.cs` |

---

## Task 1: Add Palette State Fields

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/Editor/StageLayoutEditor.cs`

- [ ] **Step 1: Read the file and locate the state section**

  The section begins at `// ── 에디터 전용 상태` (around line 27). Find the line:
  ```csharp
  private Vector2 _panStartOffset;
  ```

- [ ] **Step 2: Add three fields immediately after `_panStartOffset`**

  Insert a blank line then:
  ```csharp
  private List<ObjectData>  _paletteItems         = new List<ObjectData>();
  private int               _selectedPaletteIndex = -1;
  private Vector2           _paletteScroll        = Vector2.zero;
  ```

- [ ] **Step 3: Verify**

  Read the file back. Confirm the three fields appear in the state block, before the view-range fields section (`// ── 뷰 범위`).

---

## Task 2 (Parallel A): Generate `DrawPalette` Code

> **This task produces code as text output only — do NOT write to the file.**
> The controller will apply the code in Task 4.

**Goal:** Produce the complete, ready-to-insert `DrawPalette` method.

- [ ] **Step 1: Output the following method verbatim**

```csharp
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
            string[] guids = AssetDatabase.FindAssets("t:ObjectData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<ObjectData>(path);
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

    Event e = Event.current;

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
            EditorGUI.DrawRect(new Rect(thumbRect.x,               thumbRect.y,                thumbRect.width, bw), ColSelectedBorder);
            EditorGUI.DrawRect(new Rect(thumbRect.x,               thumbRect.yMax - bw,        thumbRect.width, bw), ColSelectedBorder);
            EditorGUI.DrawRect(new Rect(thumbRect.x,               thumbRect.y,                bw, thumbRect.height), ColSelectedBorder);
            EditorGUI.DrawRect(new Rect(thumbRect.xMax - bw,       thumbRect.y,                bw, thumbRect.height), ColSelectedBorder);
        }

        GUI.Label(labelRect, _paletteItems[i].name,
            new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });

        if (e.type == EventType.MouseDown && e.button == 0 && hitRect.Contains(e.mousePosition))
        {
            _selectedPaletteIndex = (i == _selectedPaletteIndex) ? -1 : i;
            e.Use();
            Repaint();
        }
    }

    GUI.EndScrollView();
}
```

---

## Task 3 (Parallel B): Generate Grid Click Update Code

> **This task produces two code snippets as text output only — do NOT write to the file.**
> The controller will apply the code in Task 4.

**Goal:** Produce the updated left-click block and the updated hint label string.

- [ ] **Step 1: Output the replacement for the left-click block**

  This replaces the entire `// 좌클릭 선택` block (lines ~179–190 in the original):

```csharp
            // 좌클릭 선택 / 배치
            if (inCanvas && e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                if (ScreenToGrid(localMouse, localCanvas, out Vector2Int cell))
                {
                    int idx = layout.entries.FindIndex(en => en.cellIndex == cell);
                    if (_selectedPaletteIndex >= 0 && idx < 0)
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
```

- [ ] **Step 2: Output the replacement for the hint label**

  This replaces the `GUILayout.Label(...)` at the bottom of `DrawGridCanvas` (the line containing `"좌클릭: 선택"`):

```csharp
            GUILayout.Label("우클릭 드래그: 패닝   |   스크롤: 줌   |   좌클릭: 선택/배치",
                new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 1f, 1f, 0.3f) } });
```

---

## Task 4: Apply All Changes to File

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/Editor/StageLayoutEditor.cs`

This task applies the outputs from Tasks 2 and 3, then updates `OnInspectorGUI`.

- [ ] **Step 1: Read the current file**

- [ ] **Step 2: Insert `DrawPalette` method after `DrawToolbar`**

  Find the closing brace of `DrawToolbar` (the `}` on the line before `public override void OnInspectorGUI()`).
  Insert the full `DrawPalette` method (from Task 2 output) between `DrawToolbar`'s closing `}` and `public override void OnInspectorGUI()`.

- [ ] **Step 3: Replace the left-click block in `DrawGridCanvas`**

  Find and replace:
  ```csharp
              // 좌클릭 선택
              if (inCanvas && e.type == EventType.MouseDown && e.button == 0)
              {
                  Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                  if (ScreenToGrid(localMouse, localCanvas, out Vector2Int cell))
                  {
                      int idx = layout.entries.FindIndex(en => en.cellIndex == cell);
                      _selectedIndex = idx;
                  }
                  else _selectedIndex = -1;
                  e.Use(); Repaint();
              }
  ```
  With the updated block from Task 3 Step 1.

- [ ] **Step 4: Replace the hint label in `DrawGridCanvas`**

  Find and replace:
  ```csharp
              GUILayout.Label("우클릭 드래그: 패닝   |   스크롤: 줌   |   좌클릭: 선택",
  ```
  With:
  ```csharp
              GUILayout.Label("우클릭 드래그: 패닝   |   스크롤: 줌   |   좌클릭: 선택/배치",
  ```

- [ ] **Step 5: Replace `OnInspectorGUI`**

  Find and replace the entire `OnInspectorGUI` method with:

  ```csharp
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
                  if (en.obstacleData != null && en.obstacleData.Prefab != null
                      && GetPreview(en.obstacleData) == null)
                      needsRepaint = true;
              }
              foreach (var item in _paletteItems)
              {
                  if (item != null && item.Prefab != null && GetPreview(item) == null)
                      needsRepaint = true;
              }
              if (needsRepaint) Repaint();
          }
  ```

- [ ] **Step 6: Read the file back and verify**

  Confirm:
  - `DrawPalette` method exists between `DrawToolbar` and `OnInspectorGUI`
  - Left-click block has `_selectedPaletteIndex >= 0 && idx < 0` condition
  - Hint label reads `"선택/배치"`
  - `OnInspectorGUI` calls `DrawPalette(layout)` after `DrawGridCanvas`
  - `OnInspectorGUI` has two foreach loops (entries + paletteItems)
