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
  seeded from the club's **real-world stadium capacity** (see the appendix) at
  new-game/new-club time, with a per-division fallback for club names not in the
  table.
- **Attendance is an occupancy model in every division**: the week's figure is a
  random draw from a small band set by league position, as a fraction of capacity.
  Division One slides from 98–100% at 1st to 88.5–90.5% at 20th; Divisions
  Two–Four slide from 98–100% at 1st to 60–62% at 24th — **lower-league
  attendance never drops below 60% of capacity**, however bad the season. Gate
  receipts stay `attendance × TicketPriceInPounds`, so the finance pipeline is
  untouched. The old BASIC demand formula survives only as a fallback for
  unseeded legacy states (`GroundCapacity == 0`).
- **Capacity is fixed** — there is no way to increase it in this spec. The magic
  18,721 cap is deleted, and the existing ground improvement purchase is left
  untouched (see "What does NOT need implementing" for the consequence).
- **Sell-outs are detected and reported** ("SOLD OUT") in the post-match screen.
  A sell-out is **attendance ≥ 99% of capacity** — away fans don't always fill
  their allocation, so a nominally full house rarely hits the exact number.
- **The ground gets a name**, shown with its capacity on the manager profile
  screen ("Ground: Old Trafford (74,300)").
- **Wembley is unaffected** — semi-finals and the final keep their fixed 80,000 /
  100,000 neutral-venue attendance handled in `GameService`.

---

## BASIC reference

- Subroutine **3801** (`FOOT.BAS:3197–3229`) — the attendance/demand formula ported in
  `WeeklyTickService.CalculateGateAttendance`. **Replaced by the occupancy model in
  all divisions** (deviation — the original had no capacity to be a fraction of);
  the ported formula remains in the method only as the fallback when
  `GroundCapacity == 0` (a legacy state that never seeded a ground).
- The **18,721** cap inside that subroutine (applied when `NI > 0` in Divisions 1–2) is
  the original's only nod to capacity. It is *replaced* by `GroundCapacity`, not kept
  alongside it.
- Subroutines **4201–4206** (`FOOT.BAS:3623–3642`) — the one-time ground improvement
  purchase (`NI = INT(1,500,000 / Division)`, cleared to 0 on purchase), ported in
  `GroundImprovementService`. **Not touched by this spec.**

The original has no per-club capacity variable; everything here that isn't the demand
formula is new behaviour.

---

## Design decisions

- **Attendance is occupancy-driven in every division.** The ground, not an
  abstract demand curve, is the variable: each league position sets a narrow
  2-point occupancy band and the week's figure is drawn randomly within it. The
  band ceiling slides linearly from 100% at 1st to a per-division floor at the
  bottom — 90.5% in Division One (a struggling top-flight club still fills
  88.5–90.5%), 62% in Divisions Two–Four (a bottom-place club still fills
  60–62%; **attendance never drops below 60% of capacity**). A deliberate side
  effect: the *visiting* club no longer affects the gate (the old formula docked
  the crowd for a lowly opponent) — season-ticket reality is that the ground
  fills for whoever visits. Economic pressure in the lower leagues now comes
  from the *size* of the ground rather than from empty seats.
- **Capacity is seeded from the real ground** of the club being managed (appendix
  table, keyed by `TeamData` name). Only *our* club's capacity ever matters: away
  games earn no gate, and AI-vs-AI cup ties are resolved without attendance — so
  the 92 league clubs are the whole data set, and the 32 cup-only non-league
  entrants need no capacity at all.
- **Fallback for unknown names**: a club name with no table entry (e.g. a
  player-typed club like "TESTFC") gets a per-division base with a ±10% jitter
  rolled once at initialisation and persisted — it never re-rolls.
- **Capacity is a fixed fact about the club.** No purchase, event, or season
  rollover changes it. Expansion (in any form) is deferred — this spec only makes
  the ground's size real.
- **Capacity follows the club, not the manager.** On sacking/`JoinNewClub`, the new
  club's capacity is seeded fresh for its name/division; on promotion/relegation the
  ground does *not* change size (a promoted Division 2 club plays Division 1
  football in a Division 2 ground — which is exactly when sell-outs bite).
