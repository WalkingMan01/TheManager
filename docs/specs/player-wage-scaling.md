# Specification: Player Wage Scaling

## Overview

Player wages are currently far too low to feel like a football economy: the formula
ported from FOOT.BAS produces weekly wages in the tens-to-low-hundreds of pounds even
for a Division One squad, with barely any gap between divisions (skill only ranges
1.0–7.9 across all four divisions, so the wage spread is compressed accordingly). This
spec introduces a **global scale-up factor** plus a **division multiplier**, so wages
land in a plausible real-world range and the gap between top-flight and bottom-flight
pay reflects an actual football pyramid rather than a narrow skill-driven spread.

The wage *formula itself* (skill × random multiplier, age discount past 27, star
bonus) is unchanged — this spec only multiplies the result by two new constants. It
also consolidates the formula, which is currently duplicated between
`InitializationService.CalculateWage` (new player creation) and
`ContractService.GetPlayerDemands` (contract renewal), into a single shared method so
the two can never drift apart again.

## BASIC reference

The base formula is unchanged from lines 1077–1082 / 2610 (see the existing XML doc
comments on both methods) — this spec does not touch FOOT.BAS fidelity, it only scales
the port's output. There is no BASIC equivalent for division-based wage scaling; the
original's four divisions already had a much wider skill spread than this port's
`|divisionNumber − 5|` formula produces, which is presumably why wages never felt this
flat in the source material. This is a **deviation**, not a port gap.

---

## Step 1 — New constants

### `TheManager.Models/Constants.cs`

Add a new section alongside the existing ground-capacity constants:

```csharp
// ── Player wages ──────────────────────────────────────────────────────────

/// <summary>
/// Flat multiplier applied to every player's calculated wage, on top of the
/// division multiplier below. Exists as a single dial to retune overall wage
/// levels without touching the underlying skill/age formula.
/// </summary>
public const double WageScaleFactor = 10.0;

/// <summary>
/// Division multiplier applied on top of <see cref="WageScaleFactor"/>, widening
/// the pay gap between divisions well beyond what the narrow 1.0–7.9 skill spread
/// alone would produce — Division One roughly 8x Division Four, mirroring the
/// real gap between Premier League and League Two wages.
/// </summary>
public static double DivisionWageMultiplier(int divisionNumber) => divisionNumber switch
{
    1 => 8.0,
    2 => 4.0,
    3 => 2.0,
    _ => 1.0
};
```

`divisionNumber` (not the `Division` enum) matches the existing convention in
`InitializationService.GeneratePlayer`/`CalculateWage`, which already work in raw
`int divNum` rather than the enum. `ContractService.GetPlayerDemands` takes a
`Division division` parameter and already casts it to `int` for the signing-fee
formula (`(int)division`) — the same cast is used here.

---

## Step 2 — Consolidate and scale the wage formula

### `TheManager.Services/InitializationService.cs`

Change `CalculateWage`'s signature to take the division number and apply both
constants. The floor scales too — otherwise a low-skill veteran in a rich division
would be stuck at an unscaled `£50/week` while every teammate earns thousands:

```csharp
/// <summary>
/// Calculates the weekly wage for a player given their skill, age, and division.
/// Shared by squad generation and contract renewal — extracted for testability
/// aside from the RNG call.
///
/// BASIC lines 1077–1082 / 2610:
///   V(1,Y) = (1 + RND*20 + 50) * INT(H(Y)) [+ 1000 if star]
///   V(1,Y) = INT(V(1,Y) / HV)   where HV = MAX(1, age-27)
///   V(1,Y) = MAX(50, V(1,Y))
/// Deviation: the BASIC result is then scaled by WageScaleFactor and
/// DivisionWageMultiplier (see docs/specs/player-wage-scaling.md) — the original
/// formula alone produces wages far too low to read as a football economy.
/// </summary>
internal static double CalculateWage(double skill, int age, int divisionNumber, Random rng)
{
    int    ageDivisor = Math.Max(1, age - 27);
    double scale      = Constants.WageScaleFactor * Constants.DivisionWageMultiplier(divisionNumber);
    double wageBase    = ((1 + rng.Next(20) + 50) * (int)skill
                          + (skill > 9.6 ? 1_000 : 0)) * scale;
    return Math.Max(50 * scale, (int)(wageBase / ageDivisor));
}
```

