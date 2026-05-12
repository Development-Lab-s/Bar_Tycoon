# IsoAutoWallTile Pivot-Safe Placement Design

**Date:** 2026-05-07
**Area:** `Assets/00. Work/CheolYee/02. Scripts/TileMaps/`
**Primary Files:**
- Existing tile logic: `Assets/00. Work/CheolYee/02. Scripts/TileMaps/IsoAutoWallTile.cs`
- Verified scene target: `Assets/00. Work/CheolYee/01. Scene/Main.unity`
- Verified tilemap prefab root: `Assets/00. Work/CheolYee/08. Prefabs/Grid.prefab`

## Goal

Fix `IsoAutoWallTile` so wall sprites authored with import pivot `(0, 0)` place correctly on all four sides of an isometric rectangular wall layout without requiring per-sprite pivot hacks.

The fix must:
- preserve the current tile painting workflow
- preserve existing tile asset references and tilemap cell data
- keep the current side-resolution behavior based on neighboring cells
- move pivot compensation from sprite import settings into code

## Problem Statement

`IsoAutoWallTile` currently resolves wall side from neighboring cells and then applies only one of two transforms:
- identity
- horizontal mirror

That is not enough for the current grid and sprite setup.

Verified runtime facts:
- The active `Grid` uses `Isometric` layout with `cellSize = (1, 0.5, 1)`.
- The wall tilemaps use `tileAnchor = (0.5, 0.5, 0)`.
- The wall sprites are authored around a `256x512` isometric wall convention with `PPU = 512`.
- Some existing wall assets were previously made to "almost" fit by changing sprite import pivot to values around `(1, 0.25)` to `(1, 0.256)`.

Root cause:
- A plain `FlipX` mirrors around the tile-local origin generated from the imported sprite pivot.
- With import pivot `(0, 0)`, the mirrored wall keeps the wrong anchor point relative to the tile cell.
- This produces acceptable placement on some edges and incorrect placement on the opposite edges.

This is a transform-definition problem, not a neighbor-detection problem.

## Selected Approach

### Option 1: Code-managed directional transform with mirror compensation

This is the selected solution.

Behavior:
- Keep the current neighbor-based side resolution.
- When the tile needs the mirrored presentation, do not use a raw `FlipX`.
- Instead, apply `FlipX` plus a translation that simulates the old "working" pivot behavior in code.

Why this is the right choice:
- It keeps sprite import pivot consistent at `(0, 0)`.
- It keeps all existing tilemap paint data valid.
- It avoids asset-by-asset pivot maintenance.
- It contains the fix inside the system that already owns wall presentation.

### Option 2: Continue solving placement through sprite import pivots

Rejected.

Reason:
- It couples art import settings to runtime placement rules.
- It is fragile as more wall assets are added.
- It makes tile behavior depend on external asset state that the tile script does not control.

### Option 3: Split wall presentation into separate directional assets

Rejected for this task.

Reason:
- It increases authoring overhead.
- It weakens the value of the current auto-wall tile behavior.
- It solves the symptom by multiplying assets instead of correcting transform logic.

## Current Verified Contract

### `IsoAutoWallTile`

The current script:
- determines whether the tile belongs to an X-connected wall or Y-connected wall
- resolves `Left` or `Right` presentation from that result
- uses color tinting to differentiate the two sides
- locks transform and color through `TileFlags.LockTransform | TileFlags.LockColor`

This contract should remain intact.

### Scene and Tilemap

The current `Main` scene and `Grid/BackWalls` tilemap were verified through Unity MCP:
- `Grid` has `cellSize = (1, 0.5, 1)`
- `BackWalls` has `tileAnchor = (0.5, 0.5, 0)`
- the room layout is already painted and should not need repainting

## Design Decisions

### 1. Keep side resolution logic, replace transform logic

Neighbor detection and side resolution are not the failing part.

The change will stay scoped to:
- how the final transform matrix is built
- how mirrored walls are compensated

This keeps the behavioral surface area small.

### 2. Introduce a virtual mirrored pivot in code

For mirrored walls, the transform should behave as if the sprite had been authored with a different pivot, while the actual imported sprite pivot remains `(0, 0)`.

Equivalent virtual pivot:
- X: right edge of the nominal wall tile
- Y: quarter height of the nominal wall tile

Expressed in nominal authoring pixels:
- `(256, 128)` for a `256x512` wall convention

This reproduces the useful part of the old workaround without requiring sprite import changes.

### 3. Use nominal authoring size, not trimmed sprite rect height

The compensation should be based on the wall authoring convention, not the exact imported sprite rect height.

Reason:
- some current files are slightly trimmed and do not all report the same sprite height
- using runtime rect height would bake trim variance into placement
- the desired placement is tied to the intended wall tile size, not to the current PNG crop

