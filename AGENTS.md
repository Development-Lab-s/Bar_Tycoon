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

## Unconfirmed Requirement Protocol

If a requirement, behavior rule, data model, file ownership boundary, UI hierarchy, naming convention, migration direction, or runtime/editor responsibility is not clearly defined, do not guess and do not implement the uncertain part.

Instead:

1. Stop before modifying code related to the uncertain area.
2. Summarize the ambiguity briefly.
3. Ask the user focused questions with 2-3 concrete options when possible.
4. State the recommended option and why.
5. Continue implementation only after the user confirms the direction.

Assumptions must be explicitly labeled as assumptions. Do not silently turn assumptions into code, serialized data, prefab hierarchy changes, or migration logic.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

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