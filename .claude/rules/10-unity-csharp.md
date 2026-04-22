---
paths:
  - "Assets/**/*.cs"
---

# Unity C# Rules

## Design
- Prefer SOLID-friendly structure.
- Favor composition over inheritance when behavior can be modular.
- Prefer explicit responsibilities for runtime, data, and presentation.
- Avoid hidden global coupling unless it is already an intentional project pattern.

## Script style
- Use clear class and method names.
- Keep MonoBehaviours thin when possible.
- Put reusable logic into dedicated services, directors, modules, or plain C# classes when appropriate.
- Guard null-sensitive Unity references early.

## Safety
- Do not silently change public APIs or serialized fields without noting the impact.
- Be careful with prefab, ScriptableObject, and scene reference breakage.
- If a change affects initialization order, call that out explicitly.