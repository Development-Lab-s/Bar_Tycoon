# CheolYee/AGENTS.md

## Encoding

- Save this file and related handoff docs as UTF-8.
- PowerShell reads should use `Get-Content -Encoding UTF8`.
- Avoid direct shell text rewrites without explicit UTF-8 encoding.
- If Korean text is touched, verify it did not become mojibake before finishing.

## Scope

- Work only inside `Assets/00. Work/CheolYee` unless the user explicitly expands scope.
- If a change must cross folder boundaries, explain why before editing.

## Unity Coding Expectations

- Favor SOLID-friendly structure and composition over inheritance.
- Keep MonoBehaviours thin.
- Put reusable behavior in services, directors, modules, plain C# classes, or ScriptableObject data where appropriate.
- Guard Unity null-sensitive references early.
- Do not silently change public APIs or serialized fields without noting impact.

## Story System Rules

- `StoryLineSO.modules` is the source of truth.
- Story modules must remain line sub-assets.
- Preserve Add Module -> line sub-asset creation.
- Do not reintroduce old inline authoring or loose asset authoring paths.
- Preview-authored data should be structured so Runtime can eventually read the same data.

## Current Story Preview State (2026-04-22)

- `StoryPreviewWindow` is the active editor/preview surface.
- It is split into 7 partial files under:
  - `02. Scripts/Story/RuntIme/Data/Definitions/Editor/`
- Runtime Preview is read-only and camera-frame focused.
- Stage Authoring is the editing mode.
- GameView-ratio camera frame, fit-to-view, stage grid, camera guide, focus markers, pan/zoom, inspector splitter, and workspace collapse controls exist.
- Dialogue display mode exists:
  - `RenderOnly`
  - `EditorOnly`
  - `Both`
  - `None`
- Stage Authoring can:
  - Add Actor
  - Remove Selected Actor
  - Set Background
  - Clear Background
  - Import Previous Stage
  - Preview Line Motion
  - Move to Prev Line / Next Line without graph clicks
- Actor selection is keyed by `StoryActorStateData.actorInstanceKey`.
- The same `CharacterDefinitionSO` can be added multiple times as independent actor instances.
- Actor/background inspector selection and stage selection share one model.
- Actor scale handles support:
  - Shift axis lock
  - Alt center scale
  - Ctrl opposite-corner anchored scale

## Current Stage / Motion Data

- `StoryStageLayoutModuleSO`
  - `Background`
  - `Actors`
  - `ActorTracks`
- `StoryActorStateData`
  - absolute actor state for a line
  - includes actor instance key, position, scale, visibility, focus, sort, pose/expression, enter/move/exit motion
- `StoryBackgroundStateData`
  - line-level background state
  - includes transition motion/duration
- `StoryActorTrackData`
  - optional per-line actor keyframe track
  - keyed by actor instance key
- `StoryActorKeyframeData`
  - optional intra-line keyframe
  - normalized time, position, scale, visibility, easing

`StoryActorStateData` remains the authoritative line snapshot. `StoryActorTrackData` is optional intra-line motion data and must not replace the snapshot.

## Tooling Status

- Unity MCP connection is available again as of 2026-04-22.
- Latest MCP console check returned 0 errors.
- Do not manually compile into `Library/Bee/artifacts` while Unity is open.
- If `CS2012` appears for `Assembly-CSharp.dll`, stop only the Unity NetCoreRuntime `dotnet.exe` compiler process, then refresh Unity.

## Must Not Break

- Graph editor pan / zoom / grid background
- Node drag / connectable ports / connections
- Multi-select / multi-move / delete
- Undo / Redo
- Add Module -> sub-asset flow
- Preview open / graph selection sync
- Runtime Preview choice buttons
- `StoryRunner` runtime flow

## Known Gaps

- Full keyframe/timeline editor UI is not finished.
- Runtime does not yet fully execute all preview-authored actor track/background transition data.
- Branch-aware preview accumulation after choices is still incomplete.
- Prefab-based preview parity is incomplete.
- Camera animation authoring and background parallax are not implemented.
- Runtime stage scene wiring still needs verification.
