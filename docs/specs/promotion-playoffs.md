# Specification: Championship / League One / League Two Promotion Play-offs

## Overview

Today, promotion out of the Championship, League One, and League Two (`Division.Two`,
`Division.Three`, `Division.Four` — see `Ui.DivisionName`) is a flat "top 3 go up"
rule (`SeasonService.DetermineNewDivision`, `PromoteAndRelegateActualTeams`), identical
to relegation's "bottom 3 go down". This spec replaces that with the real English
football structure, which is **not uniform across divisions**:

| Division | Automatic promotion | Play-off field | Relegation |
|---|---|---|---|
| Premier League (`Division.One`) | — (top flight) | — | bottom 3 |
| Championship (`Division.Two`) | top 2 | 3rd–6th (3v6, 4v5) | bottom 3 |
| League One (`Division.Three`) | top 2 | 3rd–6th (3v6, 4v5) | **bottom 4** |
| League Two (`Division.Four`) | **top 3** | **4th–7th (4v7, 5v6)** | — (bottom of the pyramid) |

The play-off is always a 4-team knockout — 3rd hosts 6th, 4th hosts 5th in the
Championship/League One shape, or 4th hosts 7th, 5th hosts 6th in League Two's — and
the two winners meet at Wembley for the last promotion spot. This keeps team counts
balanced across the boundary between each pair of divisions: League One's 4
relegated sides are replaced by League Two's 4 promoted sides (3 automatic + 1
play-off), exactly as Championship's 3 relegated are replaced by League One's 3
promoted (2 automatic + 1 play-off).

There is **no BASIC equivalent** — FOOT.BAS (1988) predates the English play-off
system (introduced 1986–87 but never modelled in the original game) — so this is a new
mechanic, not a port. It reuses two mechanisms already built for the FA Cup
(`docs/specs/fa-cup.md`): `PenaltyShootoutService`'s kick-by-kick shootout, and the
"final at Wembley, neutral venue" treatment.

**Semi-finals are two legs**, matching the real competition: the lower seed hosts
leg 1, the higher seed hosts the decisive leg 2. Aggregate score across both legs
decides the tie. **Leg 2 is 90 minutes plus injury time only — no extra time**; if
the aggregate is still level at that final whistle, the tie goes straight to
penalties (the away-goals tiebreaker was abolished for 2019–20 and is **not**
reintroduced here). The final stays a **single match** at Wembley, with extra time
before penalties on a draw, as in the real competition.

**Scope:** only the division the player's club actually plays in has a real,
simulated `LeagueTable` — the other three divisions are never played match-by-match
(`SeasonService.SwapPromotedRelegatedTeams` just swaps array slots blindly, with no
table, no real play-off). This spec only builds a real, played play-off for the
player's division; the two AI-only divisions keep a blind swap — no real table,
no simulated play-off — but the **swap count at the League One/League Two boundary
still needs to move to 4 teams** (was a flat 3 for every boundary), otherwise that
boundary silently loses team-count balance the moment the player's own division
enforces the asymmetric 4-relegated/4-promoted rule on it. See Step 4.

---

## Step 1 — Model additions

### `TheManager.Models/Enums/MatchType.cs`

```csharp
/// <summary>A promotion play-off semi-final or final (Divisions Two–Four only).</summary>
Playoff
```

### `TheManager.Models/Constants.cs`

```csharp
/// <summary>Play-off semi-final, leg 1: the lower seed's home leg. First matchday
/// after the regular 54-matchday calendar. Only scheduled for clubs whose table
/// position calls for it.</summary>
public const int PlayoffSemiFinalFirstLegMatchday = SeasonMatchdays + 1; // 55

/// <summary>Play-off semi-final, leg 2: the higher seed's home leg (decisive —
/// aggregate score over 90 minutes, straight to penalties if still level; no
/// extra time).</summary>
public const int PlayoffSemiFinalSecondLegMatchday = SeasonMatchdays + 2; // 56

/// <summary>Play-off final matchday, at Wembley (neutral venue, single match).</summary>
public const int PlayoffFinalMatchday = SeasonMatchdays + 3; // 57

/// <summary>Teams promoted automatically (before the play-off), per division.
/// League Two promotes 3 automatically; Championship and League One promote 2.</summary>
public static int AutomaticPromotionSpots(Division division)
    => division == Division.Four ? 3 : 2;

/// <summary>
/// Teams relegated at the end of the season, per division. League One relegates 4
/// (to keep pace with League Two's 4 promoted places: 3 automatic + 1 play-off);
/// every other division relegates 3. Division Four has nothing below it to relegate to.
/// </summary>
public static int RelegationSpots(Division division)
    => division == Division.Three ? 4 : 3;
```

