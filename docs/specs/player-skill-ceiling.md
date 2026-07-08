# Specification: Player Skill Ceiling (Hidden Potential & Peak Age)

## Overview

Today a player's skill can climb indefinitely (up to the global 9.9 clamp) as long as
the team keeps winning — every player on a successful side eventually converges on 9.9.
This spec introduces a **hidden per-player ceiling**: each player is created with a
`PotentialSkill` (the best they can ever become) and a `PeakAge` (26–30, varying per
player). Skill gains from wins, goals, and extra training are silently capped at
`PotentialSkill`. The ceiling **never drops once assigned**; ages 26–30 are all peak
years, and only past age 30 is the end-of-season skill drift biased downward so older
players decline. Both properties are **hidden** — never shown in the squad screen,
scout reports, or transfer negotiations.

## BASIC reference

There is no equivalent mechanism in FOOT.BAS — this is a **new mechanic**, not a port.
In the original, `H(I)` grows without a per-player bound (lines 3306–3308 add per-match
win bonuses forever) and senior players never age (`G(I)` is set once at line 1076 and
only changes sign for transfer listing).

The design does have an in-game precedent: youth players already carry a hidden
potential, `Y(3,I)` → `YouthPlayer.PotentialSkillPercent` (35–99), and stop improving
when they reach it (`HasReachedPotential`). This spec extends the same idea to the
senior squad, and reuses the youth potential when a youth player is promoted.

**Deviation note:** Step 3 adds end-of-season aging for senior players, which FOOT.BAS
does not do. Without it a peak *age* is meaningless (players would stay 24 forever).
Aging also feeds the existing age-33+ retirement mechanic
(see [player-retirement.md](player-retirement.md)) — long careers now naturally end.

---

## Step 1 — Model changes

### `TheManager.Models/Player.cs`

Add two hidden properties in the *Skill / Age* section. **`PotentialSkill` must be
declared before `Skill`** — `System.Text.Json` serialises in declaration order and
assigns in document order on load, so the ceiling must be populated before `Skill` is
assigned when deserialising a save (otherwise the loaded skill would be clamped against
the not-yet-loaded ceiling).

```csharp
// ── Skill / Age ───────────────────────────────────────────────────────────

private double _potentialSkill = 9.9;

/// <summary>
/// Hidden ceiling on Skill — the best this player can ever become (1.1–9.9).
/// Never displayed in the UI. No FOOT.BAS equivalent (new mechanic); the youth
/// analogue is Y(3,I) / YouthPlayer.PotentialSkillPercent.
/// Defaults to 9.9 (uncapped) so pre-existing save files load unchanged.
/// NOTE: declared before Skill so JSON deserialisation assigns it first.
/// </summary>
public double PotentialSkill
{
    get => _potentialSkill;
    set => _potentialSkill = Math.Clamp(value, 1.1, 9.9);
}

/// <summary>
/// Hidden age (26–30) at which the player is expected to reach their peak;
/// sizes the headroom rolled at creation. Ages 26–30 are all peak years —
/// decline only begins past 30. Never displayed. No FOOT.BAS equivalent.
/// 0 = not yet assigned (legacy save) — treated as "assign on load".
/// </summary>
public int PeakAge { get; set; }
```

Change the existing `Skill` setter's upper bound from the fixed 9.9 to the ceiling:

```csharp
public double Skill
{
    get => _skill;
    set => _skill = Math.Clamp(value, 1.1, _potentialSkill);
}
```

Because every skill mutation in the codebase goes through this setter
(`ApplyPostMatchSkillChanges`, the scorer's `SkillBoostPerGoal` in
`MatchEngineService`, `ExtraTrainingService`, `ApplyEndOfSeasonSkillUpdate`,
`RandomEventService` penalties), **no growth site needs editing** — the cap is enforced
centrally. Downward changes are unaffected: the ceiling only bounds the top.

---

## Step 2 — Assigning potential at creation

### `TheManager.Services/PlayerService.cs` — new helper

