# CheolYee/AGENTS.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

## Core Rules

- Save this file and related handoff docs as UTF-8.
- Work only inside `Assets/00. Work/CheolYee` unless the user explicitly expands scope.
- `StoryLineSO.modules` is the source of truth.
- Story modules must remain current-line sub-assets.
- Preserve Add Module -> line sub-asset creation.
- Do not reintroduce loose asset authoring or old inline authoring paths.
- Preview-authored data should be shaped so Runtime can read the same data.

## Current Story Authoring State

- `StoryPreviewWindow` is the active preview/authoring surface.
- Runtime Preview is read-only.
- Stage Authoring is the edit mode.
- Actor identity is `StoryActorStateData.actorInstanceKey`.
- Same `CharacterDefinitionSO` can exist multiple times as separate actor instances.
- Stage Authoring currently supports:
  - actor add/remove
  - background set/clear
  - Import Previous Stage
  - Prev/Next Line
  - Preview Line Motion
  - actor drag / scale handles

## Current Data Model

- `StoryStageLayoutModuleSO`
  - `Background`
  - `BackgroundTrack`
  - `Actors`
  - `ActorTracks`
- `StoryActorStateData`
  - authoritative absolute snapshot for a line
- `StoryBackgroundStateData`
  - authoritative absolute background snapshot for a line
- `StoryActorTrackData`
  - optional intra-line actor timeline keyed by `actorInstanceKey`
- `StoryBackgroundTrackData`
  - optional intra-line background timeline for the current line
- `StoryActorKeyframeData`
  - shared key payload currently used by actor/background timeline rows

`StoryActorTrackData` and `StoryBackgroundTrackData` extend the snapshot model. They must not replace the line snapshot objects.

## Timeline State

- Actor rows:
  - `Position`
  - `Scale`
  - `Expression`
- Background rows:
  - `Cut`
  - `Position`
  - `Scale`
- `Position` / `Scale` use segment easing.
- `Expression` / `BackgroundCut` are discrete channels.
- Motion presets are still actor `Position` / `Scale` only.
- Preview/runtime sampling is shared through `StoryTransitionSampler`.

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

- Background timeline is first pass only and needs smoke testing.
- Actor expression channel is first pass only and needs smoke testing.
- Background preset / expression preset are not done.
- Full curve editor / snapping / polished scrubber / multi-actor timeline are not done.
- Camera timeline, parallax, and branch-aware accumulation are not done.
- Runtime prefab / scene wiring still needs Play Mode verification.

## Verification Note

- Latest direct Unity Roslyn compile passed for `Assembly-CSharp` and `Assembly-CSharp-Editor`.
- Latest `Editor.log` tail showed no new compile error after the last fix.
- Avoid manual writes into `Library/Bee/artifacts` while Unity is open.