For the first implementation, the compensation should therefore use:
- width offset: `256 / PPU`
- height offset: `128 / PPU`

With `PPU = 512`, this becomes:
- `+0.5` world units on X
- `-0.25` world units on Y

### 4. Keep the fix opt-in at the tile asset level

The tile asset should own whether mirror compensation is applied.

Recommended serialized settings:
- `useMirrorPivotCompensation` default `true`
- `mirrorPivotCompensationPixels` default `(256, 128)`

Reason:
- existing `IsoAutoWallTile` assets can inherit safe defaults
- the fix stays tunable if another wall family later needs a different authoring convention
- no scene or prefab references need relinking

Impact:
- this adds serialized fields to `IsoAutoWallTile` assets
- existing tile assets remain valid and pick up default values
- tile painting data does not change

### 5. Only mirrored presentation needs the extra translation

Unmirrored presentation should stay at identity.

Mirrored presentation should use:
- horizontal scale `(-1, 1, 1)`
- translation derived from the virtual mirrored pivot compensation

This keeps the normal case simple and makes the changed behavior explicit.

## Proposed Implementation

### Structure

Keep the work inside `IsoAutoWallTile.cs`, but split responsibilities into smaller helpers:
- neighbor sampling
- side resolution
- transform building
- tint selection

Recommended helper methods:
- `ResolveWallSide(...)`
- `BuildTransform(...)`
- `GetMirrorCompensationWorld(...)`
- `GetSpritePixelsPerUnitSafe()`

This keeps the file readable without introducing unnecessary new classes.

### Transform Model

### Unmirrored wall

Use:
- identity transform

### Mirrored wall

Use a combined transform equivalent to:
- translate by mirror compensation
- then apply `FlipX`

Conceptually:

```text
MirroredTransform = Translation(compensation) * FlipX
```

Where:
- `compensation.x = mirrorPivotCompensationPixels.x / sprite.pixelsPerUnit`
- `compensation.y = -(mirrorPivotCompensationPixels.y / sprite.pixelsPerUnit)`

Default first-pass values:
- `compensation.x = +0.5`
- `compensation.y = -0.25`

This makes the mirrored wall behave like a stable code-defined pivot adjustment instead of a raw origin flip.

## Data Flow in `GetTileData`

1. Assign sprite, collider, flags, and base tile data.
2. Sample four-neighbor occupancy using the existing same-tile checks.
3. Resolve the logical wall side using existing rules.
4. Build the transform:
   - `identity` for non-mirrored
   - `translation * FlipX` for mirrored
5. Apply tint as today.

No change is needed to tile refresh behavior.

## Risk Management

### Asset risk

If a wall sprite still uses a non-zero imported pivot after this change, the result may be double-compensated.

Mitigation:
- the intended workflow after this change is `pivot = (0, 0)` for wall sprites using this tile
- verification should explicitly test with pivot-normalized assets

### Serialized-field risk

Adding serialized fields changes the `IsoAutoWallTile` asset schema.

Mitigation:
- add only the minimum fields needed for compensation
- keep defaults aligned with the current `256x512 / PPU 512` wall convention
- do not rename or remove existing serialized fields

### Scope risk

This fix only addresses placement of the wall sprite itself.

It does not redesign:
- corner resolution policy
- endpoint outline generation
- shader seam behavior
- multi-sprite composite wall systems

That is intentional. The change should stay focused on the transform root cause.

## Verification Plan

Verification should be done in Unity Editor against the current `Main` scene.

### Required checks

1. Confirm the target wall sprites used by `IsoAutoWallTile` are imported with pivot `(0, 0)`.
2. In `Grid/BackWalls`, verify that all four sides of the rectangular wall layout align without switching sprite pivots.
3. In any other tilemap using `T_IsoWall_Auto` or `BackWalls`, verify that X-connected and Y-connected chains still resolve to the intended tints and facing.
4. Confirm that no repainting of existing tile cells is required.
5. Confirm that collider behavior is unchanged.

### Regression checks

1. A straight X-axis wall chain still uses the expected mirrored or non-mirrored presentation from `xAxisWallSide`.
2. A straight Y-axis wall chain still uses the opposite presentation rule.
3. Isolated single-cell walls still use `isolatedWallSide`.
4. Corners still follow `axisPriority` exactly as before.

## Out of Scope

- changing tilemap anchor values
- repainting tilemaps
- changing grid layout or cell size
- requiring separate top-wall and bottom-wall tiles
- moving wall placement responsibility into prefabs or scene scripts

## Implementation Recommendation

Proceed with a single-file change in `IsoAutoWallTile.cs` that:
- adds mirrored-pivot compensation settings
- replaces the raw `FlipXMatrix` usage with a compensation-aware transform builder
- leaves the neighbor resolution and tinting rules intact

This is the smallest content-safe fix that addresses the actual transform root cause.
