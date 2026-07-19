# Specification: FA Cup Competition

## Overview

The FA Cup existed in FOOT.BAS but was never ported: `FixtureSchedulerService` has its
cup-matchday sets emptied with a ToDo, and `CupService` contains a partial port of the
bracket machinery that is not called from the game loop. This spec restores the FA Cup
as a playable knockout competition with the real FA Cup's shape: an **80-team first
round** (all 48 Division Three/Four clubs + 32 non-league sides) narrowing to a
**64-team third round** when the 44 Division One/Two clubs enter, a random draw each
round, cup ties played by the player through the normal match flow, simulated results
for every other tie, and the final at Wembley on the last matchday of the season.

The original game assumed four divisions of 20 teams, all playing 38 league games, so
one shared 59-slot calendar worked for everyone. This port has **Division One with 20
teams (38 fixtures) and Divisions Two–Four with 24 teams (46 fixtures)**, so the cup
calendar must serve both. The design (per the sketched approach): **cup matchdays are
dedicated calendar slots — Division One simply has no league fixture on them, while
Divisions Two–Four use every non-cup matchday for league play** so their 46 fixtures
still complete inside the season. Division One's shorter league schedule is what buys
it the slack: 8 of its non-cup matchdays are also fixture-free rest days.

**League Cup is out of scope** — this spec deliberately ports only the FA Cup. The
calendar design below leaves no free matchdays for a second cup in Divisions Two–Four;
porting the League Cup later will need either double-fixture matchdays or the
postponement mechanic (`GameState.PostponedFixtures` — "DUE TO CUP MATCHES", BASIC
line 2293). That is a follow-up spec.

## Terminology: matchday, not week

The season calendar is a sequence of **54 matchdays**, and each `ScheduledMatch` is
one matchday. The term "week" is deliberately avoided for calendar slots: a real
football season is ~40 weeks with cup ties played midweek, so several matchdays can
fall within the same real-world week — a 54-"week" season would read as 13 months.
The game does not model real dates; the matchday is the atomic unit of time, exactly
as `CI` was in FOOT.BAS (59 ticks, more than a year of "weeks" by the same misnomer).

Practical consequences:

- **New identifiers use matchday naming**: `Constants.SeasonMatchdays`,
  `Constants.FACupMatchdays`, `FixtureSchedulerService.BuildSeasonCalendar`
  matchday loop, and all UI text ("Matchday 12", column header "MD").
- **Persisted property names keep their current spelling** for save-file
  compatibility: `GameState.CurrentWeek` and `ScheduledMatch.Week` remain, but their
  XML doc comments are updated to "matchday index (property name kept for save
  compatibility)". Renaming them would silently break every existing JSON save.
- **Non-persisted code identifiers are renamed** as part of this work:
  `FixtureSchedulerService.AdvanceWeek` → `AdvanceMatchday` (and its
  `WeekAdvanceResult` → `MatchdayAdvanceResult`).
- **The weekly tick runs once per matchday**, unchanged — wages, training, and
  transfer processing tick 54 times a season, close to the original's 59 `CI` ticks,
  so the economy pacing stays faithful. `WeeklyTickService` keeps its name (it is the
  port of the BASIC "weekly news" subroutine); renaming it is optional and out of
  scope.

## BASIC reference

- **Season cycle** (lines 422–430, 1701–1724): CI runs 1–59. FA Cup slots were
  CI = 16, 23, 30, 37, 44, 51, 58; League Cup CI = 12, 19, 26, 33, 40, 47, 54;
  European CI = 14, 21, … On a cup matchday with the club eliminated (`CR=0`),
  `AAA=1` skips straight to the weekly tick — a blank matchday. This spec reuses
  that shape.
- **Late entry** (line 1723): `IF AP<3 AND CI<19 THEN MR=3:CR=MR` — a club in
  Division One or Two enters the FA Cup at round 3. Preserved.
- **Bracket** (subroutines 1100, 1237, 1249): `L(64)` bracket slots + `Z(3..4, I)`
  paired fixtures; draw by random walk; round-3 sequential redistribution. Already
  ported in `CupService` (with bugs — see Step 2).
- **Wembley** (line 130): `IF CI=54 OR CI=58 THEN PRINT"WEMBLEY":BK%=2` — cup finals
  are at a neutral venue. The port also stages the **semi-finals** at Wembley,
  matching modern FA Cup practice.