Update the call site in `GeneratePlayer` (already has `divNum` in scope):

```csharp
player.WeeklyWage = CalculateWage(player.Skill, player.Age, divNum, rng);
```

### `TheManager.Services/ContractService.cs`

`GetPlayerDemands` currently duplicates the unscaled formula inline (lines 134–138).
Replace it with a call to the now-shared method instead of re-deriving it — this is
the fix that prevents the two formulas drifting apart, which is exactly what would
otherwise happen the next time either one needs retuning:

```csharp
// Wage (BASIC line 2610; scaled — see docs/specs/player-wage-scaling.md)
int statedWage = (int)InitializationService.CalculateWage(
    player.Skill, player.DisplayAge, (int)division, rng);
```

`InitializationService.CalculateWage` stays `internal` — both types are in
`TheManager.Services`, so no accessibility change is needed. `MinimumWeeklyWage`
(the hidden floor a player will actually accept) is unaffected by this spec; it is
computed separately and already scales relative to whatever `statedWage` comes out
as.

---

## Step 3 — TV broadcast income

Scaling wages ~10–80x (global factor × division multiplier) inflates
`Finances.PlayerWageBill` proportionally, which feeds directly into the weekly
finance report and the financial-crisis threshold (`FinancialCrisisService`). Most of
that (starting bank balances, gate/sponsorship income, the crisis threshold) is
deliberately **not** rebalanced here — doing so blind, without playtesting the new
wage levels first, risks overcorrecting in the other direction.

TV broadcast income is the one exception, addressed directly: it was previously a
flat `£20,000` every 5+ weeks for every club regardless of division
(`FinanceService.CalculateWeeklyReport`, "TV broadcast income" block), which is the
one income line most obviously mismatched with a division-scaled wage bill — in
reality TV money is one of the biggest drivers of the pay gap between divisions.
It now reuses `Constants.DivisionWageMultiplier` directly, so it grows in lockstep
with the wage multiplier rather than a separately-tuned curve:

```csharp
if (finances.WeeksSinceLastTvBroadcast >= 5)
{
    double tvIncome = 20_000 * Constants.DivisionWageMultiplier(input.Division);
    report.TvBroadcastIncome = tvIncome;
    weeklyBalance           += tvIncome;
    finances.WeeksSinceLastTvBroadcast = 0;
}
```

Division One: £160,000 every 5+ weeks. Division Two: £80,000. Division Three:
£20,000. Division Four: £10,000.

Recommended follow-up: play a season or two post-change and see whether clubs can
plausibly afford their squads before touching bank balances or the crisis threshold.

---

## Step 4 — Gate receipts (ticket price)

`Club.TicketPriceInPounds` is set once, at `SetupNewGame`, from `5 - division` — i.e.
literally £1–4 per ticket (`InitializationService.cs`, "line 5620: nj=1+(4-AP)"). That
was fine as a straight BASIC port, but it's now wildly disproportionate to the scaled
wage bill: gate money (`attendance × TicketPriceInPounds`, `WeeklyTickService.Process`)
would barely register against a Division Four wage bill that's already grown 5x.

A new `Constants.TicketPriceScaleFactor` (`12.0`) is applied to the existing formula
rather than replacing its shape:

```csharp
gameState.Club.TicketPriceInPounds = (5 - (int)division) * Constants.TicketPriceScaleFactor;
```

Division One: £48. Division Two: £36. Division Three: £24. Division Four: £12 —
realistic modern ticket prices.

**Deliberately not** `Constants.DivisionWageMultiplier` here: `GroundCapacity` already
gives gate money a ~3.75x spread between divisions (30,000 vs 8,000-seat fallback
capacities) before ticket price is even factored in. Stacking the 8x wage multiplier
on top of that would compound into a ~30x gate-money gap between Division One and
Four — disproportionate even by real football standards, where TV money (Step 3), not
gate receipts, drives most of the pay gap between divisions. A flat scale-up keeps the
ticket price realistic and leaves capacity as the main division differentiator for
gate income.

