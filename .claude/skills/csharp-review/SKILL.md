---
name: csharp-review
description: Reviews C# code in the TheManager project for correctness, nullable safety, conventions, and faithfulness to the original FOOT.BAS logic.
allowed-tools: Read Grep Glob
---

# C# Code Review — TheManager

You are reviewing C# code in the TheManager project (.NET 10). The codebase is a faithful port of Football Director II (AmigaBASIC, 1988). Review the file(s) or selection provided by the user.

## What to Check

### Correctness
- Does the logic match the original FOOT.BAS behaviour? If a BASIC line number is referenced in comments, read that section of `Original Code/FOOT.BAS` and verify the C# matches it.
- Are edge cases handled — e.g. empty squad slots (`null` entries in `Player?[29]`), zero finances, injured/suspended players being excluded from selection?
- Are squad array indices used correctly? Slots are 1-based (slot 0 unused). First team: 1–11, substitute: 12, reserves: 13–20, transfer slots: 21–28.

### Nullable Safety
- Nullable reference types are enabled. Flag any dereference of a nullable without a null check.
- `Player?[]` squad arrays must be null-checked before accessing player properties.
- Prefer `?.` and `??` over explicit null checks where it reads clearly.

### Service & Architecture Conventions
- Pure logic belongs in a static method in the relevant `*Service` class — not in models.
- Randomised logic must use an injected `Random` instance, not `Random.Shared` or `new Random()`.
- Models must be POCOs — no business logic, no service calls inside model classes.
- Services must not reference any UI layer.

### Documentation
- Every public member needs an XML `<summary>` doc comment.
- If the member corresponds to a BASIC variable or subroutine, the comment must say so:
  ```csharp
  /// <summary>Player skill. Corresponds to H(I) in FOOT.BAS, lines 312–318.</summary>
  ```
- BASIC line number references must be accurate — cross-check against `FOOT.BAS` if in doubt.

### Style
- Section dividers inside model classes use this format:
  ```csharp
  // ── Section Name ──────────────────────────────────────────────────────────
  ```
- Enums must live in `TheManager.Models/Enums.cs`, not in separate files or inline.
- No magic numbers — use named constants or enum values.
- No unnecessary comments explaining *what* the code does; only comment the *why* when non-obvious.
- Use `var` for local variable declarations where the type is inferable — avoid spelling out the type explicitly.
- Prefer LINQ over manual loops for querying, filtering, and transforming collections.

## Output Format

Report findings grouped by severity:

**Bugs / Logic errors** — things that will cause incorrect behaviour  
**Nullable safety issues** — potential null reference exceptions  
**Convention violations** — deviations from project standards  
**Missing documentation** — public members without XML docs or missing BASIC references  
**Suggestions** — optional improvements (clearly marked as non-blocking)

If the code is correct and well-formed, say so briefly. Don't invent problems.