The play-off field is always the 4 positions directly below the automatic spots:
`AutomaticPromotionSpots(division) + 1` through `+ 4`. For Championship/League One
that's 3rd–6th; for League Two, 4th–7th. No separate constant needed — `PlayoffService`
derives it from `AutomaticPromotionSpots`.

These are **not** added to `FixtureSchedulerService.BuildSeasonCalendar`'s fixed
54-matchday array. Unlike FA Cup rounds (interleaved with league play all season,
so they need a fixed slot from day one), play-offs only ever happen after matchday
54 is complete and the final table is known — so their `ScheduledMatch` entries are
generated on demand (Step 3) and appended to `GameState.Fixtures`, exactly like any
other fixture `GetCurrentMatch` can look up. Clubs that don't need them simply never
get a Week-55/56/57 entry, and `GetCurrentMatch` falls through to `EndOfSeason` as it
does today — no change to that fallback.

### `TheManager.Models/GameState.cs` — new `PlayoffState`

Small piece of state that has to survive a save/load across all three play-off
matchdays (the leg-1 score, and "who's in the final"), and lets `RunEndOfSeason`
know whether it's mid play-off:

```csharp
/// <summary>State of the current season's promotion play-off (Divisions Two–Four
/// only). Reset at the start of every season.</summary>
public PlayoffState Playoff { get; set; } = new();
```

### New file `TheManager.Models/PlayoffState.cs`

```csharp
namespace TheManager.Models;

/// <summary>
/// Tracks an in-progress two-legged promotion semi-final plus single-match final
/// for the division the player's club is playing in. No FOOT.BAS equivalent — see
/// promotion-playoffs.md.
/// </summary>
public class PlayoffState
{
    /// <summary>True once the play-off has been set up for this season (after matchday 54).</summary>
    public bool Active { get; set; }

    /// <summary>True if the player's club finished in the play-off field and is contesting it.</summary>
    public bool PlayerInvolved { get; set; }

    /// <summary>True if the player's club is the higher seed of its semi-final (hosts leg 2,
    /// the decisive leg). False means the player hosts leg 1 and travels for leg 2.</summary>
    public bool PlayerIsHigherSeed { get; set; }

    /// <summary>Our goals in leg 1. Null until leg 1 has been played.</summary>
    public int? FirstLegOurScore { get; set; }

    /// <summary>Opponent goals in leg 1. Null until leg 1 has been played.</summary>
    public int? FirstLegTheirScore { get; set; }

    /// <summary>The winner of the semi-final not involving the player's club, resolved
    /// immediately (as a single aggregate result — see PlayoffService.SimulateTie)
    /// once the final table is known. Opponent in the final if the player wins their semi.</summary>
    public string OtherSemiFinalWinner { get; set; } = string.Empty;

    /// <summary>Name of the club promoted via the play-off, once decided.</summary>
    public string Winner { get; set; } = string.Empty;

    public bool IsResolved => !string.IsNullOrEmpty(Winner);
}
```

---

## Step 2 — `PlayoffService` (new)

### `TheManager.Services/PlayoffService.cs`

Stateless, `Random` injected per the project convention.

```csharp
public static class PlayoffService
{
    /// <summary>
    /// Builds the 4-team play-off field starting from the position directly below
    /// the automatic promotion spots (0-based index <c>autoSpots</c> through
    /// <c>autoSpots + 3</c>): 3rd vs 6th / 4th vs 5th when <c>autoSpots == 2</c>
    /// (Championship, League One), or 4th vs 7th / 5th vs 6th when
    /// <c>autoSpots == 3</c> (League Two). Each pair is <c>(HigherSeed, LowerSeed)</c>
    /// — not a fixed home/away, since a two-legged tie has both teams host a leg
    /// (Step 3 decides which matchday is whose home leg).
    /// </summary>
    public static (string HigherSeedA, string LowerSeedA, string HigherSeedB, string LowerSeedB)
        BuildSemiFinals(LeagueTable table, int autoSpots);

    /// <summary>
    /// Resolves a play-off tie not involving the player as a single aggregate
    /// result — used for (a) a semi-final the player isn't part of, standing in for
    /// the full two legs, and (b) the Wembley final, which really is single-match.
    /// Score weighted by the gap between the two teams' final league positions
    /// (closer positions = more even game), mirroring
    /// LeagueService.SimulateOtherFixtures' position-weighted formula rather than
    /// CupService.SimulateTie's division-gap formula (both teams are in the same
    /// division here). A level result goes to penalties, reusing
    /// CupService.SimulateTie's shootout-odds shape (edge to the better-placed side).
    /// </summary>
    public static PlayoffTieResult SimulateTie(
        string homeTeam, int homePosition,
        string awayTeam, int awayPosition,
        Random rng);
}

public class PlayoffTieResult
{
    public string Winner { get; set; } = string.Empty;
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public bool WonOnPenalties { get; set; }
    public int? HomePenalties { get; set; }
    public int? AwayPenalties { get; set; }
}
```