Note `TicketPriceInPounds` is only (re)set in `SetupNewGame` — `JoinNewClub` and
`JoinNewClubMidSeason` (the post-sacking club-change paths) don't touch it, so a
manager who changes division mid-career keeps their old club's ticket price. That gap
predates this spec and is not addressed here.

---

## Step 5 — Transfer fees (asking price / rival bids / forced sales)

`TransferService.CalculateAskingPrice` — the single formula behind every fee quoted in
the game (rival bids in `MarketService.GenerateRivalBid`, scouted players in
`ScoutReportService.RunWeeklyReports`, and the legacy-quote path in
`ScoutReportsScreen`) — is entirely **division-independent**: a skill-7 player costs
the same whether they play in Division One or (hypothetically) Division Four. Against
the new wage bill this collapsed the fee-to-wage ratio unevenly: a Division One
skill-7 player now costs roughly 9–17 weeks of his own wage to buy, while the same
formula applied at Division Four's wage scale would cost ~270–350 weeks — the exact
inverse of what "in line with the wage increase" should mean.

The fix mirrors Step 2's approach — scale the existing formula's *output* — but keyed
to the **selling club's** division (not the buyer's — a rival's aggressiveness,
`rivalDivision` in `GenerateRivalBid`, already separately controls how close to
asking price they bid), and using a **separate** multiplier from wages,
`Constants.TransferFeeDivisionMultiplier`, softer at the bottom of the table
(Division Three 1x, Division Four 0.5x, vs. the wage multiplier's 2x/1x) — transfer
fees at Division Three/Four felt disproportionately steep next to those divisions'
wages when using the same curve as wages:

```csharp
public static double CalculateAskingPrice(Player player, int sellingDivision, Random rng)
{
    // ... unchanged BASIC-derived base/spread/age-deduction logic ...
    double scale = Constants.WageScaleFactor * Constants.TransferFeeDivisionMultiplier(sellingDivision);
    return askingPrice * scale;
}
```

`FinancialCrisisService.ForceSalePrice` — a second, independent copy of the same
formula used when the board force-sells a player to escape a financial crisis — gets
the identical treatment, scaled by the club's own division (available as `club.Division`
at its call site).

**Call-site division wiring:**
- `MarketService.GenerateRivalBid` gains a `sellingDivision` parameter (the listing
  club's own division, from `GenerateIncomingInterest`'s existing `ourDivision`) — kept
  distinct from `rivalDivision`, which still only controls the bid-fraction
  aggressiveness.