- **Save compatibility by derivation**: `GroundCapacity == 0` on a loaded save means
  "pre-capacity save" — derive it from the club's name (or division fallback) at
  load time rather than versioning the save format.

### Tunable constants (suggested values)

Primary seeding is the real-capacity table in the appendix. The fallback for club
names not in the table:

| Division | Fallback capacity |
|----------|------------------:|
| 1        | 30,000            |
| 2        | 22,000            |
| 3        | 12,000            |
| 4        | 8,000             |

Plus, for fallback clubs only, a per-club jitter of **±10%**, rolled once at seed
time (`capacity = base × (0.90 + rng.NextDouble() × 0.20)`, rounded to the nearest
100). The fallback values sit near each division's real-world median so an unknown
club behaves like a typical peer. Occupancy: ceiling **100%** at 1st in every
division, sliding linearly to a bottom-place ceiling of **90.5%** in Division One
and **62%** in Divisions Two–Four, with the week's occupancy drawn uniformly from
a **2-point band** below the ceiling; +2% for a home cup tie (capped at 100%).
The fallback bases, jitter fraction, occupancy ceiling/floors, and band width live
in `Constants`; the real-capacity table lives beside the names it keys on in
`TeamData`.

---

## Step 1 — Model: `Club.GroundCapacity`

`TheManager.Models/Club.cs`, in the existing `// ── Ground ──` section:

```csharp
/// <summary>
/// Ground capacity in spectators. New — no BASIC equivalent; the original's
/// only capacity notion was a hard 18,721 cap while NI > 0 (subroutine 3801).
/// 0 in a loaded save means "seed from club name / division" (see SaveLoadService).
/// </summary>
public int GroundCapacity { get; set; }

/// <summary>
/// Ground name (e.g. "Old Trafford"). New — no BASIC equivalent. Seeded with
/// the capacity; "" in a loaded save means "seed from club name / division".
/// </summary>
public string GroundName { get; set; } = "";
```

The real-ground lookup goes in `TheManager.Models/TeamData.cs` (a
`Dictionary<string, (string GroundName, int Capacity)>` keyed by the exact `Names`
entries, exposed as
`TeamData.TryGetGround(string clubName, out string groundName, out int capacity)`),
so the data lives beside the names it must stay in sync with. The tuning values
(fallback bases, jitter fraction, occupancy ceiling/step/band, cup bump, sell-out
fraction) go in `TheManager.Models/Constants.cs` with a helper
`Constants.FallbackGroundCapacity(Division division)`.

## Step 2 — Seeding at initialisation

`InitializationService` (new-game setup and `JoinNewClub`) seeds capacity as part of
the existing club setup:

- look the club's name up in the appendix table — a hit sets both `GroundName` and
  `GroundCapacity` as-is (no jitter; the real numbers *are* the flavour),
- on a miss, take the division fallback base, apply the ±10% jitter with the
  injected `Random`, round to the nearest 100, and name the ground
  `"<Club> Stadium"` (e.g. "TESTFC Stadium").

A private static `SeedGround(Club club, Division division, Random rng)` in
`InitializationService` covers both call sites.

## Step 3 — Attendance: occupancy bands in every division

`WeeklyTickService.CalculateGateAttendance`:

- **Delete** the `18,721` special case (`divNum < 3 && GroundImprovementCost > 0`)
  and the `if (divNum == 1) dn += dn / 3` bonus.
- **All divisions (home game, seeded capacity)**: each league position has a
  **2-point occupancy band**; the ceiling slides linearly from 100% at 1st to a
  per-division bottom, and the week's occupancy is drawn uniformly from the band:

  | Position       | Division One | Divisions Two–Four (24 teams) |
  |---------------:|-------------:|------------------------------:|
  | 1st            | 98.0–100%    | 98.0–100%                     |
  | 2nd            | 97.5–99.5%   | ~96.3–98.3%                   |
  | mid-table      | 93.5–95.5% (10th) | ~79.8–81.8% (12th)       |
  | bottom         | 88.5–90.5% (20th) | 60.0–62.0% (24th)        |

  ```csharp
  double bottom     = Constants.OccupancyCeilingAtBottom(division);   // 0.905 Div 1, 0.62 below
  double ceiling    = 1.00 - (1.00 - bottom) * (ourPos - 1) / (teamCount - 1.0);
  double occupancy  = ceiling - rng.NextDouble() * 0.02;              // uniform in the 2-point band
  int    attendance = Math.Min(capacity, (int)(capacity * occupancy));
  ```

  **Lower-league attendance never drops below 60% of capacity**, however bad the
  season. The in-band draw is the week-to-week variation (no separate jitter
  needed); a home cup tie adds a flat +2% occupancy (capped at 100%) in every
  division — the old ×1.25 demand boost is gone with the demand model.
