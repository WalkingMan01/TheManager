# Specification: Player Development (Appearance-Based Skill Growth)

## Overview

Today `Skill` moves for reasons that have nothing to do with a player actually getting
better: a win nudges every starter's skill up by a flat 0.03 (and clean-sheet defenders/
GK get a further 0.03), a loss knocks 0.03 off, and the scorer of a goal gets a flat
+0.04 on top of all that. None of this is proportional to the player, the opponent, or
anything except the scoreline — a squad player who comes on for the last five minutes
of a scrappy 1-0 win is nudged exactly as much as the match-winner.

This spec **removes all of that** and replaces it with a single, per-player growth path:
each player is assigned a hidden `DevelopmentRate` — a fixed `Skill` gain applied every
time they appear for the first team — rolled once at creation from their remaining
headroom (`PotentialSkill − Skill`, see [player-skill-ceiling.md](player-skill-ceiling.md))
and a random pace multiplier. Growth is now driven entirely by playing time, not by
results, and — because it is calibrated against each player's own `PeakAge` — some
players close their gap to potential in a season or two while others take most of a
career, exactly as `PeakAge` already varies per player. Squad members who don't feature
in a given week — the unused substitute, the reserves, anyone subbed off injured — still
grow, just at half `DevelopmentRate`, rather than being frozen out entirely. A hard
per-season cap (`SeasonSkillGainCap`, 4.0) keeps all of this bounded regardless of
`DevelopmentRate` or how many matches a player is picked for, so no single season can
hand out unrealistically fast growth.

## BASIC reference