One shared helper so every creation path produces consistent potentials:

```csharp
/// <summary>
/// Assigns the hidden PeakAge (26–30) and PotentialSkill for a newly created
/// player. PotentialSkill is always strictly greater than the skill the
/// player was created with: a guaranteed base headroom of 0.3–0.5, plus
/// 0.2–0.5 per year remaining until peak. New mechanic — no BASIC equivalent.
/// </summary>
public static void AssignPotential(Player player, Random rng)
{
    player.PeakAge = 26 + rng.Next(5);                       // 26–30 inclusive

    int yearsToPeak        = Math.Max(0, player.PeakAge - player.DisplayAge);
    double baseHeadroom    = 0.3 + rng.Next(3) / 10.0;       // 0.3–0.5, always
    double headroomPerYear = 0.2 + rng.Next(4) / 10.0;       // 0.2–0.5
    player.PotentialSkill  = player.Skill + baseHeadroom
                           + yearsToPeak * headroomPerYear;
    // PotentialSkill's own setter clamps to 9.9
}
```

The base headroom guarantees `PotentialSkill > Skill` for every new player, whatever
their age — even a 33-year-old signing has a little left in the tank. (The 9.9 clamp
is unreachable at creation: the highest initial skill `GeneratePlayer` can roll is
7.9 in Division 1, so the guarantee always holds in practice.)

Call it from every place a senior `Player` is created, **after** `Skill` and `Age` are
set:

- `InitializationService.GeneratePlayer` — after line 92 (`player.Name = …`), before
  `RecalculateStatus`.
- `ScoutReportService` — the `new Player { … }` for discovered players (scouted
  players fill transfer-target slots 21–23 and may join the squad).
- `StaffService.PromoteYouthPlayer` — **do not** use the age formula here; the youth
  already has a rolled potential. Convert it on the same scale as the skill display
  (percent ÷ 10) and keep the peak-age roll:

  ```csharp
  player.PeakAge        = 26 + rng.Next(5);
  player.PotentialSkill = Math.Max(skill + 0.3 + rng.Next(3) / 10.0,   // guaranteed headroom
                                   youth.PotentialSkillPercent / 10.0);
  ```

  The first term keeps the "always greater than creation skill" guarantee even when
  the youth's rolled potential converts to less than their promoted skill. A youth
  with `PotentialSkillPercent = 97` can therefore still become a star
  (`Skill > 9.7`); one rolled at 60 tops out around 6.0.

Star players remain possible but rare: only a player whose rolled ceiling exceeds 9.7
can ever reach `IsStar`, which matches the intent that some players are simply born
better than others.

---

## Step 3 — Aging, peak, and decline

### `TheManager.Services/PlayerService.cs` — `ApplyEndOfSeasonSkillUpdate`

Extend the existing end-of-season loop (slots 1–20). For each surviving player, in
order:

1. **Age one year** (new behaviour — see deviation note above). `Age` is sign-encoded
   (negative = transfer-listed), so increment away from zero:

   ```csharp
   player.Age += Math.Sign(player.Age);
   ```

2. **Bias the drift downward past the peak window.** The ceiling never drops once
   assigned, and ages 26–30 are all peak years — a late developer can still reach
   their full potential anywhere in that window. Decline only sets in past age 30:
   the existing random drift (−0.7 to +0.5) gains a penalty of 0.05 per year beyond
   30 (`PeakWindowEndAge` constant):

   ```csharp
   double randomDrift = ((rng.Next(25) / 10.0) - 1.4) / 2;
   int yearsPastPeak  = Math.Max(0, player.DisplayAge - PeakWindowEndAge);   // 30
   player.Skill += randomDrift - yearsPastPeak * 0.05;
   ```

   A 34-year-old drifts in the range −0.9 to +0.3 — decline is likely but a good
   season can still hold form, and any recovery is capped at their unchanged
   `PotentialSkill`.

