---
name: unity-team-rules
description: Use this skill for Unity work in this repo, especially when editing C# gameplay code, story/UI systems, UGUI layouts, ScriptableObjects, or when the task needs Unity-specific verification steps.
---

# Purpose
Apply the shared Unity engineering, UI, and verification rules for this repository.

# When to use
- Editing Unity C# files
- Changing MonoBehaviours, services, directors, modules, or ScriptableObjects
- Working on UGUI layout or input flow
- Fixing gameplay/UI bugs that need editor verification
- Working in `CheolYee/`

# Core coding rules
- Prefer SOLID-friendly structure.
- Favor composition over inheritance when behavior can be modular.
- Prefer explicit responsibilities for runtime, data, and presentation.
- Avoid hidden global coupling unless it is already an intentional project pattern.
- Use clear class and method names.
- Keep MonoBehaviours thin when possible.
- Put reusable logic into dedicated services, directors, modules, or plain C# classes when appropriate.
- Guard null-sensitive Unity references early.

# Safety rules
- Do not silently change public APIs or serialized fields without noting the impact.
- Be careful with prefab, ScriptableObject, and scene reference breakage.
- If a change affects initialization order, call that out explicitly.
- Prefer content-safe changes.

# UGUI and UX rules
- Prefer responsive UGUI layout over hardcoded positions when possible.
- Favor layout-driven setups when they improve maintainability.
- Design with portrait mobile readability in mind unless the task clearly targets another layout.
- Check raycast flow when diagnosing UI bugs.
- Distinguish between display-only graphics and clickable controls.
- Prefer explicit input priority for overlays, logs, popups, and story progression.
- Modal overlays must block underlying interactions cleanly.
- State transitions should not accidentally advance gameplay or story.

# Verification rules
- For gameplay bug fixes, explain how to reproduce and verify the fix in-editor.
- For UI fixes, include hierarchy and inspector checks when needed.
- For data-driven systems, mention which assets or prefabs need to be created or re-linked.
- Prefer concrete "what to test in Unity" guidance over abstract statements.

# Reference docs
- Read `references/project-summary.md`
- Read `references/glossary.md`