Score formula (deliberately close to `LeagueService.SimulateOtherFixtures`'s
`R = 1 + INT(RND*8) + (KI<KH)*2 - 5` shape, since these are same-division sides):

```csharp
int homeScore = Math.Max(0, 1 + rng.Next(8) + (homePosition < awayPosition ? 2 : 0) - 5);
int awayScore = Math.Max(0, 1 + rng.Next(7) + (awayPosition < homePosition ? 2 : 0) - 5);
```

Level → penalties, same shape as `CupService.SimulateTie`'s shootout branch (edge to
the better-placed side instead of the higher division).

---

## Step 3 — Wiring into `SeasonService.RunEndOfSeason` / `GameService`

### Trigger point: `GameService.RunEndOfSeason()`

Today this calls `SeasonService.WrapUpSeason` directly once `MatchType.EndOfSeason`
is reached (matchday 55 today, i.e. `Constants.SeasonMatchdays + 1`). New logic,
**only when `club.Division != Division.One`**:

1. `int autoSpots = Constants.AutomaticPromotionSpots(club.Division)` — 2 for
   Championship/League One, 3 for League Two. The play-off field is
   `gameState.CurrentLeague.Entries[autoSpots .. autoSpots + 3]` (3rd–6th, or
   4th–7th for League Two).
2. `PlayoffService.BuildSemiFinals(table, autoSpots)` gives the two ties, each as
   `(HigherSeed, LowerSeed)`.
3. **If the player's club is not among the four** (finished inside the automatic
   spots, or outside the play-off field entirely): resolve both semis (each as a
   single aggregate result via `PlayoffService.SimulateTie` — see Step 2) and then
   the final the same way (no matchdays consumed), store the winner in
   `gameState.Playoff.Winner`, and go straight to `WrapUpSeason` as today — the
   only change downstream is which team fills the last promotion slot (Step 4).
