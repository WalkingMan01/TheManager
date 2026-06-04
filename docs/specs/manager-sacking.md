# Specification: Manager Sacking Flow

## Overview

When the manager is sacked the game does **not** end. The original BASIC immediately
resets club finances and transitions the manager into a new job at a different club.
The current C# console path (`ConsoleGame.cs:467`) incorrectly calls
`Environment.Exit(0)` — that must be replaced with the flow below.

---

## Trigger conditions

Sacking is triggered only by **poor on-pitch performance**, not by financial crisis.

| Outcome | Trigger | Location |
|---|---|---|
| `OnNotice` | ≤6 form-points in last 14 matches, dice roll unfavourable, **and** league position is in the bottom 3 of the division | `FinancialCrisisService.Evaluate` |

`OnNotice` sets `ManagerContractWeeks = 0` and returns `CrisisResult.ManagerSacked == true`.

The bottom-3 check uses `Club.LeaguePosition` against the division size (20 teams per
division). A manager in positions 1–17 is safe from sacking regardless of form.

### Financial crisis — no sacking

The outcomes `Sacked` and `SackedSharesSold` from `FinancialCrisisService` must **not**
trigger the sacking flow. When the club's finances reach crisis point the rescue sequence
still runs (loans, mortgage, share sales, force-selling players), but the manager keeps
their job regardless of the outcome. `CrisisResult.ManagerSacked` should return `false`
for these two outcomes.

The `ManagerSacked` property on `CrisisResult` (`FinancialCrisisService.cs:246`) must be
updated to exclude `CrisisOutcome.Sacked` and `CrisisOutcome.SackedSharesSold`:

```csharp
// Before
public bool ManagerSacked =>
    Outcome is CrisisOutcome.Sacked or CrisisOutcome.OnNotice or CrisisOutcome.SackedSharesSold;

// After
public bool ManagerSacked =>
    Outcome is CrisisOutcome.OnNotice;
```

---

## Step 1 — Immediate reset on sacking

When `CrisisResult.ManagerSacked` is true, before any UI:

- Clear all transfer slots (`TransferMarket.IncomingOffers`, buying/selling targets
  in `Squad[21–26]`).

Note: the BASIC zeroed the bank balance (`AI=0`) at line 5445 after any sacking. Since
financial crisis no longer causes sacking, this reset is no longer appropriate and should
**not** be applied.

---

## Step 2 — Display sacking reason

Show all strings from `CrisisResult.Actions` to the player (already produced by the
service). For `OnNotice` this will be:

- *"You are given a week's notice."*

Pause for acknowledgement before continuing.

---

## Step 3 — Generate and present a new job offer

After the player acknowledges, automatically generate one incoming offer from a
lower-division club. The post-sacking offer should come from a club one division lower
than current (clamped to Division 4), at a random league position.

Use `IncomingOfferService.GenerateOffer()` passing a constructed `IncomingOffer` with:

- `BuyingClubDivision` = if currently in Division 4, stay in Division 4; otherwise pick
  randomly from any division strictly lower than the current one (e.g. sacked from
  Division 2 → offer from Division 3 or 4 with equal probability)
- `BuyingClubPosition` = random 1–20
- `BuyingClub` = pick a club name from `GameState.AllTeamNames` in that division

Display the offer terms (club name, division, weekly wage, contract length) using the
same format as a voluntary incoming offer.

The manager **cannot decline** a post-sacking offer — the BASIC gives no exit. If the
player presses any key other than accept, re-show the offer. There is no
"stay at current club" option.

---

## Step 4 — Accept and transition

A new method `InitializationService.JoinNewClubMidSeason()` should be introduced rather
than reusing `JoinNewClub()`, because the season continues — it does not restart.

This method should:

- Swap club name and division (same as `JoinNewClub`)
- Reset cup state, shares, loans, and financial ceilings (same as `JoinNewClub`)
- Generate a fresh squad, coach, physio, scouts, and youth team (same as `JoinNewClub`)
- **Preserve** `CurrentWeek`, `FixturesPlayed`, and `MatchesRemainingThisSeason` — the
  season clock is not reset
- **Replace the league table** with the one for the new division, keeping its current
  mid-season state (points, goal difference, results to date for all clubs in that
  division). `GameState.CurrentLeague` should be swapped to the new division's league.
- The remaining fixtures are regenerated for the new club against opponents from the new
  division, starting from the current week.

After the transition, the weekly game loop resumes at the current week with the new club
and the new division's league table.

---

## Step 5 — Career record

Before calling `Accept()`, append the sacked club's name to `GameState.PreviousClubs`
(already done inside `JoinNewClub` — no extra action needed).

---

## What does NOT need implementing

- The BASIC's signature check (`GOSUB 5542`, line 4481: `INPUT "SIGNATURE:";G$`) —
  this was an anti-piracy measure, not gameplay. Skip it.
- A "choose from multiple offers" screen — the post-sacking offer is force-generated,
  not pulled from the transfer market's existing `IncomingOffers` list.

---

## Affected files

| File | Change |
|---|---|
| `TheManager/ConsoleGame.cs:467` | Replace `Environment.Exit(0)` branch with steps above |
| `TheManager.Services/FinancialCrisisService.cs` | Update `ManagerSacked` to return `true` only for `OnNotice` |
| `TheManager.Services/InitializationService.cs` | Add `JoinNewClubMidSeason()` that preserves season state but swaps division and league table |
| `TheManager.Console/` (Spectre console app) | Same sacking flow in the weekly-tick result handler |

---

## Net effect

A sacked manager (due to poor form only) loses their club and their squad, but immediately
continues the current season managing a lower-division side with a new set of everything.
The league table they inherit is the live mid-season table for the new division. Financial
mismanagement carries consequences through the rescue sequence but never ends the
manager's tenure.