- **Legacy fallback** (`GroundCapacity == 0`, a state that never seeded a ground):
  the original BASIC demand formula runs unchanged, uncapped, exactly as before
  this spec.

Gate receipts remain `attendance × TicketPriceInPounds` in `Process` — no change.
The occupancy ceiling (100%), per-division bottom ceilings (90.5% / 62%), band
width (2 points), cup bump (+2%), and sell-out fraction (99%) live in `Constants`
beside the fallback capacities.

## Step 4 — Save/load migration

`SaveLoadService.Load`: after deserialising, if `Club.GroundCapacity == 0` (or
`GroundName` is empty), seed both — real-ground table lookup by club name first;
on a miss the division fallback base with no jitter plus the `"<Club> Stadium"`
name (deterministic migration, no `Random` in the load path). This means a
migrated custom club gets the exact fallback base while a fresh game with the
same name would roll ±10% jitter — intended: determinism on load matters more
than matching the new-game distribution. Saves written after this change
round-trip both values like any other `Club` property; no format version bump.

## Step 5 — UI: sell-outs and the manager profile

`PlayMatchScreen.ShowResult`, on the existing attendance line: when
`LastMatchAttendance >= Club.GroundCapacity * Constants.SellOutFraction` (0.99),
append a `[bold yellow]SOLD OUT[/]` badge:

```
  Attendance: 17,850 (SOLD OUT)   Gate: £35,700
```

The 99% threshold exists because away fans don't always fill their allocation: a
"full house" rarely hits the exact capacity figure. With occupancy bands in every
division, a sell-out is an earned event everywhere — only the top few places'
bands cross 99% (or a high-placed club's home cup tie, where the +2% bump caps
occupancy at 100%).

`ManagerProfileScreen.Show`, in the "This Season" block directly under the
`Club :` line, a ground line with name and capacity:

```
    Club      : Manchester United  (Division One)
    Ground    : Old Trafford  (capacity 74,300)
```

## Step 6 — Tests

`TheManager.Tests` (xUnit, seeded `Random`, following `FixtureSchedulerServiceTests`
conventions):

- **Seeding**: a club in the appendix table (e.g. Arsenal) gets exactly its real
  ground name and capacity; an unknown name gets the division fallback within ±10%,
  rounded to 100, named "&lt;Club&gt; Stadium", deterministic for a given seed; every
  `TeamData` league club (indices 1–92) has a table entry (guard against the list
  and table drifting apart).
- **Division One occupancy**: over repeated seeded draws, a 1st-place club's
  attendance always lands in 98–100% of capacity, a 10th-place club's in
  93.5–95.5% (pins the linear slide, not just the endpoints), and a 20th-place
  club's in 88.5–90.5%, never exceeding capacity; different seeds at the same
  position give different figures (the band is really random); a home cup tie at
  the top fills the ground exactly (+2% always caps).
- **Lower-division occupancy**: 1st lands in 98–100%, mid-table (12th) in
  ~79.8–81.8%, bottom (24th) in 60–62%; a seed sweep at the bottom proves
  attendance **never drops below 60% of capacity**; a home cup tie bumps the
  bottom band to 62–64%.
- **Sell-out threshold**: attendance at exactly 99% of capacity qualifies as sold
  out; 98.9% does not.
- **Receipts**: gate money equals attendance × ticket price and matches the
  weekly finance report's gate line.
- **Immutability**: buying the ground improvement leaves `GroundCapacity` unchanged.
- **Migration**: loading a save with `GroundCapacity == 0` seeds by club name (or
  division fallback); a modern save round-trips its value unchanged.

---

## What does NOT need implementing