4. **If the player's club is one of the four:** don't call `WrapUpSeason` yet.
   - Simulate the *other* semi-final immediately (single aggregate result); store
     its winner in `gameState.Playoff.OtherSemiFinalWinner`.
   - Set `gameState.Playoff.PlayerIsHigherSeed` from which side of our tie we're
     on (higher seed hosts leg 2, the decisive leg — see Overview).
   - Append leg 1's `ScheduledMatch { MatchType = Playoff, Week =
     Constants.PlayoffSemiFinalFirstLegMatchday, OpponentName = <our semi
     opponent>, IsHomeGame = !PlayerIsHigherSeed }` to `gameState.Fixtures` (the
     lower seed hosts leg 1).
   - Set `gameState.Playoff.Active = true`, `PlayerInvolved = true`,
     `gameState.CurrentWeek = Constants.PlayoffSemiFinalFirstLegMatchday`.
   - Return without ending the season — the normal week-hub/`PlayMatch` loop picks
     up matchday 55 exactly like any other fixture.

### `GameService.PreparePlayoffMatchday()` (mirrors `PrepareCupMatchday`)

No draw needed (opponent already resolved in step 3/5 below) — this method exists
only for symmetry with the cup flow and can be a no-op placeholder, or folded away
entirely if nothing needs preparing.

### `GameService.PlayMatch()` — new `Playoff` branch

Alongside the existing `FACup` branch. Three matchdays can occur, distinguished by
`gameState.CurrentWeek`:

- **Matchday 55 (semi-final, leg 1) result:** always 90 minutes, no extra time or
  penalties — a single leg is never decisive on its own. Store the result:
  `gameState.Playoff.FirstLegOurScore/FirstLegTheirScore = OurScore/TheirScore`.
  Append leg 2's `ScheduledMatch { MatchType = Playoff, Week =
  Constants.PlayoffSemiFinalSecondLegMatchday, OpponentName = <same opponent>,
  IsHomeGame = PlayerIsHigherSeed }` (venue flips). Set `CurrentWeek =
  Constants.PlayoffSemiFinalSecondLegMatchday` and return.
- **Matchday 56 (semi-final, leg 2) result:** 90 minutes plus injury time — **no
  extra time period** — then compute the aggregate: `aggOurs = FirstLegOurScore +
  OurScore`, `aggTheirs = FirstLegTheirScore + TheirScore` (both already
  club-relative, so summing across legs needs no home/away reorientation). **If
  level on aggregate**, go straight to penalties — reusing the FA Cup's
  `PenaltyShootoutService` and on-screen flow (`docs/specs/fa-cup.md` Step 5), just
  entered directly from full time instead of after an extra-time period — to decide
  the tie right there.
  - **We lose the aggregate/shootout:** play-offs are over for us. Resolve the
    final between the club that eliminated us and
    `gameState.Playoff.OtherSemiFinalWinner` via `PlayoffService.SimulateTie`
    (single aggregate result, silent), set `gameState.Playoff.Winner`, then call
    `SeasonService.WrapUpSeason` — our division is unchanged.
  - **We win:** append the matchday-57 final `ScheduledMatch { MatchType =
    Playoff, Week = Constants.PlayoffFinalMatchday, OpponentName =
    OtherSemiFinalWinner }`, neutral venue. Set `gameState.CurrentWeek =
    Constants.PlayoffFinalMatchday` and return — same "let the normal loop pick it
    up" pattern as step 3.
- **Matchday 57 (final) result:** single match; extra time then penalties on a
  draw, same treatment as the FA Cup final. Win or lose, set
  `gameState.Playoff.Winner` (us, if we won) and call `SeasonService.WrapUpSeason`.

### Neutral venue (Wembley)

Derived the same way the FA Cup final is: `MatchType.Playoff` **and**
`Week == Constants.PlayoffFinalMatchday` ⇒ neutral venue, fixed attendance split
50/50 (reuse the FA Cup Wembley gate constants — no new ones needed). Both semi-final
legs are normal home games for whichever side is hosting that leg, full gate-receipt
logic applies to each.

---

## Step 4 — `SeasonService` changes

### `DetermineNewDivision` — new parameter

```csharp
public static Division DetermineNewDivision(
    int      finalLeaguePosition,
    Division currentDivision,
    bool     promotedViaPlayoff = false)
{
    int divisionNumber   = (int)currentDivision;
    int teamCount        = Constants.TeamCount(currentDivision);
    int relegationSpots  = Constants.RelegationSpots(currentDivision);
    int autoSpots        = Constants.AutomaticPromotionSpots(currentDivision);

    if (finalLeaguePosition > teamCount - relegationSpots && divisionNumber < 4)
        return (Division)(divisionNumber + 1);

    // Automatic promotion.
    if (finalLeaguePosition <= autoSpots && divisionNumber > 1)
        return (Division)(divisionNumber - 1);

    // Play-off promotion: the 4 places below the automatic spots, only if won.
    if (finalLeaguePosition > autoSpots && finalLeaguePosition <= autoSpots + 4
        && divisionNumber > 1 && promotedViaPlayoff)
        return (Division)(divisionNumber - 1);

    return currentDivision;
}
```

`WrapUpSeason` passes `promotedViaPlayoff: club.Name == gameState.Playoff.Winner`.

### `PromoteAndRelegateActualTeams` — last promotion slot comes from the play-off

The automatic-spot swap is unchanged in shape but now runs `AutomaticPromotionSpots`
times instead of a hardcoded 2 or 3; the play-off winner always fills the final slot
of the division above. Relegation likewise runs `RelegationSpots` times instead of a
hardcoded 3:

```csharp
public static void PromoteAndRelegateActualTeams(
    string[] allTeamNames, LeagueTable table, string playoffWinner)
{
    int divisionNumber = (int)table.Division;
    int autoSpots       = Constants.AutomaticPromotionSpots(table.Division);
    int relegationSpots = Constants.RelegationSpots(table.Division);

    if (divisionNumber > 1)
    {
        var (_, aboveEnd) = Constants.DivisionRange((Division)(divisionNumber - 1));
        for (int i = 0; i < autoSpots; i++)
            SwapTeamIntoSlot(allTeamNames, table.Entries[i].TeamName, aboveEnd - autoSpots + i);
        SwapTeamIntoSlot(allTeamNames, playoffWinner, aboveEnd);
    }

    if (divisionNumber < 4)
    {
        var (belowStart, _) = Constants.DivisionRange((Division)(divisionNumber + 1));
        for (int i = 0; i < relegationSpots; i++)
            SwapTeamIntoSlot(allTeamNames, table.Entries[table.Entries.Count - 1 - i].TeamName, belowStart + i);
    }
}
```

League One relegating 4 teams into a division (League Two) whose top-of-table slots
only ever receive `autoSpots (3) + 1 playoff winner = 4` swapped-in teams keeps the
array-index arithmetic self-consistent — `belowStart..belowStart+3` are exactly the
same 4 slots League Two's own promotion swap vacates.

### `SwapPromotedRelegatedTeams` — swap count follows the boundary

This is the blind, no-table swap used for whichever Div1/2-or-Div3/4 boundary the
player's own division didn't touch this season (Step 5b of `WrapUpSeason`). It swaps
a fixed 3-for-3 today; the League One/League Two boundary (`upperDivisionNumber ==
3`) now needs 4-for-4 to match the real rule at that boundary:

```csharp
public static void SwapPromotedRelegatedTeams(string[] allTeamNames, int upperDivisionNumber)
{
    var (_, upperEnd)   = Constants.DivisionRange((Division)upperDivisionNumber);
    var (lowerStart, _) = Constants.DivisionRange((Division)(upperDivisionNumber + 1));

    int swapCount = upperDivisionNumber == 3 ? 4 : 3; // League One/League Two boundary = 4

    for (int i = 0; i < swapCount; i++)
    {
        int upperSlot = upperEnd - swapCount + 1 + i;
        int lowerSlot = lowerStart + i;
        (allTeamNames[upperSlot], allTeamNames[lowerSlot]) =
            (allTeamNames[lowerSlot], allTeamNames[upperSlot]);
    }
}
```

`WrapUpSeason` passes `gameState.Playoff.Winner` (already resolved by step 3/5 before
`WrapUpSeason` is ever called).

### Reset for the new season

`gameState.Playoff = new PlayoffState();` alongside the other end-of-season resets in
`WrapUpSeason` (step 9, next to `ResetMatchState`).

---

## Step 5 — UI (`TheManager.Console`)

- **`WeekHubScreen`** — matchdays 55/56/57 show "PLAY-OFF SEMI FINAL — 1ST LEG vs
  \<opponent\> (H/A)", "PLAY-OFF SEMI FINAL — 2ND LEG vs \<opponent\> (H/A)" (with
  the running aggregate, e.g. "agg 2–1"), and "PLAY-OFF FINAL — WEMBLEY" when the
  club has reached them, matching the FA Cup semi/final treatment.
- **`FixturesScreen`** — the extra rows only appear for a club that actually played
  them (they're not part of the static 54-row calendar); append them to the
  displayed list when present, with a "PO" type label, "1st"/"2nd leg" annotation,
  and "N" venue for the final.
- **News / season summary** — when the player isn't involved, still report who won
  the play-off and earned the third promotion spot (silent resolution shouldn't be
  invisible to the player if it's a divisional rival).

---

## Worked example

Managing a League One (Division Three) club:

1. Matchday 54 completes; final table has us 4th, paired against 5th place. As the
   higher seed, `gameState.Playoff.PlayerIsHigherSeed = true` — we host leg 2 and
   travel for leg 1. The 3rd-vs-6th tie is resolved immediately as a single
   aggregate result: 6th wins on penalties (`OtherSemiFinalWinner` set).
   `CurrentWeek` becomes 55; leg 1 (away) is added as a `Playoff` fixture.
2. Matchday 55 (leg 1, away): we draw 1–1. `FirstLegOurScore/FirstLegTheirScore =
   1/1`. `CurrentWeek` becomes 56; leg 2 (home) is added.
3. Matchday 56 (leg 2, home): we win 2–1 at the 90-minute whistle — aggregate 3–2
   to us, already decisive, so the match ends there (no extra time is ever played
   in this leg). `CurrentWeek` becomes 57; a neutral-venue `Playoff` final fixture
   vs the pens-winning 6th-place side is added.
4. Matchday 57 (Wembley, single match): 1–1 after extra time, we win 4–3 on
   penalties. `gameState.Playoff.Winner` = our club name.
5. `WrapUpSeason` runs: `DetermineNewDivision(4, Division.Three, promotedViaPlayoff:
   true)` returns `Division.Two`. `PromoteAndRelegateActualTeams` promotes the 1st-
   and 2nd-place clubs automatically and us as the play-off winner; the bottom 4 are
   relegated (`RelegationSpots(Division.Three) == 4`).

Contrast: if leg 2 had instead finished 1–0 to us (aggregate 2–2, level), the match
goes straight from the 90-minute whistle to penalties — no extra time — and if we
lost that shootout, `WrapUpSeason` runs immediately (no final for us to play) with
`promotedViaPlayoff: false` — we stay in Division Three regardless of how the final
we're not in turns out.

**League Two contrast** — managing a Division Four club that finishes 5th:
`autoSpots = Constants.AutomaticPromotionSpots(Division.Four) = 3`, so the play-off
field is positions 4–7, not 3–6; we're paired against 6th place (not 5th, which our
own read of "3rd–6th" would have picked). As the higher seed we again host leg 2.
Everything else (aggregate over 90 minutes, straight-to-penalties on a level
aggregate, the extra-time Wembley final, `promotedViaPlayoff`) proceeds identically.

---

## What does NOT need implementing

- **Away-goals tiebreaker** — deliberately not implemented; an aggregate-level tie
  after leg 2's 90 minutes goes straight to penalties, matching the modern rule
  (see Overview).
- **Extra time in the semi-final** — leg 2 is 90 minutes plus injury time only;
  extra time is exclusive to the Wembley final.
- **Play-offs for the two AI-only divisions** the player isn't managing this season
  — they keep the existing blind top-3/bottom-3 array swap
  (`SwapPromotedRelegatedTeams`), unchanged.
- **Premier League (`Division.One`) play-offs** — it's the top flight; nothing to be
  promoted to.
- **Persisting play-off history across seasons** — `SeasonRecord` gets no new field;
  `WasPromoted` on `SeasonSummary` already reflects the outcome correctly once
  `DetermineNewDivision` accounts for the play-off.
- **Relegation play-offs** — out of scope; this spec only touches promotion.

---

## Affected files

| File | Change |
|---|---|
| `TheManager.Models/Enums/MatchType.cs` | Add `Playoff` |
| `TheManager.Models/Constants.cs` | `PlayoffSemiFinalFirstLegMatchday`, `PlayoffSemiFinalSecondLegMatchday`, `PlayoffFinalMatchday`, `AutomaticPromotionSpots(division)`, `RelegationSpots(division)` |
| `TheManager.Models/PlayoffState.cs` | New: `Active`, `PlayerInvolved`, `PlayerIsHigherSeed`, `FirstLegOurScore`, `FirstLegTheirScore`, `OtherSemiFinalWinner`, `Winner` |
| `TheManager.Models/GameState.cs` | Add `Playoff` property |
| `TheManager.Services/PlayoffService.cs` | New: `BuildSemiFinals(table, autoSpots)`, `SimulateTie`, `PlayoffTieResult` |
| `TheManager.Services/SeasonService.cs` | `DetermineNewDivision` gains `promotedViaPlayoff` and reads `AutomaticPromotionSpots`/`RelegationSpots` instead of hardcoded 3/4; `PromoteAndRelegateActualTeams` loops `autoSpots`/`relegationSpots` times and takes the resolved play-off winner for the last promoted slot; `SwapPromotedRelegatedTeams` swaps 4-for-4 at the League One/League Two boundary instead of a flat 3; `WrapUpSeason` resolves/consumes `gameState.Playoff` and resets it for the new season |
| `TheManager.Services/GameService.cs` | `RunEndOfSeason` branches into play-off setup for Divisions Two–Four, using each division's own `autoSpots`; new `Playoff` branch in `PlayMatch` (leg 1 → leg 2 → aggregate/extra-time/penalties → Wembley final progression across matchdays 55–57) |
| `TheManager.Console/Screens/WeekHubScreen.cs`, `FixturesScreen.cs` | Play-off leg 1/leg 2/final display, running aggregate |
| `TheManager.Tests/` | `DetermineNewDivision` promoted/not-promoted via play-off cases for both the 2-auto and 3-auto shapes; `RelegationSpots` bottom-4 case for League One; `PromoteAndRelegateActualTeams` with a non-adjacent play-off winner in both shapes; `SwapPromotedRelegatedTeams` 4-for-4 at the League One/League Two boundary; `PlayoffService.BuildSemiFinals` pairing for both `autoSpots` values (3v6/4v5 and 4v7/5v6); `SimulateTie` position-weighted outcome + penalties path; aggregate calculation across two legs (decisive at 90, and level-on-aggregate → extra time/penalties); `RunEndOfSeason` player-involved vs player-not-involved branching |