`PeakAge` therefore only sizes the headroom rolled at creation (Step 2); it does not
gate growth or trigger decline — the ceiling itself and the age-30 window end do that.

---

## Step 4 — Save-file compatibility

`SaveLoadService` needs no schema change — `System.Text.Json` picks the new properties
up automatically. Old saves simply lack both keys, which yields `PotentialSkill = 9.9`
(the field initialiser) and `PeakAge = 0`.

After deserialising, run a one-time migration over squad slots 1–28 (and any other
persisted player collections):

```csharp
foreach (var player in gameState.Squad)
{
    if (player is { PeakAge: 0 })
        PlayerService.AssignPotential(player, rng);
}
```

`AssignPotential` always sets `PotentialSkill` above current `Skill` (guaranteed base
headroom), so no loaded player loses ability. Note the save file is plain JSON, so a determined user *can*
read the hidden values — acceptable; "hidden" means hidden from the UI, not encrypted.

---

## Worked example

1. A new game generates a 21-year-old attacker with `Skill = 4.5`. `AssignPotential`
   rolls `PeakAge = 28`, base headroom `0.4`, and per-year headroom `0.3`:
   `PotentialSkill = 4.5 + 0.4 + 7 × 0.3 = 7.0`.
2. The team wins consistently. Post-match boosts (+0.05/win) and goal boosts (+0.04)
   push his skill up each season — but every assignment is clamped at 7.0. A teammate
   created with `PotentialSkill = 9.8` keeps climbing and eventually shows as a star.
3. Each season end he ages one year. Throughout ages 26–30 he is in his peak years —
   if he hasn't reached 7.0 by his `PeakAge` of 28, he can still get there at 29 or
   30.
4. The ceiling never moves: whether he plateaus at 7.0 or falls short, `PotentialSkill`
   stays 7.0 for his whole career.
5. At 33 (three years past the age-30 window end) his end-of-season drift is
   `(−0.7…+0.5) − 0.15` — decline sets in, and the existing retirement mechanic
   starts rolling against him.

---

## What does NOT need implementing

- **UI changes** — `PotentialSkill` and `PeakAge` are hidden by design; `SquadScreen`,
  scout reports, and transfer screens are untouched. (A future scout feature could
  hint at potential — "one for the future" — but that is out of scope.)
- **Edits to individual growth sites** — `ApplyPostMatchSkillChanges`,
  `MatchEngineService.RecordOurGoal`, and `ExtraTrainingService` are all enforced
  automatically by the `Skill` setter clamp.
- **Transfer valuation changes** — fees continue to be driven by current skill and
  age; pricing in hidden potential is a possible follow-up, not part of this spec.
- **Weekly/birthday aging** — aging is end-of-season only, matching the game's
  season-granular time model.
- **Opponent squads** — opponents are represented by aggregate `OpponentRatings`, not
  individual players, so nothing to cap there.

---

## Affected files

| File | Change |
|---|---|
| `TheManager.Models/Player.cs` | Add `PotentialSkill` (default 9.9, declared before `Skill`) and `PeakAge`; `Skill` setter clamps upper bound to `PotentialSkill` |
| `TheManager.Services/PlayerService.cs` | New `AssignPotential(player, rng)` helper; `ApplyEndOfSeasonSkillUpdate` gains aging and a drift penalty of 0.05 per year past age 30 |
| `TheManager.Services/InitializationService.cs` | `GeneratePlayer` calls `AssignPotential` |
| `TheManager.Services/ScoutReportService.cs` | Discovered players get `AssignPotential` |
| `TheManager.Services/StaffService.cs` | `PromoteYouthPlayer` maps `PotentialSkillPercent / 10.0` to `PotentialSkill` and rolls `PeakAge` |
| `TheManager.Services/SaveLoadService.cs` | Post-load migration: assign potential to legacy players (`PeakAge == 0`) |
| `TheManager.Tests/` | New tests: potential ranges at generation, setter clamp, ceiling immutability, past-30 decline bias, legacy-save migration |
