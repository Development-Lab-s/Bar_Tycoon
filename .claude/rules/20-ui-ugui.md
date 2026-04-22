# UI and UGUI Rules

## Layout
- Prefer responsive UGUI layout over hardcoded positions when possible.
- Favor VerticalLayoutGroup, HorizontalLayoutGroup, GridLayoutGroup, ContentSizeFitter, LayoutElement, and CanvasGroup where they improve maintainability.
- Design with portrait mobile readability in mind unless the task clearly targets another layout.

## Input
- Check raycast flow when diagnosing UI bugs.
- Distinguish between display-only graphics and clickable controls.
- Prefer explicit input priority for overlays, logs, popups, and story progression.

## UX
- Keep panels readable and hierarchy-driven.
- Modal overlays must block underlying interactions cleanly.
- State transitions should not accidentally advance gameplay or story.