- **Round names** (lines 1209–1212): "FA CUP ROUND n", QF, SF, Final.

**Deviation notes:**
1. The competition follows the **real FA Cup structure**, not the original's flat
   64-team bracket: Round 1 has 80 teams (48 league + 32 non-league, 40 ties),
   Round 2 has 40, and Round 3 is where the 20 survivors meet all 44 Division
   One/Two clubs — 64 teams and clean powers of two from there (64 → 32 → 16 → 8 →
   4 → 2). Eight rounds total: R1–R5, QF, SF, Final. The bracket capacity grows from
   the original `L(64)` to 80 accordingly.
2. Drawn player ties are settled **on the day by a penalty shootout** (played out
   kick by kick — see Step 3, "Penalty shootouts") instead of the original's replays.
   Replays displace league fixtures and need the postponement mechanic; they can be
   added later alongside the League Cup.
3. The original had a 16-team cup-only pool (indices 81–96, 8 drawn per season). The
   port needs **32 non-league teams**, extending `AllTeamNames` — cup-only indices
   become 93–124 (see Step 1).

---

## Step 1 — Season calendar

### `TheManager.Models/Constants.cs`

```csharp
/// <summary>Total matchdays in a season, all divisions. FA Cup Final = matchday 54.</summary>
public const int SeasonMatchdays = 54;

/// <summary>
/// FA Cup matchdays: eight rounds, evenly spaced, final on the season's last matchday.
/// R1=12, R2=18, R3=24, R4=30, R5=36, QF=42, SF=48, Final=54.
/// </summary>
public static readonly int[] FACupMatchdays = [12, 18, 24, 30, 36, 42, 48, 54];
```

54 − 8 cup matchdays = 46 league matchdays — exactly the Division Two–Four schedule.

### `TheManager.Models/GameState.cs` — team-name pool

`AllTeamNames` currently documents cup-only slots at [93–108] (16 teams) in a
`string[120]`. Thirty-two non-league teams need indices **[93–124]**, so the array
grows to `string[128]` and `TeamData.Seed` gains 16 more non-league names.
`CupService.GetDivisionForTeamIndex` already treats everything above 92 as
"division 5" — no change there.

Also update the doc comments on `GameState.CurrentWeek` and `ScheduledMatch.Week` to
say "matchday" (names kept for save compatibility — see Terminology).

### `TheManager.Models/Enums/MatchType.cs`

Add a `NoFixture` value. Today `GetCurrentMatch` returns `EndOfSeason` whenever no
fixture matches the matchday, which would silently end the season on a Division One
rest day. Every matchday 1–54 must have an explicit `ScheduledMatch`; end of season
is `CurrentWeek > Constants.SeasonMatchdays`, not "no fixture found".

### `TheManager.Models/ScheduledMatch.cs`

Add the shootout result fields:

```csharp
/// <summary>True when this cup tie was level and won/lost on penalties.</summary>
public bool WonOnPenalties { get; set; }

/// <summary>Shootout tallies. Null unless the tie went to penalties.</summary>
public int? OurPenalties   { get; set; }
public int? TheirPenalties { get; set; }
```

Without the flag the fixtures screen would render a penalties win at 1–1 as a "D" —
the result cell derives W/L/D from the scores alone; the tallies let it show
"(pens 4–3)".

### `TheManager.Services/FixtureSchedulerService.cs`

New method that wraps the existing round-robin generator and maps league fixtures onto
the 54-matchday calendar:

```csharp
public static List<ScheduledMatch> BuildSeasonCalendar(
    Division division, string clubName, string[] allTeamNames)
```

1. Generate league fixtures with the existing `GenerateSeasonFixtures` (order and
   home/away untouched).
2. Walk matchdays 1–54. Cup matchdays (`Constants.FACupMatchdays`) get a placeholder
   `ScheduledMatch { MatchType = MatchType.FACup }` — opponent filled in at draw time
   (Step 3).
3. The remaining matchdays receive league fixtures in order. For Divisions Two–Four
   that consumes all 46 slots. For Division One, 8 of the 46 non-cup slots become
   `MatchType.NoFixture` rest days, spread evenly (roughly one every six league
   matchdays; exact positions are implementation-defined but deterministic, never
   matchday 1 and never consecutive).

