# AGENTS.md

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

## Repository identity
- This repo is a 2D isometric subculture tycoon game.
- Prioritize mobile-friendly UX, readable game state, and maintainable content pipelines.
- Favor practical solutions that support iteration by designers and artists.

## Working agreements
- Read the relevant guidance before making large edits.
- Prefer small, reviewable changes over broad refactors.
- When a change affects architecture, explain the reason and the touched systems first.
- Before editing code, identify the target folder and stay scoped to that area unless cross-folder changes are clearly required.
- Do not silently change public APIs or serialized fields without calling out the impact.

## Project routing
- If the task is under `CheolYee/`, also apply `CheolYee/AGENTS.md`.
- For Unity/C#/UGUI/testing conventions, use the `unity-team-rules` skill.
- For active local work notes, check `CheolYee/AGENTS.override.md` if it exists.

## Done when
- The requested change is implemented in the correct folder.
- Risks to prefab, scene, or ScriptableObject references are called out.
- Verification steps in Unity are included when relevant.