There is no equivalent in FOOT.BAS — like `PotentialSkill`/`PeakAge`, this is a new
mechanic layered on top of the port. The code being *removed* here (lines 3306–3308,
the win/loss/clean-sheet skill deltas, and the scorer's skill bump) is itself already a
deviation from the original, which applies those same deltas unconditionally with no
ceiling. This spec doesn't restore BASIC behaviour; it replaces one non-BASIC mechanic
with a different, more deliberate one.

---

## Step 1 — Model change

### `TheManager.Models/Player.cs`

Add one hidden property in the *Skill / Age* section, alongside `PotentialSkill` and
`PeakAge`:

```csharp
/// <summary>
/// Hidden Skill gain applied for every first-team appearance. Rolled once at
/// creation from the player's headroom to PotentialSkill and a random pace
/// multiplier, so two players with identical potential can still develop at
/// different speeds. Never displayed. No FOOT.BAS equivalent (new mechanic).
/// 0 = not yet assigned (legacy save) — assigned on load.
/// </summary>
public double DevelopmentRate { get; set; }

/// <summary>
/// Total Skill gained from appearance-based development so far this season,
/// reset to 0 at the same point <see cref="YellowCardsThisSeason"/> is.
/// Used to enforce the season growth cap in
/// <see cref="PlayerService.ApplyAppearanceGrowth"/>. Never displayed.
/// No FOOT.BAS equivalent (new mechanic).
/// </summary>
public double SkillGainedThisSeason { get; set; }
```

No setter clamp needed on either property — `DevelopmentRate` is just a constant added
to `Skill` each week, `Skill`'s own setter already clamps the result to
`[1.1, PotentialSkill]` (`Player.cs` lines 58–65), and `SkillGainedThisSeason` is a
plain running total that `ApplyAppearanceGrowth` itself keeps under the season cap
(Step 4) before ever adding to `Skill`. Declaration order relative to `Skill`/
`PotentialSkill` doesn't matter for JSON deserialisation since neither property
participates in any clamp.

---

## Step 2 — Remove the match-result skill changes

### `TheManager.Services/PlayerService.cs`

Delete `ApplyPostMatchSkillChanges` entirely (lines 156–180) — the win/+0.03, loss/−0.03,
and clean-sheet/+0.03 deltas all go.

### `TheManager.Services/GameService.cs`

Remove the call site (line 329):

```diff
- PlayerService.ApplyPostMatchSkillChanges(_gameState.Squad, weWon, weLost, cleanSheet);
```

The surrounding morale update (lines 331–334) is untouched — morale still reacts to
results, only `Skill` stops doing so.

### `TheManager.Services/MatchEngineService.cs`

Remove the scorer's flat skill bump in `RecordOurGoal` (line 243) and the now-unused
constant (line 22):

```diff
- private const double SkillBoostPerGoal          = 0.04; // skill gain awarded to the scorer
```

```diff
  scorer.SeasonGoals++;
  scorer.Appearances++;
- scorer.Skill += SkillBoostPerGoal;
- PlayerService.RecalculateStatus(scorer);
  return scorer.Name;
```

(`RecalculateStatus` is only needed here because of the skill change being removed —
drop it too. Goal/appearance stat tracking is untouched.)

---

## Step 3 — Assigning development rate at creation

### `TheManager.Services/PlayerService.cs`

Two new constants alongside the existing `PeakWindowEndAge`/`OffSeasonWeeks`:

```csharp
/// <summary>Rough first-team appearances a season, used only to calibrate
/// DevelopmentRate against PeakAge — not a simulated fixture count.</summary>
private const int AppearancesPerSeason = 30;

/// <summary>Lower bound of the random development-pace multiplier (0.6–1.6).</summary>
private const double DevelopmentPaceMin = 0.6;
```

New helper, callable on its own (needed for the save-migration case in Step 5 where a
player already has a valid `PotentialSkill`/`PeakAge` from an older save and only needs
`DevelopmentRate` backfilled):

```csharp
/// <summary>
/// Rolls the hidden per-appearance Skill gain from the player's current
/// headroom to PotentialSkill, spread over the seasons remaining to PeakAge,
/// and scaled by a random pace multiplier (0.6–1.6) so players with identical
/// headroom still develop at different speeds. New mechanic — no BASIC
/// equivalent. Call after PotentialSkill and PeakAge are both set.
/// </summary>
public static void AssignDevelopmentRate(Player player, Random rng)
{
    double pace           = DevelopmentPaceMin + rng.Next(11) / 10.0;   // 0.6–1.6
    int    seasonsToPeak  = Math.Max(1, player.PeakAge - player.DisplayAge);
    double headroom       = Math.Max(0, player.PotentialSkill - player.Skill);

    player.DevelopmentRate = headroom / (seasonsToPeak * AppearancesPerSeason) * pace;
}
```

`AssignPotential` calls it at the end, so every existing call site gets a development
rate for free with no other edits:

```diff
  public static void AssignPotential(Player player, Random rng)
  {
      player.PeakAge = 26 + rng.Next(5);                       // 26–30 inclusive

      int yearsToPeak        = Math.Max(0, player.PeakAge - player.DisplayAge);
      double baseHeadroom    = 0.3 + rng.Next(3) / 10.0;       // 0.3–0.5, always
      double headroomPerYear = 0.2 + rng.Next(4) / 10.0;       // 0.2–0.5
      player.PotentialSkill  = player.Skill + baseHeadroom
                             + yearsToPeak * headroomPerYear;
      // PotentialSkill's own setter clamps to 9.9
+
+     AssignDevelopmentRate(player, rng);
  }
```

This covers `InitializationService.GeneratePlayer` (line 110) and
`ScoutReportService`'s discovered-player path (line 77) automatically — both already
call `AssignPotential`.

### `TheManager.Services/StaffService.cs` — `PromoteYouthPlayer`

This path sets `PeakAge`/`PotentialSkill` by hand (lines 178–180) instead of calling
`AssignPotential`, so it needs an explicit call to the new helper:

```diff
  player.PeakAge        = 26 + rng.Next(5);
  player.PotentialSkill = Math.Max(skill + 0.3 + rng.Next(3) / 10.0,
                                    youth.PotentialSkillPercent / 10.0);
+ PlayerService.AssignDevelopmentRate(player, rng);
```

---

## Step 4 — Applying growth weekly

### `TheManager.Services/PlayerService.cs`

New method next to `UpdateSquadAppearances` — same slot range, same "did this player
actually turn out for the first team" gate — plus a half-rate case for every squad
member who didn't feature, with both paths routed through a shared helper that
enforces a hard cap on how much `Skill` any one player can gain from development in a
season:

```csharp
/// <summary>Fraction of DevelopmentRate credited to a squad member who was
/// available for the match but didn't feature (the substitute, or a reserve
/// never named).</summary>
private const double NonFeaturingGrowthFactor = 0.5;

/// <summary>Hard ceiling on Skill gained from appearance-based development in
/// a single season, regardless of DevelopmentRate or appearance count.</summary>
private const double SeasonSkillGainCap = 4.0;

/// <summary>
/// Applies each starting player's DevelopmentRate for a match played this
/// week. Everyone else still in the squad (slots 12–20 — the unused
/// substitute plus the reserves) grows at half rate instead of nothing, on
/// the basis that training and being around the first team still counts for
/// something even without minutes played. Every gain is capped so a player
/// can never pick up more than SeasonSkillGainCap of development-driven
/// Skill in one season, however high their DevelopmentRate or however many
/// matches/weeks count toward it. Replaces the old result-based skill deltas
/// (see docs/specs/player-development.md) — growth now depends only on
/// playing time and squad involvement, not the scoreline. New mechanic — no
/// BASIC equivalent.
/// </summary>
public static void ApplyAppearanceGrowth(Player?[] squad)
{
    for (int squadSlot = 1; squadSlot <= 11; squadSlot++)
    {
        var player = squad[squadSlot];
        if (player == null || player.Skill <= 0) continue;

        GrowPlayer(player, player.DevelopmentRate);
    }

    // Slots 12–20: the substitute (12, occupied only if never brought on —
    // ResolveIncident clears it and moves the player into the vacated
    // starting slot the moment a substitution happens, see
    // MatchEngineService.ResolveIncident, lines 174–180) and the reserves
    // (13–20). Anyone occupying one of these slots at the weekly tick either
    // never featured this week, or started and was subbed/sent off/injured
    // out of the XI mid-match — either way they get half credit rather than
    // none.
    for (int squadSlot = 12; squadSlot <= 20; squadSlot++)
    {
        var player = squad[squadSlot];
        if (player == null || player.Skill <= 0) continue;

        GrowPlayer(player, player.DevelopmentRate * NonFeaturingGrowthFactor);
    }
}

/// <summary>
/// Applies a development gain to a player, clipped so their running
/// SkillGainedThisSeason total never exceeds SeasonSkillGainCap.
/// </summary>
private static void GrowPlayer(Player player, double proposedGain)
{
    double remainingAllowance = Math.Max(0, SeasonSkillGainCap - player.SkillGainedThisSeason);
    double actualGain         = Math.Min(proposedGain, remainingAllowance);
    if (actualGain <= 0) return;

    player.Skill                  += actualGain;
    player.SkillGainedThisSeason  += actualGain;
    RecalculateStatus(player);
}
```

### `TheManager.Services/WeeklyTickService.cs`

Call it alongside `UpdateSquadAppearances`, under the same `MatchPlayed` guard
(lines 25–27):

```diff
  if (ctx.MatchPlayed)
+ {
      PlayerService.UpdateSquadAppearances(gameState.Squad);
+     PlayerService.ApplyAppearanceGrowth(gameState.Squad);
+ }
```

### `TheManager.Services/PlayerService.cs` — resetting the cap each season

`SkillGainedThisSeason` must be reset alongside `YellowCardsThisSeason` in
`ApplyEndOfSeasonSkillUpdate` (line 215), or the cap would only ever apply once, for
the player's first season:

```diff
  player.YellowCardsThisSeason = 0;
+ player.SkillGainedThisSeason = 0;
  player.WeeksInjured = Math.Max(0, player.WeeksInjured - OffSeasonWeeks);
```

This reset runs before the `IsRetiring`/`Position == None` early-outs below it in the
loop (same statement group as `YellowCardsThisSeason`), so it applies to every
surviving player regardless of position, matching how the yellow-card tally already
behaves.

No age gating is needed here, deliberately — the same reasoning as
[player-skill-ceiling.md](player-skill-ceiling.md)'s Step 1: `Skill`'s setter already
clamps at `PotentialSkill`, so once a player closes their headroom, further
`DevelopmentRate` additions are silent no-ops. A player who never fully reaches their
ceiling keeps trickling upward for the rest of their career at their fixed rate — in
practice this is a small number for a slow developer, and is increasingly outweighed by
the existing past-30 decline drift (`ApplyEndOfSeasonSkillUpdate`), so it fades out
rather than needing an explicit cutoff.

---

## Step 5 — Save-file compatibility

### `TheManager.Services/SaveLoadService.cs`

`MigrateLegacyPotentials` (lines 137–144) already re-rolls `PeakAge`/`PotentialSkill`
for any player with `PeakAge == 0`, which now also picks up `DevelopmentRate` for free
via the `AssignPotential` change in Step 3. But a player saved *after* the potential
mechanic shipped and *before* this spec has a valid non-zero `PeakAge` already, so that
branch won't fire — add a second condition to backfill just the rate:

```diff
  private static void MigrateLegacyPotentials(GameState state, Random rng)
  {
      foreach (var player in state.Squad)
      {
-         if (player is { PeakAge: 0 })
-             PlayerService.AssignPotential(player, rng);
+         if (player is { PeakAge: 0 })
+             PlayerService.AssignPotential(player, rng);
+         else if (player is { DevelopmentRate: <= 0 })
+             PlayerService.AssignDevelopmentRate(player, rng);
      }
  }
```

`DevelopmentRate` can never legitimately roll to exactly 0 or below (`headroom` is
always ≥ 0 and `pace` is always ≥ 0.6), so `<= 0` safely distinguishes "never assigned"
(the `double` default) from a real rolled value.

---

## Worked example

1. A new 21-year-old midfielder is generated with `Skill = 4.0`. `AssignPotential` rolls
   `PeakAge = 27` (6 years away) and `PotentialSkill = 6.8` (headroom 2.8).
   `AssignDevelopmentRate` rolls `pace = 1.0`: `DevelopmentRate = 2.8 / (6 × 30) × 1.0
   ≈ 0.0156` per appearance.
2. He's a first-team regular — roughly 30 appearances a season. Each week he plays,
   `ApplyAppearanceGrowth` adds `0.0156` to `Skill`; over a full season that's
   `≈ 0.47`, tracking almost exactly the pace the headroom was calibrated for.
3. A teammate generated at the same age and skill rolls `pace = 1.6` instead: his
   `DevelopmentRate ≈ 0.025`, so he closes the same 2.8 headroom in well under 4 seasons
   — a genuine fast developer, purely from the random roll, with no result dependency.
4. A third player rolls `pace = 0.6`: `DevelopmentRate ≈ 0.0093`. He's still developing
   at 27 (his `PeakAge`), just slower — and by the time the past-30 decline drift kicks
   in, it likely outpaces what's left of his trickle.
5. None of this cares whether the team wins, draws, or loses — a squad on a genuine
   losing spiral (see morale floor discussion) still develops its young players exactly
   as fast as a promotion-chasing one, which is the intended behaviour: development is
   about minutes played, not results.
6. A fifth player is named as the substitute (slot 12) but the match passes without
   injury, so he's never brought on. At the weekly tick, slot 12 is still occupied when
   `ApplyAppearanceGrowth` runs, so he gets `DevelopmentRate × 0.5` instead of the full
   amount — credited for being involved in the squad without claiming a full
   appearance's worth of growth for time he didn't play.
7. A sixth player sits in the reserves (slot 15) all week, never named anywhere near
   the matchday squad. He also gets `DevelopmentRate × 0.5` — reserves are no longer
   frozen out entirely, they just develop at half the pace of anyone actually picked.
8. A seventh player starts (slot 3) but is injured in the 60th minute and, with no
   substitution left, is parked in slot 13 for the rest of the match. Because the first
   loop of `ApplyAppearanceGrowth` only looks at slots 1–11 *at the point the weekly
   tick runs* — after the match, not before — he's no longer there to get full credit;
   the second loop picks him up in slot 13 instead and gives him the half rate. Playing
   most of a match still nets less than a full 90 minutes uninterrupted, which is a
   reasonable, if slightly rough, approximation.
9. An eighth player is an exceptional 17-year-old with 13 years to his `PeakAge` of 30
   and a huge rolled headroom. Combined with a `pace` of 1.6, his `DevelopmentRate`
   alone would add well over 4.0 across a big season (cup runs, playoffs — more than
   the ~30 `AppearancesPerSeason` the rate was calibrated against). `GrowPlayer`'s
   `remainingAllowance` check means once his `SkillGainedThisSeason` hits 4.0, every
   further appearance that season is a no-op — he picks back up next season once
   `ApplyEndOfSeasonSkillUpdate` resets the tally to 0.

---

## What does NOT need implementing

- **UI changes** — `DevelopmentRate` is hidden by the same rule as `PotentialSkill`/
  `PeakAge`: never shown in `SquadScreen`, scout reports, or transfer screens.
- **Position-weighted growth** — every position develops the same way; there's no
  GK/defender clean-sheet special case any more, matching the removal in Step 2.
- **Age gating on `ApplyAppearanceGrowth`** — the `PotentialSkill` clamp and the
  existing decline drift together already bound growth; see the note at the end of
  Step 4.
- **Tracking whether the substitute actually came on, or whether a reserve was ever
  close to selection** — the codebase has no separate "played 10 minutes as a sub" or
  "was on the bench" state; `ApplyAppearanceGrowth` infers non-featuring purely from a
  player still occupying slots 12–20 at the weekly tick. No new state is needed to
  support the half-rate case, and it doesn't distinguish "named sub, unused" from
  "permanent reserve, never considered" from "started, then subbed/sent off/injured
  out" — all three land in slots 12–20 by the time the tick runs and are treated
  identically. A finer split (e.g. reserves developing slower than the actual matchday
  substitute) is a possible follow-up, not part of this spec.
- **Transfer-target slots (21–28)** — these hold players from other clubs mid-negotiation,
  not this club's own squad, so `ApplyAppearanceGrowth`'s slot 12–20 loop deliberately
  stops short of them.

---

## Affected files

| File | Change |
|---|---|
| `TheManager.Models/Player.cs` | Add `DevelopmentRate` and `SkillGainedThisSeason` (both hidden, default 0) |
| `TheManager.Services/PlayerService.cs` | Delete `ApplyPostMatchSkillChanges`; add `AssignDevelopmentRate`, `ApplyAppearanceGrowth` (full rate for slots 1–11, half rate for anyone occupying slots 12–20 at the weekly tick) and its `GrowPlayer` helper (enforces `SeasonSkillGainCap`), and the `AppearancesPerSeason`/`DevelopmentPaceMin`/`NonFeaturingGrowthFactor`/`SeasonSkillGainCap` constants; `AssignPotential` calls `AssignDevelopmentRate`; `ApplyEndOfSeasonSkillUpdate` resets `SkillGainedThisSeason` to 0 alongside `YellowCardsThisSeason` |
| `TheManager.Services/GameService.cs` | Remove the `ApplyPostMatchSkillChanges` call site |
| `TheManager.Services/MatchEngineService.cs` | Remove `SkillBoostPerGoal` and the scorer's skill bump in `RecordOurGoal` |
| `TheManager.Services/StaffService.cs` | `PromoteYouthPlayer` calls `AssignDevelopmentRate` after setting `PeakAge`/`PotentialSkill` |
| `TheManager.Services/WeeklyTickService.cs` | Calls `ApplyAppearanceGrowth` alongside `UpdateSquadAppearances` |
| `TheManager.Services/SaveLoadService.cs` | `MigrateLegacyPotentials` backfills `DevelopmentRate` for saves that already have a valid `PeakAge` |
| `TheManager.Tests/` | Delete the two `ApplyPostMatchSkillChanges_*` tests; add tests for `AssignDevelopmentRate` (positive, capped by headroom, pace varies outcome), `ApplyAppearanceGrowth` (starters get full rate, anyone in slots 12–20 gets exactly half, empty slots get nothing, slots 21–28 are untouched, clamps at potential, a season's cumulative gain never exceeds `SeasonSkillGainCap` even across many appearances), the `ApplyEndOfSeasonSkillUpdate` reset of `SkillGainedThisSeason`, and the new save-migration branch |