`AdvanceWeek` (renamed `AdvanceMatchday` — see Terminology) keeps incrementing
`CurrentWeek` every matchday but must **only increment `FixturesPlayed` / decrement
`MatchesRemainingThisSeason` when a league fixture was played** — pass the matchday's
`MatchType` in. This keeps the sacking form window (`WeeklyResults[FixturesPlayed]`)
and `LeagueService.SimulateOtherFixtures` aligned: both are league-matchday-only and
are simply not run on cup / rest days.

---

## Step 2 — Fix the half-ported `CupService`

Bugs to fix before the service can be wired up:

| Location | Problem | Fix |
|---|---|---|
| `BracketSize = 64` | Round 1 now has 80 teams | `BracketSize = 80` |
| `CupTeamPoolStart = 81`, `CupTeamPoolSize = 16` | Old 80-team layout | Cup-only pool is 93–124, 32 teams (Step 1) |
| `SetupInitialBracket` | Fills league slots from indices 41–80 and only 8 random cup-only teams | New composition: all 48 Div 3+4 teams (45–92) + all 32 non-league teams (93–124) = exactly 80, no random subset needed |
| `FindTeamInBracket` | Stub returning −1 | Implement: resolve the club's index via `AllTeamNames` |
| `DrawRound` | An odd leftover team is silently dropped | Cannot occur (every round has an even field: 80/40/64/32/16/8/4/2), but guard: leftover receives a bye into the next round |
| `SimulateFixture` | Returns `IsReplay` and leaves the bracket unfilled | Caller re-simulates drawn AI ties until decisive (abstracted replay) |

`GetDivisionForTeamIndex` already matches the new layout — no change.

### Persisting the bracket and results

`CupCompetition` (`TheManager.Models/CupCompetition.cs`) gains the bracket and a
per-round results history so both survive save/load:

```csharp
/// <summary>80-slot knockout bracket (1-based; 0 = empty). Extends L(64) in FOOT.BAS.</summary>
public int[] Bracket { get; set; } = new int[CupService.BracketSize + 1];

/// <summary>
/// Completed rounds, in order: every tie with its final score. The winners of
/// the last entry (plus the round-3 entrants, before round 3) are the teams
/// still in the competition. No FOOT.BAS equivalent — the original kept only
/// the player's own fixture log (A$).
/// </summary>
public List<CupRoundRecord> RoundHistory { get; set; } = new();
```

```csharp
/// <summary>One completed cup round: the round and all of its finished ties.</summary>
public class CupRoundRecord
{
    public CupRound Round { get; set; }
    public List<CupFixture> Results { get; set; } = new();
}
```

`CupFixture` (`TheManager.Models/CupFixture.cs`) already carries nullable
`HomeScore`/`AwayScore`; it gains the outcome fields:

```csharp
/// <summary>Winning team's name (scores can be level when decided on penalties).</summary>
public string Winner { get; set; } = string.Empty;

/// <summary>True when the tie was level and decided on penalties.</summary>
public bool WonOnPenalties { get; set; }

/// <summary>Shootout tallies (home/away). Null unless decided on penalties.</summary>
public int? HomePenalties { get; set; }
public int? AwayPenalties { get; set; }
```

(Declare the bracket-size constant on the model or duplicate it — models must not
reference Services.)

The flow on each cup matchday (Step 3) is: play/simulate every tie of the current
round → fill in the scores and winners on `CurrentRoundFixtures` → append the
completed list to `RoundHistory` as a `CupRoundRecord` → `AdvanceRound` and draw the
next round into a fresh `CurrentRoundFixtures`. The bracket remains the operational
structure the draw runs on; `RoundHistory` is the authoritative record of who is
still in the competition and feeds the cup screen (Step 5). A consistency invariant
worth a test: the non-zero bracket entries after a round always equal the winners of
the last `RoundHistory` entry (plus the 44 round-3 entrants when merging).

---

## Step 3 — Competition flow

### Season start (`InitializationService.SetupNewGame` and `SeasonService` rollover)

