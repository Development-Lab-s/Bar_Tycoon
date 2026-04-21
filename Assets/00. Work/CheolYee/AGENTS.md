# CheolYee/AGENTS.md

## Encoding
- Save this file and related handoff docs as UTF-8.
- PowerShell reads should use `Get-Content -Encoding UTF8`.
- Avoid direct shell text rewrites without an explicit UTF-8 encoding.
- If Korean text is touched, verify it did not become mojibake before finishing.

## Scope
- Only modify files under `CheolYee/` unless the user explicitly requires cross-folder changes.
- If a change must cross folder boundaries, explain why before doing it.

## Unity coding expectations
- Favor SOLID-friendly structure; composition over inheritance.
- Keep MonoBehaviours thin. Put reusable logic into services, directors, or plain C# classes.
- Guard null-sensitive Unity references early.
- Do not silently change public APIs or serialized fields without noting the impact.

## Safety
- Prefer content-safe changes that do not break inspector references or authoring flow.
- If a change affects initialization order, call that out explicitly.

## UI
- Prefer responsive UGUI layout over hardcoded positions.
- Modal overlays must block underlying interactions cleanly.
- State transitions must not accidentally advance gameplay or story.

## Verification
- For gameplay/UI fixes: explain how to reproduce and verify in-editor.
- For data-driven systems: state which assets or prefabs need to be created or re-linked.

## Story system handoff
Before making any story editor, preview, stage layout, or runtime glue changes:
1. Read `Assets/00. Work/CheolYee/AGENTS.override.md` (current focus, architecture, known gaps).
2. Read root `CLAUDE.local.md` (full phase history, file table, architecture constraints).

Rules that must always hold:
- `StoryLineSO.modules` is the source of truth. Stage layout data must remain sub-assets.
- Preserve graph editor behavior: pan, zoom, grid, drag, connect, multi-select, delete, undo/redo.
- Preserve `StoryRunner` runtime flow and choice handling.
- Do not reintroduce loose asset authoring or old inline authoring paths.
- `StoryPreviewWindow` is now split into 7 partial class files — see `AGENTS.override.md` for the file map.

## Current progress checkpoint (2026-04-21)
- `StoryPreviewWindow` has Runtime Preview and Stage Authoring modes.
- GameView-ratio camera frame, stage world separation, authoring grid, focus badge, pan/zoom, fit-to-view, and inspector splitter are in place.
- Choice buttons are visible again in Runtime Preview and intentionally hidden with runtime UI in Stage Authoring.
- Stage Authoring can Add Actor, Remove Selected Actor, Set Background, and Clear Background into the current line's `StoryStageLayoutModuleSO` sub-asset.
- Actor identity is now moving to instance keys: `StoryActorStateData.actorInstanceKey` is preferred by preview/runtime, with old character keys as fallback.
- The same `CharacterDefinitionSO` can be added multiple times as separate actor entries.
- Inspector selection and stage selection share the same model for actors/background.
- Actor inspector edits position, scale, visible, focused, sort order, character reference, and enter motion basics.
- Background inspector edits definition, key, visible, offset, scale, and sort order basics.
- Delete/Backspace in Stage Authoring removes the selected actor or clears the selected background, except while editing text/numeric/vector fields.
- Latest Unity compile check: 0 script errors; one unrelated MCP WebSocket warning may appear.

## Latest late-session checkpoint (2026-04-21)
- Runtime Preview was made read-only and camera-frame-fit oriented.
- Dialogue display mode exists: `RenderOnly`, `EditorOnly`, `Both`, `None`.
- Stage Authoring has actor move/scale handles with Shift axis lock, Alt center scale, and Ctrl opposite-corner anchored scale.
- Stage Authoring has `Import Previous Stage`, which should bake previous accumulated stage state into the current line when the current line has no `StoryStageLayoutModuleSO`.
- Preview now tries to recover the current `StoryEpisodeSO` from the selected `StoryLineSO` asset if the episode field becomes empty.
- Preview has `Prev Line` / `Next Line` buttons so motion authoring does not require clicking graph nodes every time.
- Motion MVP exists: actor enter/move/exit fields, expanded move easing options, background transition fields, and `Preview Line Motion`.
- Motion preview is defined as `previous accumulated state -> current line state`.
- Important tooling note: do not manually run Roslyn/csc against Unity `Library/Bee/artifacts` while Unity is open. It caused a CS2012 lock on `Assembly-CSharp.dll`. If it happens, stop only the Unity NetCoreRuntime `dotnet` compiler process, not the Unity Editor, then refresh Unity.
- Unity MCP may still be unavailable to Codex even if the local server is restarted; the observed Codex-side error was `unsupported call`.

## Still pending
- Full motion/keyframe/timeline authoring.
- Runtime execution of all new motion/background transition fields.
- Branch-aware preview accumulation after choices.
- Prefab-based preview parity and full runtime scene wiring.
- Background parallax/camera focus animation.
- In-Unity smoke testing of latest Import Previous Stage, line navigation, motion preview, and Ctrl scale behavior.
