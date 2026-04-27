# CheolYee/AGENTS.md

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