- **Ground expansion of any kind** — no purchase, event, or upgrade changes
  `GroundCapacity`. Note the consequence: with the 18,721 cap deleted, the existing
  `GroundImprovementService` purchase no longer has *any* attendance effect — it
  stays in the game as-is (menu, cost, one-shot flag) but is effectively cosmetic.
  Tying it to a real capacity increase is the natural follow-up spec.
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
| `TheManager.Models/Club.cs` | new `GroundCapacity` + `GroundName` properties |
| `TheManager.Models/TeamData.cs` | real ground name/capacity table + `TryGetGround` |
| `TheManager.Models/Constants.cs` | fallback bases, jitter fraction, occupancy ceiling/step/band, cup bump, sell-out fraction |
| `TheManager.Services/InitializationService.cs` | `SeedGround` at new game / new club |
| `TheManager.Services/WeeklyTickService.cs` | occupancy bands in all divisions (60% floor below Div 1); demand formula kept only as legacy fallback; delete 18,721 cap and Div 1 bonus |
| `TheManager.Services/SaveLoadService.cs` | derive capacity for pre-capacity saves |
| `TheManager.Console/Screens/PlayMatchScreen.cs` | SOLD OUT badge on the attendance line |
| `TheManager.Console/Screens/ManagerProfileScreen.cs` | ground name + capacity in "This Season" |
| `TheManager.Tests/…` | seeding, clamping, immutability, migration, receipts tests |

---

## Appendix — real-world ground capacities

Approximate real capacities (circa the 2025–26 season) for the 92 league clubs in
`TeamData`, keyed by the exact `Names` strings. Figures are rounded and intended as
game data, not an authoritative record — tune freely. The 32 cup-only non-league
entrants are deliberately absent: only the managed club's ground is ever used
(away and simulated ties have no gate), and non-league sides cannot be managed.

### Division One (indices 1–20)

| Club | Ground | Capacity |
|------|--------|---------:|
| AFC Bournemouth | Vitality Stadium | 11,300 |
| Arsenal | Emirates Stadium | 60,700 |
| Aston Villa | Villa Park | 42,900 |
| Brentford | Gtech Community Stadium | 17,250 |
| Brighton & Hove Albion | Amex Stadium | 31,900 |
| Chelsea | Stamford Bridge | 40,300 |
| Coventry City | CBS Arena | 32,600 |
| Crystal Palace | Selhurst Park | 25,500 |
| Everton | Hill Dickinson Stadium | 52,900 |
| Fulham | Craven Cottage | 29,600 |
| Hull City | MKM Stadium | 25,400 |
| Ipswich Town | Portman Road | 29,700 |
| Leeds United | Elland Road | 37,600 |
| Liverpool | Anfield | 61,300 |
| Manchester City | Etihad Stadium | 53,400 |
| Manchester United | Old Trafford | 74,300 |
| Newcastle United | St James' Park | 52,300 |
| Nottingham Forest | City Ground | 30,400 |
| Sunderland | Stadium of Light | 49,000 |
| Tottenham Hotspur | Tottenham Hotspur Stadium | 62,850 |

### Division Two (indices 21–44)

| Club | Ground | Capacity |
|------|--------|---------:|
| Birmingham City | St Andrew's | 29,400 |
| Blackburn Rovers | Ewood Park | 31,400 |
| Bolton Wanderers | Toughsheet Community Stadium | 28,700 |
| Bristol City | Ashton Gate | 27,000 |
| Burnley | Turf Moor | 21,900 |
| Cardiff City | Cardiff City Stadium | 33,300 |
| Charlton Athletic | The Valley | 27,100 |
| Derby County | Pride Park | 33,000 |
| Lincoln City | LNER Stadium (Sincil Bank) | 10,700 |
| Middlesbrough | Riverside Stadium | 34,700 |
| Millwall | The Den | 20,100 |
| Norwich City | Carrow Road | 27,200 |
| Portsmouth | Fratton Park | 21,000 |
| Preston North End | Deepdale | 23,400 |
| Queens Park Rangers | Loftus Road | 18,400 |
| Sheffield United | Bramall Lane | 32,100 |
| Southampton | St Mary's Stadium | 32,400 |
| Stoke City | bet365 Stadium | 30,100 |
| Swansea City | Swansea.com Stadium | 21,100 |
| Watford | Vicarage Road | 22,200 |
| West Bromwich Albion | The Hawthorns | 26,800 |
| West Ham United | London Stadium | 62,500 |
| Wolverhampton Wanderers | Molineux | 31,750 |
| Wrexham | Racecourse Ground | 13,300 |

