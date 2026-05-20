# StageLayoutSO Visualization Editor — Design Spec

**Date:** 2026-05-19  
**Branch:** BBJ  
**Status:** Approved

---

## 1. Goal

Create a `CustomEditor` for `StageLayoutSO` that lets designers visually inspect and edit the placed obstacle layout directly from the Inspector, with prefab preview thumbnails on each grid cell.

---

## 2. File

| Item | Value |
|---|---|
| New file | `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/Editor/StageLayoutEditor.cs` |
| Class | `StageLayoutEditor : UnityEditor.Editor` |
| Attribute | `[CustomEditor(typeof(StageLayoutSO))]` |
| Neighbor | `ObstacleDataEditor.cs` (same folder) |

---

## 3. UI Layout

Three zones stacked vertically inside `OnInspectorGUI`.

```
┌─────────────────────────────────┐
│  Grid Canvas (~300px fixed)     │  Zone 1: Quarter-view isometric grid
│  Each cell shows AssetPreview   │
└─────────────────────────────────┘
┌─────────────────────────────────┐
│  [AssetPreview 128×128]         │  Zone 2: Detail panel (hidden when nothing selected)
│  cellIndex:    [x]  [y]         │
│  obstacleData: [ObjectField]    │
│                      [Delete]   │
└─────────────────────────────────┘
┌─────────────────────────────────┐
│  [Add Entry]    entries: 70     │  Zone 3: Toolbar
└─────────────────────────────────┘
```

---

## 4. Grid Rendering

- **Bounding box**: computed from all `entry.cellIndex` min/max, with ±1 cell padding.
- **Cell shape**: isometric diamond, same aspect ratio as `ObstacleDataEditor` (2:1 width-to-height, quarter-view standard).
- **Cell size**: fixed 48px width initially; no pan/zoom needed (bounding box auto-fits).
- **Thumbnail source**: `AssetPreview.GetAssetPreview(entry.obstacleData.Prefab)`. If the texture is not ready yet (returns null), draw a gray placeholder rect and call `Repaint()` to retry.
- **Selected cell**: draw a colored border (e.g. yellow, 2px) over the cell.
- **Hit testing**: on `EventType.MouseDown` inside the canvas `Rect`, iterate all cells and test if the click point is inside the diamond polygon.

---

## 5. Detail Panel

Shown only when `_selectedIndex >= 0`.

| Field | Control |
|---|---|
| Prefab preview | `GUI.DrawTexture` with 128×128 `AssetPreview` |
| `cellIndex` | `EditorGUILayout.Vector2IntField` |
| `obstacleData` | `EditorGUILayout.ObjectField` |
| Delete button | `GUILayout.Button("Delete")` — removes entry at `_selectedIndex`, sets `_selectedIndex = -1` |

---

## 6. Toolbar

- **Add Entry** button: appends `new PlacedObstacleEntry { cellIndex = Vector2Int.zero, obstacleData = null }`, sets `_selectedIndex` to the new last index.
- **Entry count label**: `$"entries: {layout.entries.Count}"` right-aligned.

---

## 7. Mutation Rules

All writes to `StageLayoutSO.entries` must:

1. Call `Undo.RecordObject(target, "<description>")` before the mutation.
2. Perform the mutation.
3. Call `EditorUtility.SetDirty(target)`.

This ensures undo/redo works and the asset is marked for save.

---

## 8. State

Private editor-only fields (not serialized):

| Field | Type | Purpose |
|---|---|---|
| `_selectedIndex` | `int` | Currently selected entry index, `-1` = none |
| `_previewCache` | `Dictionary<ObjectData, Texture2D>` | Cached `AssetPreview` textures |

Cache is cleared in `OnDisable` to avoid stale references.

---

## 9. Non-Goals

- No pan/zoom (auto-fit bounding box is sufficient for typical layouts).
- No drag-to-move cells in the grid (cellIndex is edited via field only).
- No integration with `ObstacleEditorWindow` (independent per approved design).
- No runtime behavior changes.
