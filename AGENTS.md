# AGENTS.md

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