# Team Project Memory

@.claude/shared/project-summary.md
@.claude/shared/glossary.md

## Encoding
- Save Markdown, C# source, and Unity text assets as UTF-8.
- When using PowerShell to read files, prefer `Get-Content -Encoding UTF8`.
- When a tool must write text directly, specify UTF-8 explicitly and verify Korean text did not become mojibake.
- Prefer patch-based edits for shared docs and source files so Claude/Codex do not disagree on encoding.

## How to work in this repo
- Read the relevant rule files before making large edits.
- Prefer small, reviewable changes over broad refactors.
- When a change affects architecture, explain the reason and the touched systems first.
- Before editing code, identify the target folder and stay scoped to that area unless cross-folder changes are clearly required.
- Do not duplicate or contradict rules from `.claude/rules/`. Treat this file as the short entrypoint only.

## Project priorities
- This project is a 2D isometric subculture tycoon game.
- Prioritize mobile-friendly UX, readable game state, and maintainable content pipelines.
- Favor practical solutions that support iteration by designers and artists.