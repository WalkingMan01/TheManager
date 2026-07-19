# Specification: Gate Receipts Based on Actual Ground Capacity

## Overview

Gate receipts are currently **attendance × ticket price**, where attendance comes from
`WeeklyTickService.CalculateGateAttendance` — a demand formula (division, both teams'
league positions, random jitter) with no notion of how many people the ground can
actually hold. The only capacity-like behaviour is a magic cap of **18,721** applied
when a Division 1/2 club has an unfinished ground improvement, and the improvement
itself (`GroundImprovementService`) is a boolean: paying `INT(1,500,000 / Division)`
flips `GroundImprovementCost` to 0 and nothing else changes. A Division One club at
the top of the table can "draw" 70,000+ fans into a ground that notionally holds
18,721 the week before, and unlimited numbers the week after.

This spec gives every club a real **ground capacity**:

- **New model field** `Club.GroundCapacity` *(new — no direct BASIC equivalent)*,
  seeded by division at new-game/new-club time.
- **Attendance becomes `min(demand, capacity)`** — the existing formula is kept
  verbatim as the *demand* side; capacity is the supply side. Gate receipts stay
  `attendance × TicketPriceInPounds`, so the finance pipeline is untouched.
- **Ground improvement now means something physical**: buying it raises
  `GroundCapacity` to the division maximum instead of just clearing a flag. The
  magic 18,721 cap is deleted — an unimproved ground is simply smaller.
- **Sell-outs are detected and reported** ("SOLD OUT") in the post-match screen,
  giving the player a visible signal that the ground upgrade would pay for itself.
- **Wembley is unaffected** — semi-finals and the final keep their fixed 80,000 /
  100,000 neutral-venue attendance handled in `GameService`.

---

## BASIC reference

- Subroutine **3801** (`FOOT.BAS:3197–3229`) — the attendance/demand formula ported in
  `WeeklyTickService.CalculateGateAttendance`. Unchanged by this spec except for the
  final clamp.
- The **18,721** cap inside that subroutine (applied when `NI > 0` in Divisions 1–2) is
  the original's only nod to capacity. It is *replaced* by `GroundCapacity`, not kept
  alongside it.
- Subroutines **4201–4206** (`FOOT.BAS:3623–3642`) — the one-time ground improvement
  purchase (`NI = INT(1,500,000 / Division)`, cleared to 0 on purchase), ported in
  `GroundImprovementService`. This spec extends its effect; cost and one-shot nature
  are unchanged.
- BASIC line **5546** (`NI=NI-ABS(O(1,IB)>2)*NI`) — Division 3+ clubs never get the
  improvement option. Preserved: their capacity is fixed for the season.

The original has no per-club capacity variable; everything here that isn't the demand
formula or the improvement purchase is new behaviour.

---

## Design decisions

- **Demand and capacity are separated, not blended.** The existing formula already
  models *interest* in the fixture well (division, form of both sides, jitter). Capping
  it is the smallest change that produces correct behaviour, keeps every existing
  BASIC-derived constant intact, and makes sell-outs a meaningful event.
- **Capacity is seeded by division with a small random spread**, so two clubs in the
  same division don't have identical grounds. The spread is rolled once at
  initialisation and persisted — it never re-rolls.
- **Unimproved vs improved is a two-state model**, matching the existing one-shot
  upgrade. Multi-stage stand-by-stand building is explicitly out of scope.
- **Capacity follows the club, not the manager.** On sacking/`JoinNewClub`, the new
  club's capacity is seeded fresh for its division; on promotion/relegation the ground
  does *not* change size (a promoted Division 2 club plays Division 1 football in a
  Division 2 ground — which is exactly when sell-outs and the upgrade get interesting).
  The improvement option re-seeds per `GroundImprovementService.InitialiseForDivision`
  as today.
- **Save compatibility by derivation**: `GroundCapacity == 0` on a loaded save means
  "pre-capacity save" — derive it from the club's division and improvement state at
  load time rather than versioning the save format.

### Tunable constants (suggested values)

| Division | Unimproved capacity | Improved (maximum) capacity |
|----------|--------------------:|----------------------------:|
| 1        | 18,000              | 48,000                      |
| 2        | 14,000              | 30,000                      |
| 3        | 9,000               | — (no improvement option)   |
| 4        | 5,500               | — (no improvement option)   |

Plus a per-club jitter of **±10%**, rolled once at seed time (`capacity =
base × (0.90 + rng.NextDouble() × 0.20)`, rounded to the nearest 100). The Division 1/2
unimproved values deliberately bracket the original's 18,721 cap; Division 3/4 values
sit just above each division's typical peak demand so sell-outs happen for a
promotion-chasing side but not every week. All six numbers live in `Constants` so they
can be tuned without touching logic.

---

## Step 1 — Model: `Club.GroundCapacity`

`TheManager.Models/Club.cs`, in the existing `// ── Ground ──` section:

```csharp
/// <summary>
/// Ground capacity in spectators. New — no BASIC equivalent; the original's
/// only capacity notion was a hard 18,721 cap while NI > 0 (subroutine 3801).
/// 0 in a loaded save means "seed from division" (see SaveLoadService).
/// </summary>
public int GroundCapacity { get; set; }
```

Capacity constants (per-division unimproved/improved values and the jitter fraction)
go in `TheManager.Models/Constants.cs` alongside the other tuning values, with a
helper `Constants.BaseGroundCapacity(Division division, bool improved)`.

