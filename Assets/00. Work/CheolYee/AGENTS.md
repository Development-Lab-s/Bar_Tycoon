# CheolYee/AGENTS.md

## Scope
- Only modify files under `CheolYee/` unless the user explicitly requires cross-folder changes.
- If a change must cross folder boundaries, explain why before doing it.

## Unity coding expectations
- Favor SOLID-friendly structure.
- Prefer composition over inheritance when behavior can be modular.
- Keep MonoBehaviours thin when possible.
- Put reusable logic into dedicated services, directors, modules, or plain C# classes when appropriate.
- Guard null-sensitive Unity references early.

## Unity safety
- Be careful with prefab, ScriptableObject, and scene reference breakage.
- If a change affects initialization order, call that out explicitly.
- Prefer content-safe changes that do not silently break inspector references or authoring flow.

## UI and UX
- Prefer responsive UGUI layout over hardcoded positions when possible.
- Check raycast flow when diagnosing UI bugs.
- Modal overlays must block underlying interactions cleanly.
- State transitions should not accidentally advance gameplay or story.

## Verification
- For gameplay bug fixes, explain how to reproduce and verify in-editor.
- For UI fixes, include hierarchy and inspector checks when needed.
- For data-driven systems, mention which assets or prefabs need to be created or re-linked.