- `ScoutReportService.RunWeeklyReports` passes `targetDiv` (the scouted player's source
  club's division, already computed for the team-index draw) straight through.
- `ScoutReportsScreen`'s legacy-quote fallback derives the division from the finding's
  `SourceClubIndex` via the existing `CupService.GetDivisionForTeamIndex` helper.
- `FinancialCrisisService.Evaluate` passes `(int)club.Division`.

---

## Step 6 — Signing-on fees, prize money, staff wages

Three more formulas were left trivial next to the rest of the rescaled economy — all
fixed the same way: multiply the existing (unchanged) formula by
`Constants.WageScaleFactor × Constants.DivisionWageMultiplier(division)`.

**Signing-on fees** (`ContractService.GetPlayerDemands`, was `1,000 × skill / division`
— e.g. £7,000 for a Division One skill-7 player, about 0.2 weeks of that same
player's new wage):

```csharp
int statedFee = (int)((1_000.0 * (int)player.Skill) / (int)division
    * Constants.WageScaleFactor * Constants.DivisionWageMultiplier((int)division));
```

**League prize money** (`SeasonService.AwardLeaguePrizeMoney`, was
`INT(50,000/division)/position` — Division One's title now paid £50,000, less than
1.5 weeks of a single player's wage and less than a third of one TV payment):

```csharp
double prizeAmount = (int)(50_000.0 / (int)division) / finalLeaguePosition;
prizeAmount        *= Constants.WageScaleFactor * Constants.DivisionWageMultiplier((int)division);
```

**Coach/physio/scout/youth-team salaries** (`StaffService.GenerateCoach` /
`GeneratePhysio` / `GenerateScout` / `GenerateYouthPlayer`, all previously a flat
£150–249/wk — or a flat £50/wk for youth-team stipends — in every division): each
gains a `divisionNumber` parameter and multiplies its `WeeklySalary` by a shared
`StaffWageScale(divisionNumber)` helper (same two constants). `HireCoach`/
`HirePhysio`/`HireScout`/`HireYouthPlayer` pass `(int)club.Division` through, and
`InitializationService.GenerateStartingStaff` gains a `Division` parameter (its three
call sites — `SetupNewGame`, `JoinNewClub`, `JoinNewClubMidSeason` — already have the
division in scope).

**Bug fix bundled in, not just a rescale**: `StaffService.PromoteYouthPlayer` was
hardcoding a promoted youth player's `WeeklyWage` to a flat `50` — bypassing
`InitializationService.CalculateWage` entirely, so a promoted youth would earn £50/wk
standing next to Division One teammates on tens of thousands. It now calls
`CalculateWage(skill, youth.Age, divNum, rng)` like every other player, using the
skill and division already computed earlier in the same method.

---

## Worked example

Division One, skill 7.0, age 24 (`ageDivisor = 1`):

- **Before:** `wageBase = (51–70) × 7 = 357–490` → wage **£357–490/week**.
- **After:** `scale = 10 × 8 = 80` → `wageBase = (357–490) × 80 = 28,560–39,200` →
  wage **£28,560–39,200/week** — a plausible mid-table Premier League wage, with a
  genuine star (skill 9.9) reaching `(459–630 + 1,000) × 80 ≈ £116,720–130,400/week`.

Division Four, skill 2.5, age 24:

- **Before:** `wageBase = (51–70) × 2 = 102–140` → wage **£102–140/week**.
- **After:** `scale = 10 × 1 = 10` → wage **£1,020–1,400/week** — in line with real
  League Two pay, and a visibly different world from Division One's tens of
  thousands.

A 34-year-old Division Two player, skill 5.0 (`ageDivisor = 7`):

- **After:** `scale = 10 × 4 = 40` → `wageBase = (51–70) × 5 × 40 = 10,200–14,000` →
  `wage = max(50 × 40, wageBase / 7) = max(2,000, 1,457–2,000) = 2,000–2,000` — the
  scaled floor (`£2,000/week`) now actually bites for an old, modest-skill player in a
  mid-tier division, instead of the old unscaled `£50` floor which no longer reflects
  anything at this scale.

---

## What does NOT need implementing

- **Bank balances, sponsorship income, crisis thresholds** — see Step 3;
  explicitly deferred pending playtesting. (TV income, Step 3; ticket price, Step 4;
  transfer fees, Step 5; and signing-on fees/prize money/staff wages, Step 6, are the
  exceptions — all addressed here, not deferred.)
- **Ticket price for mid-season club changes** (`JoinNewClub`/`JoinNewClubMidSeason`)
  — see Step 4; pre-existing gap, not addressed by this spec.
- **`TransferService.ApplySweetenerDeductions`** (loan/free-transfer deal sweeteners)
  — not called from any production code path today; left with its original unscaled
  reduction amounts rather than guessing at a scale for dead code.
- **`TransferService.EstimateAttendance`** — also not called from production code, and
  already has a pre-existing bug (its own internal, unscaled `5 - division` ticket
  price) unrelated to this spec; left untouched.
- **`ContractService.GetRenewalRequirements`** — dead code (tests only) still carrying
  the fully unscaled original wage formula; pre-existing inconsistency, not fixed here.
- **Youth-team stipend vs. promoted first-team wage** — `GenerateYouthPlayer`'s
  £50/wk-base stipend (now scaled, Step 6) is for players still *in* the youth team;
  once promoted (`PromoteYouthPlayer`, also Step 6) they get a full `CalculateWage`
  wage instead — these are deliberately two different numbers, not a duplicate fix.
- **Manager wage** (`GameState.Club.ManagerWeeklyWage`, set via
  `IncomingOfferService`) — separate mechanic, out of scope.

---

## Affected files

| File | Change |
|---|---|
| `TheManager.Models/Constants.cs` | Add `WageScaleFactor`, `DivisionWageMultiplier(int divisionNumber)`, `TicketPriceScaleFactor`, and `TransferFeeDivisionMultiplier(int divisionNumber)` (a softer, transfer-fee-only division scale — see Step 5) |
| `TheManager.Services/InitializationService.cs` | `CalculateWage` gains a `divisionNumber` parameter and applies both scale constants (including to the floor); `GeneratePlayer` passes `divNum`; `SetupNewGame`'s ticket-price line scales by `TicketPriceScaleFactor` |
| `TheManager.Services/ContractService.cs` | `GetPlayerDemands` calls `InitializationService.CalculateWage` instead of duplicating the formula |
| `TheManager.Services/FinanceService.cs` | TV broadcast income scales by `Constants.DivisionWageMultiplier(input.Division)` instead of a flat `£20,000` |
| `TheManager.Tests/InitializationServiceTests.cs` | Existing `CalculateWage_*` tests gain a `divisionNumber` argument; new tests for scale-factor application and the scaled floor across divisions |
| `TheManager.Tests/ContractServiceTests.cs` | New: verifies `GetPlayerDemands`'s stated wage matches the shared, scaled formula and still varies by division |
| `TheManager.Tests/FinanceServiceTests.cs` | New: verifies TV income scales by division, only fires at 5+ weeks, and resets the counter |
| `TheManager.Tests/GroundCapacityTests.cs` | New: verifies `SetupNewGame`'s ticket price is scaled correctly per division |
| `TheManager.Services/TransferService.cs` | `CalculateAskingPrice` gains a `sellingDivision` parameter and applies `WageScaleFactor × TransferFeeDivisionMultiplier` to the final price |
| `TheManager.Services/MarketService.cs` | `GenerateRivalBid` gains a `sellingDivision` parameter (distinct from `rivalDivision`); `GenerateIncomingInterest` passes `ourDivision` |
| `TheManager.Services/ScoutReportService.cs` | `RunWeeklyReports`'s `CalculateAskingPrice` call passes the scouted player's source-club division (`targetDiv`) |
| `TheManager.Console/Screens/ScoutReportsScreen.cs` | Legacy-quote fallback derives the division via `CupService.GetDivisionForTeamIndex(f.SourceClubIndex)` |
| `TheManager.Services/FinancialCrisisService.cs` | `ForceSalePrice` gains a `sellingDivision` parameter and applies `WageScaleFactor × TransferFeeDivisionMultiplier`; `Evaluate` passes `(int)club.Division` |
| `TheManager.Tests/TransferServiceTests.cs` | New: verifies `CalculateAskingPrice` scales by division, still rewards star players, and still discounts older players |
| `TheManager.Tests/MarketServiceTests.cs` | Existing `GenerateRivalBid_*` tests gain a `sellingDivision` argument; new test verifying the selling division (not just the rival's) changes the bid |
| `TheManager.Services/ContractService.cs` | `GetPlayerDemands`'s `statedFee` scaled by `WageScaleFactor × DivisionWageMultiplier` |
| `TheManager.Services/SeasonService.cs` | `AwardLeaguePrizeMoney`'s `prizeAmount` scaled the same way |
| `TheManager.Services/StaffService.cs` | `GenerateCoach`/`GeneratePhysio`/`GenerateScout`/`GenerateYouthPlayer` gain a `divisionNumber` parameter and a shared `StaffWageScale` helper; `HireCoach`/`HirePhysio`/`HireScout`/`HireYouthPlayer` pass `(int)club.Division`; `PromoteYouthPlayer`'s hardcoded `WeeklyWage = 50` replaced with a real `InitializationService.CalculateWage` call |
| `TheManager.Services/InitializationService.cs` | `GenerateStartingStaff` gains a `Division` parameter, passed from all three call sites (`SetupNewGame`, `JoinNewClub`, `JoinNewClubMidSeason`) |
| `TheManager.Tests/SeasonServiceTests.cs` | New: verifies prize money matches the scaled formula and a higher division pays more |
| `TheManager.Tests/StaffServiceTests.cs` | New: verifies coach/physio/scout/youth-stipend salaries scale by division and a promoted youth no longer earns a flat £50/wk |
| `TheManager.Tests/InitializationServiceTests.cs` | `GenerateStartingStaff` calls gain a `Division` argument |