1. Build the 80-team round-1 bracket (Step 2 composition). Division Three/Four clubs
   — including the player's, if applicable — are all in from the start; Division
   One/Two clubs are **not**: all 44 of them enter at round 3 (BASIC line 1723
   generalised from the player's club to the whole top two divisions).
2. Draw round 1 with `DrawRound`; store fixtures in `FACup.CurrentRoundFixtures` and
   set `CurrentRound = Round1`.
3. Replace `gameState.Fixtures` with `BuildSeasonCalendar(...)` (Step 1) instead of
   the raw `GenerateSeasonFixtures` output.

`SeasonService.ResetMatchState` already resets `FACup.CurrentRound` and seeds
`RoundTracker` (2 for Divs 1–2, 3 for Divs 3–4 — the `cs` difficulty seed); extend the
rollover to rebuild the bracket and redraw round 1.

### On reaching a cup matchday (`GameService`)

Round for cup matchday *n* (0-based index into `Constants.FACupMatchdays`):
`Round1, Round2, Round3, Round4, Round5, QuarterFinal, SemiFinal, Final`.
The field each round: 80 → 40 → **64** (top-division entry) → 32 → 16 → 8 → 4 → 2.

1. **Round 3 entry:** before the round-3 draw, add all 44 Division One/Two teams to
   the bracket alongside the 20 round-2 survivors, making a 64-team field
   (`AdvanceRound` already does a sequential bracket redistribution at round 3 —
   repurpose it to perform this merge).
2. **Draw the round** (`DrawRound`) if not already drawn; show it (Step 5). If the
   player's club is in the draw, fill this matchday's placeholder `ScheduledMatch`
   with the opponent's name and home/away flag — from this moment the fixtures
   screen shows the actual tie instead of a bare "FA Cup" row.
3. **Player still in:** `FindPlayerFixture` locates the tie. Play it through the
   normal `PlayMatch` flow with `MatchType.FACup`:
   - Opponent ratings: `OpponentRatingService.Estimate(..., opponentDivision, isCupMatch: true, ...)`
     — **deviation from FOOT.BAS line 415**: the original rated cup opponents from
     the cup round counter (`cs`), identity-blind, with later rounds rolling
     *weaker*. The port rates cup opponents by the league they actually play in
     (`cx = opponentDivision − difficultyLevel`), with non-league sides treated
     as a lower-half League Two team (division 5 clamps to 4, no top-3 bonus) —
     so a Premier League draw plays like one and a giant-killing is earned.
   - Opponent morale: cup formula (75 + RND 0–23) — already implemented.
   - Cup-only opponents (indices 93–124) are not in `CurrentLeague`, so
     `FindLeaguePosition` must fall back gracefully (treat as mid-table, no top-3
     bonus).
   - **Draw after 90 minutes → penalty shootout on the day** (deviation 2), played
     out kick by kick — see "Penalty shootouts" below.
   - Winner: `RecordPlayerResult` puts the winner in the bracket and bumps
     `RoundTracker`; loser is eliminated (`CurrentRound` stays as the exit round —
     `SeasonRecord.FACupRoundReached` and `CalculateManagerRating` already consume it).
   - Write the result back onto the matchday's `ScheduledMatch` calendar entry
     (`OurScore`/`TheirScore`, plus `WonOnPenalties` and the shootout tallies) so
     the fixtures screen shows cup results exactly like league ones.
   - Post-match skill changes, morale, and injuries apply exactly as league matches;
     the league table, `WeeklyResults`, and `FixturesPlayed` are **not** touched.
4. **Player eliminated (or not yet entered):** the matchday is a rest day — no
   match, the weekly tick still runs (wages, training, transfers), mirroring BASIC
   `AAA=1`.
5. **Simulate all other ties** (`SimulateFixture`, re-rolling draws), write every
   score and winner back onto `CurrentRoundFixtures`, append the completed round to
   `RoundHistory`, then advance the round (`AdvanceRound`) and draw the next one.

### Penalty shootouts

New stateless service `PenaltyShootoutService` (`TheManager.Services`), `Random`
injected per the service pattern. It pre-computes the whole shootout as an ordered
kick list so the UI can reveal it one kick at a time (the same pre-compute-then-replay
approach `MatchEngine` uses for goals). No FOOT.BAS equivalent — new mechanic.

```csharp
public static PenaltyShootoutResult Run(
    IReadOnlyList<Player> ourTakers,   // on-pitch players in kicking order
    double          ourGoalkeeperSkill,
    OpponentRatings opponent,
    Random          rng)
```

**Rules:**

1. **Best of five**: teams alternate kicks (coin-flip who goes first), five kicks
   each. The shootout ends early once a side can no longer be caught (standard
   rule — e.g. 3–0 with two kicks left is over).
2. **Level after five → sudden death**: one kick each per round; if one team scores
   and the other misses in the same round, the match ends. Repeats until decided.
3. **Our kicking order**: the players still on the pitch at full time (excluding
   anyone sent off or subbed off injured — the match engine knows), ordered
   attackers first, goalkeeper last: squad slots 11 → 2, then 1. When every
   on-pitch player has taken one, the order restarts from the first taker, and so
   on — the goalkeeper takes a kick before anyone takes a second.
4. **Opponent takers**: the opposition has no named players (aggregate
   `OpponentRatings` only), so their kicks are labelled "<Club> — kick n".
5. **Score probability**: a kick scores with chance **70% + the taker's skill
   rating in percentage points** — a 5.0-skill player converts 75% of the time, a
   9.9 star 79.9%. Our kicks use the taker's `Skill`; opponent kicks use
   `opponent.AttackRating` (0–9) in place of skill. The goalkeeper does **not**
   affect the odds — saves are one of the cosmetic miss outcomes below.

**Outcome flavour:** each kick resolves to a `PenaltyOutcome` enum —
`Scored`, `Saved`, `Wide`, `OverBar` (on a miss, one of the three miss kinds is
rolled at random). Per project convention the enum lives with the others in
`TheManager.Models/Enums/`. The UI renders one commentary line per kick from a small message
pool keyed by outcome, e.g.:

| Outcome | Example lines |
|---|---|
| `Scored` | "KIRBY scores!", "KIRBY sends the keeper the wrong way!", "KIRBY buries it in the bottom corner!" |
| `Saved` | "The goalkeeper saves KIRBY's penalty!", "Kept out — a superb save!" |
| `Wide` | "KIRBY puts the penalty wide!", "Off the post and wide!" |
| `OverBar` | "KIRBY blazes it over the bar!" |

**Result type:** `PenaltyShootoutResult` holds the ordered `List<PenaltyKick>`
(`TakerName`, `IsOurKick`, `Outcome`, running scores, `IsSuddenDeath`) plus
`OurScore`, `TheirScore`, and `WeWon`. The final tallies are written to the
matchday's `ScheduledMatch` and the tie's `CupFixture` (Step 1/2 fields) so the
fixtures and cup screens can show "(pens 4–3)".

Only the player's tie gets a played-out shootout; drawn AI ties keep the existing
re-simulate abstraction (Step 2).

### Wembley — semi-finals and final (matchdays 48 and 54)

Both semi-final ties **and** the final are played at Wembley (BASIC line 130 put
finals there; the port extends this to semi-finals, matching modern FA Cup
practice). Neutral venue: not a home game for gate purposes — a fixed sell-out
attendance (tunable constants, suggest 80,000 for a semi-final and 100,000 for the
final) with the gate split half to each club. Winning the final sets
`CurrentRound = CupRound.Winner`; `SeasonRecord.WonFACup` and the manager-rating
bonus are already wired.

### Round mapping

The eight rounds map onto the existing `CupRound` enum with no gaps and no change:
Round1–Round5 = 1–5, QF = 6, SF = 7, Final = 8, Winner = 9.

---

## Step 4 — Finances

- **Gate receipts:** home cup ties (R1–QF) run through the existing attendance/gate
  logic with a cup attendance boost (suggest ×1.25, tunable). Away ties earn nothing,
  as in the league. Semi-finals and final: fixed Wembley gate, split 50/50.
- **Cup bonus pay** (BASIC line 2513, `os>0 → CUP BONUS PAY 11×NV`): out of scope for
  v1 — noted as a follow-up with the wage-negotiation cup-bonus field (BASIC 4105).
- No prize-money table in the original; do not invent one.

---

## Step 5 — UI (`TheManager.Console`)

All screens display "Matchday n" (or "MD") rather than "Week n" — see Terminology.

- **`WeekHubScreen`** — cup matchdays show "FA Cup Round n vs <opponent> (H/A)";
  rest days show "No fixture today"; matchday 48 shows "FA CUP SEMI FINAL — WEMBLEY"
  and matchday 54 "FA CUP FINAL — WEMBLEY" when the club is in them.
- **`PlayMatchScreen`** — handles `MatchType.FACup`; shows the round name
  (`CupCompetition.RoundName`). When the tie is level at full time the shootout is
  **part of the same match playback**, not a separate screen: the minute-by-minute
  feed reaches 90, shows "FULL TIME — <score>. The tie goes to penalties…", and
  flows straight into the shootout on the same screen. Each kick is revealed one at
  a time (key press or short delay, matching the match feed's pacing): "KIRBY steps
  up…" then its commentary line from the outcome pool ("KIRBY scores!", "KIRBY puts
  the penalty wide!", "The goalkeeper saves KIRBY's penalty!"), with the running
  tally shown between kicks ("3–2 after 4"). Entering sudden death shows a
  "SUDDEN DEATH" banner. The closing scoreboard shows the 90-minute score plus
  "(pens 4–3)".
- **New `CupScreen`** — the draw for the current round (all ties) plus the full
  round-by-round results from `RoundHistory` (who is still in, who went out, scores,
  "won on pens" markers), equivalent to the BASIC "Cup fixtures" view (lines
  1527–1548) and the "FA cup draw" menu entry (line 34). Reachable from the week hub.
- **`FixturesScreen`** — cup ties appear in the club's fixture list alongside league
  games. The screen already renders `state.Fixtures` with a Type column (the "FA"
  label exists in `MatchTypeLabel`), so calendar entries flow in automatically; the
  changes are:
  - Column header "Wk" becomes "MD" (matchday).
  - *Undrawn cup matchday*: opponent column shows the round name ("FA Cup R3 — draw
    pending") until the draw fills the placeholder in Step 3.
  - *Eliminated*: remaining cup-matchday rows collapse to a dimmed "—" opponent
    (they are rest days now).
  - *Rest / `NoFixture` days*: dimmed "No fixture" row, no H/A, no result.
  - *Result cell*: played cup ties show W/L and score like league rows, with a
    "(pens)" suffix when `ScheduledMatch.WonOnPenalties` is set — `BuildResultCell`
    must check the flag before deriving W/L/D from the scores.
  - Semi-final and final rows show venue "N" (neutral — Wembley) instead of H/A.
    No model field needed: neutral is derived from `MatchType.FACup` plus the
    matchday being one of the last two entries in `Constants.FACupMatchdays`.

---

## Step 6 — Save compatibility & club changes

- New properties (`CupCompetition.Bracket`, calendar `ScheduledMatch` entries with the
  new `MatchType` values) serialise automatically. Persisted matchday counters keep
  their existing JSON names (`CurrentWeek`, `Week`) — see Terminology.
- **Legacy saves** (no bracket, 38/46-matchday fixture list): on load, if
  `FACup.Bracket` is all-zero and `CurrentWeek` ≤ `SeasonMatchdays`, rebuild the
  bracket, fast-forward it to the round implied by `CurrentWeek` (simulate skipped
  rounds with the player kept alive — each simulated round is appended to
  `RoundHistory`, so the cup screen is complete even on a migrated save), and rebuild
  the remaining calendar with
  `BuildSeasonCalendar(...).Where(m => m.Week >= CurrentWeek)` merged over the
  already-played matchdays. Simpler alternative if that proves fiddly: legacy saves
  sit the cup out for the season in progress (`CurrentRound = NotEntered`) and join
  from the next season — acceptable, call it out in the PR.
- **Mid-season club change** (`JoinNewClub`): cup state resets to eliminated, exactly
  as the original ("Reset cup state" — BASIC subroutine 5555). The new club's calendar
  is rebuilt for the remaining matchdays; its cup matchdays are rest days.

---

## Worked example

Managing a Division Three club, new season:

1. Matchday 1: calendar built — 46 league fixtures on non-cup matchdays, cup
   placeholders at matchdays 12/18/24/30/36/42/48/54. The 80-team round-1 bracket
   holds all 48 Div 3+4 clubs and 32 non-league sides; our club is drawn at home to
   non-league "Bogthorpe".
2. Matchday 12 (R1): we beat Bogthorpe 3–0 through the normal match screen (cup
   morale/rating formulas, no league table change, `FixturesPlayed` unchanged). The
   other 39 ties are simulated; 40 teams survive.
3. Matchday 18 (R2): drawn away, 1–1 → penalty shootout, kick by kick. Our first
   five takers are slots 11, 10, 9, 8, 7 (attackers first); 4–4 after five each →
   sudden death; their sixth kick is saved, our sixth (slot 6) scores — we win 5–4
   on pens; 20 survive. Matchday 24
   (R3): all 44 Division One/Two clubs join the field (64 teams); we draw a Division
   One side away and lose 0–2. `FACup.CurrentRound` stays `Round3`.
4. Matchdays 30/36/42/48/54 are now rest days — wages and training tick on, no
   match. The remaining rounds and the matchday-54 final are simulated and reported
   in the news.
5. Season ends after matchday 54: `FACupRoundReached = 3` feeds the manager rating
   and season history. Meanwhile a Division One club's season had 38 league
   matchdays, at most 6 cup ties (R3 onward), and rest days filling the remainder of
   the same 54-matchday calendar.

---

## What does NOT need implementing

- **League Cup** — separate follow-up spec (needs double-fixture matchdays or
  postponements).
- **Replays** — abstracted to extra time + penalties (deviation 2); revisit with the
  postponement mechanic.
- **Real-date calendar** — matchdays remain abstract slots; mapping them onto actual
  weeks/dates (midweek vs weekend) is cosmetic and out of scope.
- **European qualification hook** — `GameState.European` and the Cup Winners' Cup
  (BASIC line 2721) exist as models; wiring FA Cup winners into Europe is a separate
  feature.
- **Cup bonus wages** — deferred (Step 4).
- **Full multi-division league simulation** — other divisions still aren't simulated
  matchday-by-matchday; the bracket is player-centric plausibility, as in the
  original.
- **`SeasonRecord` / manager rating changes** — `FACupRoundReached`, `WonFACup`, and
  the rating formula are already implemented and just start receiving real values.

---

## Affected files

| File | Change |
|---|---|
| `TheManager.Models/Constants.cs` | `SeasonMatchdays = 54`, `FACupMatchdays` array (8 matchdays) |
| `TheManager.Models/GameState.cs` | `AllTeamNames` grows to `string[128]`; cup-only doc becomes [93–124]; `CurrentWeek` doc comment → matchday |
| `TheManager.Models/Enums/MatchType.cs` | Add `NoFixture` |
| `TheManager.Models/ScheduledMatch.cs` | Add `WonOnPenalties`, `OurPenalties`, `TheirPenalties`; `Week` doc comment → matchday |
| `TheManager.Models/CupCompetition.cs` | Add persisted `Bracket` (80 slots) and `RoundHistory`; new `CupRoundRecord` class |
| `TheManager.Models/CupFixture.cs` | Add `Winner`, `WonOnPenalties`, `HomePenalties`, `AwayPenalties` |
| `TheManager.Models/TeamData.cs` | Seed 16 additional non-league team names (32 total, indices 93–124) |
| `TheManager.Services/FixtureSchedulerService.cs` | `BuildSeasonCalendar`; `AdvanceWeek` → `AdvanceMatchday`, only counts league fixtures; delete the emptied cup-matchday ToDo sets |
| `TheManager.Services/CupService.cs` | `BracketSize = 80`; pool constants (93/32); round-1 composition (45–92 + 93–124); round-3 top-division merge; `FindTeamInBracket`; bye guard |
| `TheManager.Services/PenaltyShootoutService.cs` | New: pre-computed kick-by-kick shootout (`Run`, `PenaltyShootoutResult`, `PenaltyKick`, `PenaltyOutcome`) |
| `TheManager.Services/GameService.cs` | Cup-matchday branch in `PlayMatch`; rest days; penalties resolution; simulate-others + `AdvanceRound`; end-of-season = `CurrentWeek > SeasonMatchdays` |
| `TheManager.Services/InitializationService.cs` | Use `BuildSeasonCalendar`; keep existing bracket/draw calls (now on the 80-team bracket) |
| `TheManager.Services/SeasonService.cs` | Rollover rebuilds bracket + calendar and clears `RoundHistory` |
| `TheManager.Services/SaveLoadService.cs` | Legacy-save migration (Step 6) |
| `TheManager.Console/Screens/*` | `WeekHubScreen`, `PlayMatchScreen`, `FixturesScreen` updates ("Matchday" labels, "MD" column); new `CupScreen` |
| `TheManager.Tests/` | Calendar shape per division (46 league + 8 cup / 38 league + 8 rest + 8 cup, cup matchdays fixed); round-1 composition = 80; round-3 merge = 64; field sizes 80/40/64/32/16/8/4/2; `FixturesPlayed` frozen on cup matchdays; elimination → rest days; legacy-save migration; shootout: alternation, early end when uncatchable, sudden-death termination, kicking order 11→2 then GK then cycling, 70%+skill scoring probability |