### Division Three (indices 45–68)

| Club | Ground | Capacity |
|------|--------|---------:|
| AFC Wimbledon | Cherry Red Records Stadium (Plough Lane) | 9,200 |
| Barnsley | Oakwell | 23,300 |
| Blackpool | Bloomfield Road | 16,600 |
| Bradford City | Valley Parade | 25,100 |
| Bromley | Hayes Lane | 5,000 |
| Burton Albion | Pirelli Stadium | 6,900 |
| Cambridge United | Abbey Stadium | 8,100 |
| Doncaster Rovers | Eco-Power Stadium | 15,200 |
| Huddersfield Town | John Smith's Stadium | 24,100 |
| Leicester City | King Power Stadium | 32,300 |
| Leyton Orient | Brisbane Road | 9,300 |
| Luton Town | Kenilworth Road | 11,500 |
| Mansfield Town | Field Mill | 9,200 |
| MK Dons | Stadium MK | 30,500 |
| Notts County | Meadow Lane | 19,800 |
| Oxford United | Kassam Stadium | 12,500 |
| Peterborough United | Weston Homes Stadium (London Road) | 15,300 |
| Plymouth Argyle | Home Park | 17,900 |
| Reading | Select Car Leasing Stadium | 24,200 |
| Sheffield Wednesday | Hillsborough | 39,700 |
| Stevenage | Lamex Stadium (Broadhall Way) | 7,800 |
| Stockport County | Edgeley Park | 13,300 |
| Wigan Athletic | Brick Community Stadium | 25,100 |
| Wycombe Wanderers | Adams Park | 10,100 |

### Division Four (indices 69–92)

| Club | Ground | Capacity |
|------|--------|---------:|
| Accrington Stanley | Wham Stadium (Crown Ground) | 5,450 |
| Barnet | The Hive | 6,500 |
| Bristol Rovers | Memorial Stadium | 9,800 |
| Cheltenham Town | Whaddon Road | 7,100 |
| Chesterfield | Technique Stadium | 10,500 |
| Colchester United | JobServe Community Stadium | 10,100 |
| Crawley Town | Broadfield Stadium | 6,000 |
| Crewe Alexandra | Gresty Road | 10,150 |
| Exeter City | St James Park | 8,700 |
| Fleetwood Town | Highbury Stadium | 5,300 |
| Gillingham | Priestfield Stadium | 11,600 |
| Grimsby Town | Blundell Park | 9,100 |
| Newport County | Rodney Parade | 8,700 |
| Northampton Town | Sixfields Stadium | 7,800 |
| Oldham Athletic | Boundary Park | 13,500 |
| Port Vale | Vale Park | 15,000 |
| Rochdale | Crown Oil Arena (Spotland) | 10,200 |
| Rotherham United | New York Stadium | 12,000 |
| Salford City | Peninsula Stadium (Moor Lane) | 5,100 |
| Shrewsbury Town | Montgomery Waters Meadow | 9,900 |
| Swindon Town | County Ground | 15,700 |
| Tranmere Rovers | Prenton Park | 16,600 |
| Walsall | Bescot Stadium | 11,300 |
| York City | LNER Community Stadium | 8,500 |

### How the numbers interact with the attendance model

Every division runs on occupancy bands, so the ground *is* the gate. In Division
One, mid-table Old Trafford draws ~69,500–71,000 while a title-chasing Bournemouth
still only fits 11,300. In the lower leagues the 60% floor keeps even a doomed
season financially alive: bottom-place Sheffield Wednesday still draw
~23,800–24,600 into Hillsborough (60–62% of 39,700), while a promotion push fills
98–100% of any ground from Bromley's 5,000 to Stadium MK's 30,500. Wembley's
80,000/100,000 stays fixed and separate. Clubs with small grounds are capped
hard — capacity is deliberately fixed in this spec, so that pressure is simply
part of managing a small club (a future expansion spec could relieve it).