## Step 2 — Seeding at initialisation

`InitializationService` (new-game setup and `JoinNewClub`) seeds capacity right where
`GroundImprovementService.InitialiseForDivision` is already called:

- unimproved base for the division (Divisions 3–4 count as "improved" — they have no
  upgrade option, so they get their single fixed value),
- apply the ±10% jitter with the injected `Random`,
- round to the nearest 100.

A static `GroundImprovementService.SeedCapacity(Club club, Division division, Random rng)`
keeps the logic beside the improvement code it belongs with.

## Step 3 — Attendance: `min(demand, capacity)`

`WeeklyTickService.CalculateGateAttendance`:

- Keep the whole existing formula as the **demand** value, including the cup ×1.25
  boost and the 500 floor.
- **Delete** the `18,721` special case (`divNum < 3 && GroundImprovementCost > 0`).
- Final result: `Math.Min(demand, gameState.Club.GroundCapacity)`.

Ordering: the cup boost and jitter apply to *demand*, then capacity clamps last — a
sold-out ground is sold out, boost or not. The 500 floor stays on the demand side
(every seeded capacity is far above it; the floor exists for deep-bottom-table demand,
not capacity).

Gate receipts remain `attendance × TicketPriceInPounds` in `Process` — no change.

## Step 4 — Ground improvement raises capacity

`GroundImprovementService.PurchaseImprovement`, on success (alongside `NI = 0`):

```csharp
club.GroundCapacity = ImprovedCapacityWithSameJitter(club);
```

The improved capacity re-uses the club's existing jitter rather than re-rolling:
`improvedBase × (club.GroundCapacity / (double)unimprovedBase)`, rounded to the
nearest 100 — a club with a slightly-large small ground gets a slightly-large big
ground. The success message includes the new capacity.

## Step 5 — Save/load migration

`SaveLoadService.Load`: after deserialising, if `Club.GroundCapacity == 0`, seed it —
improved (`GroundIsMaxCapacity`) clubs in Divisions 1–2 get the improved base, all
others the unimproved base, no jitter (deterministic migration, no `Random` in the
load path). Saves written after this change round-trip the value like any other
`Club` property; no format version bump.

## Step 6 — UI: report sell-outs

`PlayMatchScreen.ShowResult`, on the existing attendance line: when
`LastMatchAttendance >= Club.GroundCapacity`, append a `[bold yellow]SOLD OUT[/]`
badge:

```
  Attendance: 18,000 (SOLD OUT)   Gate: £36,000
```

`GroundImprovementScreen` (or wherever the upgrade is offered) shows current and
post-upgrade capacity so the purchase decision is informed.

## Step 7 — Tests

`TheManager.Tests` (xUnit, seeded `Random`, following `FixtureSchedulerServiceTests`
conventions):

- **Seeding**: capacity within ±10% of the division base; rounded to 100; Divisions
  3–4 get their single fixed base; deterministic for a given seed.
- **Clamping**: with capacity 5,500 and a demand scenario known to exceed it (Division
  1 club, both teams top of the table), attendance equals exactly 5,500; with a huge
  capacity, attendance equals the unclamped demand (formula regression guard).
- **Cup boost then clamp**: a cup tie whose boosted demand exceeds capacity still
  returns capacity.
- **Improvement**: purchase raises capacity to the improved value preserving jitter
  ratio; already-at-max and can't-afford paths leave capacity untouched.
- **Migration**: loading a save with `GroundCapacity == 0` seeds by division and
  improvement state; a modern save round-trips its value unchanged.
- **Receipts**: `WeeklyTickService.Process` reports `GateMoney ==
  clampedAttendance × TicketPriceInPounds` when the ground sells out.

---

## What does NOT need implementing

- **Multi-stage ground expansion** (per-stand building, incremental capacity) — the
  original has a single one-shot upgrade; keep it.
- **Price elasticity of demand** — ticket price still doesn't affect attendance,
  faithful to FOOT.BAS. (A capacity model is the prerequisite for doing this later:
  sell-outs are the natural signal to raise prices.)
- **Capacity changes on promotion/relegation** — grounds keep their size; only the
  improvement *option* re-seeds by division, as today.
- **Wembley** — fixed 80,000/100,000 neutral-venue attendance in `GameService` is
  independent of any club's ground and stays where it is.
- **Safety/segregation costs, police bill scaling** — the police bill remains the
  existing random event, not attendance-derived.

---

## Affected files

| File | Change |
|------|--------|
| `TheManager.Models/Club.cs` | new `GroundCapacity` property |
| `TheManager.Models/Constants.cs` | per-division capacity bases, jitter fraction, helper |
| `TheManager.Services/InitializationService.cs` | seed capacity at new game / new club |
| `TheManager.Services/GroundImprovementService.cs` | `SeedCapacity`; purchase raises capacity |
| `TheManager.Services/WeeklyTickService.cs` | demand clamped to capacity; delete 18,721 cap |
| `TheManager.Services/SaveLoadService.cs` | derive capacity for pre-capacity saves |
| `TheManager.Console/Screens/PlayMatchScreen.cs` | SOLD OUT badge on the attendance line |
| ground-improvement screen | show current / post-upgrade capacity |
| `TheManager.Tests/…` | seeding, clamping, improvement, migration, receipts tests |
