# Specification: Player Retirement (Age 32+)

## Overview

Currently, `RandomEventService.CheckRetirementAnnouncement` fires a weekly
`RetirementAnnouncement` event for players over a certain age, but nothing happens as a
result — the player is never actually marked for retirement or removed from the squad.
This spec closes that gap: a player over **age 32** has a weekly chance — increasing
with age — to announce retirement, which sets a dedicated `Player.IsRetiring` flag,
shows `RET` in their squad-screen Age column, and at the end of the season removes them
from the squad.

## BASIC reference

The original game has two separate, overlapping mechanisms:

- **Weekly announcement** (line 2564, current threshold `ABS(G(I))>29`): a 1-in-10
  weekly roll on an over-29 player prints *"… ANNOUNCES HE PLANS TO RETIRE AT THE END OF
  THE SEASON"*.
- **End-of-season forced retirement** (line 5611, `ABS(G(K))>30 AND J(K)<>82`): for 0–2
  random players each season, sets `E(1,K) = -E(1,K)` (retirement-track flag),
  `G(K) = ABS(G(K))` (un-transfer-lists), and `J(K)=82` (status = Retiring).
- **Clearing at new-season setup** (line 2431): `FOR I=1 TO 20 ... IF E(1,I)<0 AND flag
  THEN GOSUB 200` — clears the squad slot (subroutine 200: name, position, skill, age,
  wage, contract all zeroed → empty slot).

This spec **consolidates both BASIC mechanisms into a single age-32 threshold** for
simplicity: one weekly check, at age 33+, that both announces retirement and flags the
player for removal immediately (rather than announcing one week and forcing retirement
separately at season end).

Rather than overloading `E(1,I)`/`SeasonGoals` with a sign-based "retirement-track"
encoding (as BASIC does), this implementation uses a dedicated `Player.IsRetiring`
boolean — equivalent to `J(K)=82`, but tracked as its own flag instead of repurposing
the goals/conceded counter. `SeasonGoals` remains a plain, always-non-negative stat.

---

## Step 1 — Weekly retirement announcement (age 33+)

### `TheManager.Services/RandomEventService.cs` — `CheckRetirementAnnouncement`

Change the age threshold from `<= 29` to `<= 32`, so eligible players are **age 33 and
above** (`player.DisplayAge > 32`).

Skip players already retiring, so they don't announce again every week:

```csharp
if (player == null || player.DisplayAge <= 32 || player.IsRetiring) continue;
```

### Age-scaled chance

Replace the flat 1-in-10 roll with a chance that scales linearly with age: **1% per
year over 32, capped at 10%**. A 33-year-old has a 1% weekly chance, a 36-year-old 4%,
and players 42+ are capped at 10%:

```csharp
int retirementChancePercent = Math.Min(player.DisplayAge - 32, 10);
if (rng.Next(100) >= retirementChancePercent) continue;
```

When the roll succeeds and the event fires, in addition to building the
`RandomEvent`:

1. Flag the player for removal at the end of the season (BASIC `J(K)=82`):

   ```csharp
   player.IsRetiring = true;
   ```

2. Un-transfer-list the player (BASIC `G(K)=ABS(G(K))`):

   ```csharp
   MarketService.UnlistFromTransfer(player);
   ```

The returned `RandomEvent` description is unchanged: *"{Name} announces retirement at
end of season"*.

---

## Step 2 — Remove retiring players at end of season

### `TheManager.Services/PlayerService.cs` — `ApplyEndOfSeasonSkillUpdate`

Called from `SeasonService` (step 8 of `AdvanceToNextSeason` / end-of-season
processing). Before applying the random skill drift, clear any squad slot whose player
is retiring (`IsRetiring`), matching BASIC line 2431's `IF E(1,I)<0 THEN GOSUB 200`
(clear slot):

```csharp
public static void ApplyEndOfSeasonSkillUpdate(Player?[] squad, Random rng)
{
    for (int squadSlot = 1; squadSlot <= 20; squadSlot++)
    {
        var player = squad[squadSlot];
        if (player == null) continue;

        if (player.IsRetiring)
        {
            squad[squadSlot] = null;
            continue;
        }

        if (player.Position == PlayerPosition.None) continue;

        double randomDrift = ((rng.Next(25) / 10.0) - 1.4) / 2;
        player.Skill += randomDrift;

        RecalculateStatus(player);
    }
}
```

Retired players are simply removed — no replacement is generated (matches BASIC
subroutine 200, which only blanks the slot; squad rebuilding/promotion from reserves is
a separate, existing concern not affected by this change).

