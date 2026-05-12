# Isometric Wall Outline Shader Design

**Date:** 2026-05-06
**Area:** `Assets/00. Work/CheolYee/07. Shaders/`
**Primary Files:**
- Existing reference shader: `Assets/00. Work/CheolYee/07. Shaders/IsoWallTopCapSeamFill.shader`
- Existing debug shader: `Assets/00. Work/CheolYee/07. Shaders/WallOccupancyDebug.shader`
- Existing occupancy source: `Assets/00. Work/CheolYee/02. Scripts/TileMaps/WallOccupancyBaker.cs`

## Goal

Create a new production wall shader for isometric tilemap walls that:
- Draws a continuous top border across connected wall cells without UV-stretch artifacts.
- Draws end outlines only at the left and right ends of a wall chain.
- Expands the outline outward into transparent space rather than painting only over opaque pixels.
- Preserves the current wall cap shading style: fill, highlight, shadow, and face crease.
- Supports both outer and inner wall tilemaps through one shader with material-level configuration.

## Problem Statement

The current production shader uses sprite alpha and UV-relative searches to infer where the top cap should exist. That approach breaks when a 256x512 wall sprite already consumes pixels near the top of the texture, because there is no safe UV headroom for synthetic cap growth. In practice this causes visible disconnects or clipping between adjacent walls.

The occupancy-debug pipeline already proved a more stable solution:
- Each wall fragment can recover the sprite pivot in world space.
- The pivot can be converted into a stable tile cell coordinate through the occupancy map basis.
- Neighbor occupancy can be sampled by channel.

This design switches border and outline generation from sprite-alpha inference to occupancy-driven edge inference.

## Current Verified Runtime Contract

### Occupancy Globals

`WallOccupancyBaker` already publishes these shader globals:
- `_WallOccupancyMap`
- `_WallOccupancyOrigin`
- `_WallOccupancyBasisX`
- `_WallOccupancyBasisY`
- `_WallOccupancyMapSize`

These are sufficient for the first production pass. The baker contract will remain unchanged unless a later optimization pass proves it necessary.

### Scene Material Contract

Current debug materials establish the two wall families:
- Outer wall material uses `_TargetChannel = 0` and `_PivotUV = (0, 0)`.
- Inner wall material uses `_TargetChannel = 1` and `_PivotUV = (1, 0.253)`.

The new production shader must keep the same high-level configuration model:
- One shader.
- Separate materials per wall family.
- Material-controlled pivot and target channel.

## Design Decisions

### 1. Asset Strategy

Create a new production shader instead of directly editing `IsoWallTopCapSeamFill.shader`.

Reason:
- The old shader remains a visual reference for shading tone.
- The new shader can change the geometric generation model without carrying old UV-search assumptions.
- Debug and production responsibilities stay clear during validation.

### 2. Outer and Inner Handling

Use one production shader for both wall families.

Per-material settings will control:
- `_TargetChannel`
- `_PivotUV`
- top border thickness
- end outline thickness
- colors and shading weights

Reason:
- Outer and inner walls share the same rendering logic.
- Only occupancy channel, pivot, and tuning differ.
- This keeps the shader maintainable and avoids duplicated bug fixes.

### 3. Thickness Units

Top border and end outline thickness must follow sprite-pixel scale, not UV stretch and not world-space thickness.

Interpretation:
- Thickness is authored in sprite-pixel-like units.
- Rendering should remain visually proportional to the original wall sprite under camera zoom changes.
- The implementation may internally use texel-size-derived conversion, but the exposed tuning contract is pixel-oriented.

### 4. Connectivity Rule

Connectivity is channel-local:
- Outer walls connect only to outer walls.
- Inner walls connect only to inner walls.

This rule applies to continuous top-border generation and to the decision of whether a cell is a chain interior or a chain end.

### 5. End Outline Rule

End outlines appear only at the left and right ends of a wall chain.

They do not appear:
- on chain interior cells
- on every exposed edge
- on top-border runs that are still connected laterally

Interpretation for the first implementation:
- A cell becomes an end candidate only when the channel-local chain terminates on its left or right side.
- The visible outline shape is generated at the corresponding corner/end silhouette region, not across the entire side face.

### 6. Cross-Family Corner Priority

When outer and inner walls visually meet near a corner, outer-wall outline treatment has priority over inner-wall outline treatment.

This priority is strictly for the final visible result. Connectivity remains channel-local. Inner and outer should not merge into one logical chain.

### 7. Corner Smoothing

Use medium-strength smooth-union behavior when combining border and end-shape masks around corners.

Target visual result:
- no harsh square seam
- no overly rounded blob
- natural merge at outer/inner meeting corners and chain-end corners

### 8. Slope Profile

Assume the current wall silhouette uses a 2:1 horizontal-to-vertical diagonal rule for the relevant cap and end geometry.

The implementation must avoid hard-locking that ratio into an unchangeable constant. It should be practical to retune later if another wall family needs a different slope.

### 9. Scope Limit

The first production pass only targets the current outer-wall-focused use case. No special handling is required yet for doors, multi-cell walls, or mixed wall archetypes.

### 10. Outline Expansion

The end outline must expand outward into transparent space.

It is not a simple tint over existing opaque sprite pixels. The shader therefore needs an additive silhouette-generation step around the sprite boundary for the end mask.

### 11. Debug Strategy

Keep only lightweight debug controls in the production shader.

