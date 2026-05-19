# StageLayoutSO Palette + Click Placement — Design Spec

**Date:** 2026-05-19  
**Branch:** BBJ  
**Status:** Approved

---

## 1. Goal

Add an ObjectData palette and click-to-place workflow to the existing `StageLayoutEditor` CustomEditor, so designers can select an obstacle type from the palette and click an empty grid cell to add an entry — without manually editing cellIndex.

---

## 2. File

| Item | Value |
|---|---|
| Modify | `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/Editor/StageLayoutEditor.cs` |

No new files.

---

## 3. New State Fields

| Field | Type | Default | Purpose |
|---|---|---|---|
| `_paletteItems` | `List<ObjectData>` | empty list | ObjectData assets found by last Find |
| `_selectedPaletteIndex` | `int` | `-1` | Selected palette item index, -1 = none |
| `_paletteScroll` | `Vector2` | `Vector2.zero` | Scroll position of the palette area |

---

## 4. UI Layout Addition

After the existing hint label in `DrawGridCanvas`, a new `DrawPalette` method is called from `OnInspectorGUI` (between `DrawGridCanvas` and `DrawDetailPanel`):

```
┌─────────────────────────────────┐
│  Grid Canvas (existing)         │
└─────────────────────────────────┘
  우클릭 드래그: 패닝 | 스크롤: 줌 | 좌클릭: 선택/배치  ← hint updated
┌─────────────────────────────────┐
│  [Find ObjectData]  found: N    │  ← toolbar row
│  ┌──┐ ┌──┐ ┌──┐ ┌──┐           │  ← 64px thumbnail grid, horizontal scroll
│  │  │ │  │ │  │ │  │           │
│  └──┘ └──┘ └──┘ └──┘           │
└─────────────────────────────────┘
┌─────────────────────────────────┐
│  Detail Panel (existing)        │
└─────────────────────────────────┘
```

Palette height: fixed 88px (64px thumb + label + padding).

---

## 5. Find ObjectData

Button label: `"Find ObjectData"`  
On click:
1. `AssetDatabase.FindAssets("t:ObjectData")` → get GUIDs
2. `AssetDatabase.GUIDToAssetPath` + `AssetDatabase.LoadAssetAtPath<ObjectData>` for each GUID
3. Store non-null results in `_paletteItems`
4. Reset `_selectedPaletteIndex = -1`
5. Call `AssetPreview.SetPreviewTextureCacheSize(layout.entries.Count + _paletteItems.Count + 16)`

---

## 6. Palette Rendering

- Horizontal `ScrollView` (height 88px) using `_paletteScroll`
- Each item: 64×64 button with `AssetPreview` thumbnail
  - Selected: yellow border overlay (same `ColSelectedBorder`)
  - Unselected: dark background
- Below each button: asset name label (truncated, `EditorStyles.miniLabel`)
- Click selected item again → deselects (`_selectedPaletteIndex = -1`)

---

## 7. Grid Click Behavior Change

| Palette selection | Click target | Result |
|---|---|---|
| None (`-1`) | Occupied cell | Select that entry (existing behavior) |
| None (`-1`) | Empty cell | Deselect (`_selectedIndex = -1`) |
| Item selected | Empty cell | Add new entry: `cellIndex = clicked cell`, `obstacleData = _paletteItems[_selectedPaletteIndex]` |
| Item selected | Occupied cell | Select that entry (no overwrite) |

Adding an entry: same mutation rules as "Add Entry" button — `Undo.RecordObject` → `entries.Add` → `_selectedIndex = new last index` → `EditorUtility.SetDirty`.

---

## 8. Hint Label Update

Change the existing hint label text from:
> `"우클릭 드래그: 패닝   |   스크롤: 줌   |   좌클릭: 선택"`

To:
> `"우클릭 드래그: 패닝   |   스크롤: 줌   |   좌클릭: 선택/배치"`

---

## 9. OnInspectorGUI Call Order (updated)

```
ComputeBounds
DrawGridCanvas        ← hint label updated inside here
DrawPalette           ← new
DrawDetailPanel       ← guarded by _selectedIndex
DrawToolbar
foreach repaint loop  ← checks both entries AND _paletteItems for pending previews
```

---

## 10. Non-Goals

- No drag-to-move entries on the grid.
- No palette folder filtering (finds all ObjectData in project).
- No persistence of palette state between Unity sessions.
- No auto-find on Inspector open (user must click Find).