---

## Worked example

1. Week N: a 34-year-old player (`DisplayAge == 34`) is rolled against a 2% chance
   (`Math.Min(34 - 32, 10)`) and succeeds.
2. `RandomEvent { Type = RetirementAnnouncement, Description = "NAME announces
   retirement at end of season" }` fires.
3. `player.IsRetiring` becomes `true`. If the player was transfer-listed
   (`player.Age < 0`), `MarketService.UnlistFromTransfer` restores a positive `Age`.
4. For the rest of the season, `player.IsRetiring` is `true` so the player won't
   announce again, can no longer be transfer-listed/un-listed
   (`PlayerService.ToggleTransferListed` returns `false`), and the squad screen shows
   `RET` in their Age column (see Step 3). `SeasonGoals` continues to count up normally.
5. At the season-end transition, `ApplyEndOfSeasonSkillUpdate` sees `player.IsRetiring`
   and sets `squad[slot] = null` — the player is gone from the new season's squad.

---

## Step 3 — Show "RET" in the squad screen for retiring players

### `TheManager.Models/Player.cs`

Add a settable property alongside `IsTransferListed`:

```csharp
/// <summary>True if the player has announced retirement and will be removed at the end of the season.</summary>
public bool IsRetiring { get; set; }
```

### `TheManager.Console/Screens/SquadScreen.cs` — `AddSection`

The Age column currently always shows `player.DisplayAge`. When a player is retiring,
show `RET` instead (red, matching the transfer-listed name styling):

```csharp
string age = player is null      ? "[dim]—[/]"
           : player.IsRetiring   ? "[red]RET[/]"
           : player.DisplayAge.ToString();
```

```csharp
table.AddRow(
    $"[dim]{slot}[/]",
    pos,
    name,
    player?.DisplaySkill.ToString() ?? "[dim]—[/]",
    age,
    player?.Temper.ToString()        ?? "[dim]—[/]",
    player?.GamesPlayed.ToString()   ?? "[dim]—[/]",
    wage,
    contract);
```

---

## What does NOT need implementing

- BASIC's separate end-of-season "force 0–2 random over-30 players into retirement"
  roll (line 5611) — superseded by the single age-scaled weekly check above.
- BASIC's flat 1-in-10 weekly roll (line 2564) — replaced with the age-scaled
  1%-per-year (capped at 10%) chance described in Step 1.
- `PlayerStatus.Retiring` — still not tracked on `Player` today (see the
  transfer-list-toggle spec's note on this). `Player.IsRetiring` is a separate,
  purpose-built flag that covers the transfer-list guard
  (`PlayerService.ToggleTransferListed`) and the removal step without needing the full
  `PlayerStatus` enum wired up.
- BASIC's `E(1,I)` sign-overload for the retirement track — `SeasonGoals` stays a plain,
  always-non-negative counter; `MatchEngineService.RecordOurGoal`/`RecordOpponentGoal`
  use a simple increment with no retirement-track special case.
- Squad backfill/promotion after a retirement — if this leaves a first-team slot empty,
  that's handled the same way any other empty slot is (existing squad-management
  screens), not part of this spec.
- Surfacing `RetirementAnnouncement` (and the other `RandomEvent` types) in the console
  UI — `WeeklyTickResult.Events` is not currently consumed anywhere in
  `TheManager.Console`. Wiring up event display is a separate, pre-existing gap.

---

## Affected files

| File | Change |
|---|---|
| `TheManager.Services/RandomEventService.cs` | Raise age threshold to 32 in `CheckRetirementAnnouncement`, replace the flat 1-in-10 roll with an age-scaled 1%-per-year (capped 10%) chance, skip players already retiring, and set `IsRetiring = true` + un-transfer-list newly-announced players |
| `TheManager.Services/PlayerService.cs` | `ApplyEndOfSeasonSkillUpdate` clears squad slots where `IsRetiring` is true before applying skill drift; `ToggleTransferListed` guard checks `IsRetiring` instead of `SeasonGoals < 0` |
| `TheManager.Services/MatchEngineService.cs` | `RecordOurGoal`/`RecordOpponentGoal` revert to a plain `SeasonGoals++` (no retirement-track special case) |
| `TheManager.Models/Player.cs` | Add settable `IsRetiring` property; remove the "retirement-track" note from `SeasonGoals`' doc comment |
| `TheManager.Console/Screens/SquadScreen.cs` | `AddSection` shows `RET` (red) in the Age column for players where `IsRetiring` is true |