Full diagnostic visualization remains the job of `WallOccupancyDebug.shader`. The production shader should expose only small verification modes or toggles that help confirm:
- active channel routing
- border mask
- end-outline mask

### 12. Legacy Shading Preservation

The new production shader must preserve the visual tone of the old wall treatment:
- cap fill color
- top highlight
- bottom shadow
- face crease

The geometry-generation logic changes. The shading language should still feel like the old wall shader, not like a new unrelated material style.

## Proposed Architecture

### Component Roles

#### `WallOccupancyBaker`
- Remains responsible for building the occupancy texture and publishing shader globals.
- No functional changes are required for the first pass.

#### `WallOccupancyDebug.shader`
- Remains the validation tool for occupancy-space correctness.
- Continues to verify pivot recovery, cell lookup, and neighbor sampling.

#### New Production Shader
- Samples sprite base color and alpha.
- Reconstructs pivot world space from fragment data.
- Converts pivot world space to occupancy cell coordinates.
- Samples same-channel neighbors to classify connection state.
- Builds three logical masks:
  - top-border mask
  - end-outline mask
  - legacy face and cap shading masks
- Composites the final wall color and alpha from those masks.

## Rendering Model

### 1. Cell Resolution

For every fragment belonging to a wall sprite:
- Recover the sprite pivot world position.
- Resolve the fragment to its owning occupancy cell.
- Sample the occupancy texture using the configured target channel.

This gives the shader stable wall-cell identity without relying on the sprite's local UV boundaries to infer neighbors.

### 2. Neighbor Classification

For the owning cell, sample at least:
- left neighbor
- right neighbor
- up neighbor
- down neighbor

The first production pass primarily needs left and right chain logic and enough vertical context to keep top-border generation stable around corners.

### 3. Top-Border Generation

The top border is generated from occupancy-defined rim continuity, not from "find transparent above" UV walks.

Behavior:
- Connected same-channel cells share one visually continuous top border.
- The border tracks the isometric top silhouette profile instead of stretching vertically in texture UV space.
- Thickness is material-tunable in pixel-like units.

The top border should not break simply because one sprite's painted pixels already reach near the top of its source texture.

### 4. End-Outline Generation

End outlines are generated only for chain terminals.

Behavior:
- Left terminal gets a left-side end outline.
- Right terminal gets a right-side end outline.
- Interior cells get no end outline.
- The outline grows outward beyond the opaque sprite silhouette.
- The visible mask should stay localized to the chain-end corner region rather than becoming a full-height side border unless later art direction requires it.

### 5. Cross-Family Visual Priority

When both families could visually compete near a corner:
- The outer-wall result is considered authoritative for the exposed outline.
- Inner-wall outline contribution should recede where required to avoid doubled or noisy corner marks.

This is a compositing rule, not a connectivity rule.

### 6. Legacy Tone Composition

After border and outline masks are computed, the shader applies the existing style cues:
- cap fill as the base top-surface treatment
- top highlight near the upper cap band
- bottom shadow to preserve volume
- face crease to keep the front face readable

The design intent is "new geometry logic, same family of shading language."

## Material Parameters Required in the New Shader

### Required Functional Parameters
- `_TargetChannel`
- `_PivotUV`
- top border thickness
- end outline thickness
- diagonal and slope tuning
- corner smoothness strength

### Required Visual Parameters
- base tint
- cap fill color
- top highlight color
- bottom shadow color
- face crease color
- end outline color

### Optional Lightweight Debug Parameters
- debug mode enum
- debug blend strength

## Validation Plan

### Visual Cases That Must Pass
- Straight outer-wall run with no broken top border.
- Straight inner-wall run with no broken top border.
- Left terminal cell produces only the left end outline.
- Right terminal cell produces only the right end outline.
- Interior cells do not show end outlines.
- Outer and inner corner meeting does not create noisy double outlines.
- Zoom in and out preserves proportion relative to the sprite art.
- Legacy cap and fill shading still reads as the same wall family.

### Validation Ownership

The user will perform Unity-side visual validation and may apply art-tuning adjustments after the first implementation pass. The implementation should therefore favor clear, editable material parameters over hidden magic constants.

## Out of Scope for the First Pass

- New baker channels or occupancy texture formats
- Door-specific exceptions
- Multi-cell wall logic
- Universal support for arbitrary wall archetypes
- Replacing the dedicated debug shader

## Risks and Mitigations

### Risk: Cross-family corner ambiguity

Because connectivity is channel-local while visibility may overlap, corners where outer and inner walls meet can still produce ambiguous silhouette competition.

Mitigation:
- Treat outer family as the visible priority for exposed end outlines.
- Keep smoothing tunable instead of hard-coded.

### Risk: Overfitting to current 2:1 art

The current wall family follows a stable ratio now, but future wall sets may not.

Mitigation:
- Parameterize the slope-related terms.
- Avoid burying the ratio deep inside unrelated math.

### Risk: Over-expansion in transparent space

Outward outline growth can create halos if the silhouette mask is too generous.

Mitigation:
- Keep outline thickness localized and separately tunable from the top border.
- Preserve debug visibility for end-mask inspection.

## Rollout

1. Create the new production shader beside the current shaders.
2. Reuse the current occupancy globals and debug-validated pivot-to-cell logic.
3. Build top-border continuity first.
4. Add localized chain-end outline expansion second.
5. Reapply legacy tone and shading controls on top of the new masks.
6. Hand off to user for Unity visual verification and artistic tuning.
