# CheolYee/AGENTS.md

## Core Rules

- Save this file and related handoff docs as UTF-8.
- Work only inside `Assets/00. Work/CheolYee` unless the user explicitly expands scope.
- `StoryLineSO.modules` is the source of truth.
- Story modules must remain current-line sub-assets.
- Preserve Add Module -> line sub-asset creation.
- Do not reintroduce loose asset authoring or old inline authoring paths.
- Preview-authored data should be shaped so Runtime can read the same data.

## Current Story Preview State

- `StoryPreviewWindow` is the active preview/authoring surface.
- Runtime Preview is read-only and camera-frame focused.
- Stage Authoring is the editing mode.
- Stage Authoring supports actor/background editing, Import Previous Stage, Prev/Next Line, Preview Line Motion, actor drag, and actor scale handles.
- Actor identity is `StoryActorStateData.actorInstanceKey`.
- Same `CharacterDefinitionSO` can exist multiple times as separate actor instances.
- Actor scale modifiers:
  - Shift: axis lock
  - Alt: center scale
  - Ctrl: opposite-corner anchor

## Current Data Model

- `StoryStageLayoutModuleSO`
  - `Background`
  - `Actors`
  - `ActorTracks`
- `StoryActorStateData`
  - authoritative absolute snapshot for a line
- `StoryActorTrackData`
  - optional intra-line actor timeline keyed by `actorInstanceKey`
- `StoryActorKeyframeData`
  - optional keyframe data
  - timeline MVP uses `Position` and `Scale` rows only
  - easing is outgoing/segment metadata, not a row

`StoryActorTrackData` extends the snapshot model. It must not replace `StoryActorStateData`.

## Keyframe Editor Progress

- Bottom timeline panel exists.
- Visible property rows are `Position` and `Scale`.
- Add Property is a single button and only adds `Position` or `Scale`.
- Segment bars between keys are visible and own easing selection.
- Selected key edits only its own property.
- Actor direct manipulation updates selected key when property matches.
- Record ON locks the selected actor and records only the changed property at playhead time.
- Delete / Copy / Paste work on selected key.
- Undo / Redo refresh timeline, inspector, and preview immediately.
- Preview/runtime sampling share `StoryTransitionSampler` for Position / Scale / segment easing.

## Must Not Break

- Graph editor pan / zoom / grid
- Node drag / connectable ports / connections
- Multi-select / multi-move / delete
- Undo / Redo
- Add Module -> sub-asset flow
- Preview open / graph selection sync
- Runtime Preview choice buttons
- `StoryRunner` runtime flow

## Known Gaps

- Background full timeline/record is not done.
- Runtime prefab parity is incomplete.
- PPU/pivot/scale visual parity needs work.
- Full curve editor / snapping / polished scrubber / multi-actor timeline are not done.
- Camera timeline, parallax, and branch-aware accumulation are not done.
- Runtime stage scene wiring needs smoke testing.

## Verification Note

- Latest Unity MCP script compile reported 0 errors.
- Latest Unity console error check returned 0 errors.
- Avoid manual `csc` writes into `Library/Bee/artifacts` while Unity is open.
