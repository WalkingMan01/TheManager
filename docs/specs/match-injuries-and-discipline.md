# Specification: In-Match Injuries, Red Cards & Yellow Cards

## Overview

`MatchEngineService.ResolveIncident` already ports the original's injury/red-card dice
roll faithfully, but it is purely a query — it never mutates the squad, and nothing in
the live game (`GameService.PlayMatch`) calls it at all. The only callers are the legacy
`TheManager\ConsoleGame.cs` and the `TheManager.MatchHarness` WinForms tool, and even
there the result is cosmetic: a message is printed and the match continues exactly as
pre-computed, as if nothing happened.

This spec wires injuries and red cards into the live game with real consequences:

- **Injury** → the substitute (slot 12) comes on in the injured player's place (once per
  match, matching the original's one-substitution limit). The injured player is parked
  in a reserve slot, unavailable for `WeeksInjured` weeks.
- **Red card** → the player is removed from the lineup with no replacement. The team
  plays the rest of the match a player down, and that player is excluded from the team
  ratings used to simulate the remainder of the match.
- **Yellow card** *(new — no BASIC equivalent)* → a reasonable per-match chance of any
  starting outfield/GK player being booked. No immediate playing effect, but cards
  accumulate across the season.
- **Suspensions**: a red card now suspends for **3 matches** (the original used a flat
  2-week ban via `u(FL)=2`); **5 accumulated yellow cards** in a season suspends for
  **1 match**, then the count resets to 0.
- **Live notification**: yellow cards, red cards, and injuries are all announced during
  the minute-by-minute match playback on `PlayMatchScreen` — the same animated timeline
  that already reveals goals as they happen (Step 7).
- **Weekly news notification**: whenever a suspension is newly imposed (a red card, or a
  5th yellow card), it is additionally announced in the post-match weekly news block —
  the same area that already reports scout findings and contract expiries — so the
  manager sees it even if they weren't watching the live minute-by-minute feed (Step 8).

---

## BASIC reference

The original's in-match incident logic (`FOOT.BAS:3795-3868`):

- `4651-4656`: picks a random slot `FL` (1–18, restricted to first-team 1–12) and a
  red-card-vs-injury roll. Requires a free reserve slot (13–20) found via the
  `4654-4655` loop — if none exists, the incident is dropped entirely (`JS=1:RETURN`).
- `4657-4661` (**injury**): sets `u(FL)` (weeks out, physio-adjusted) and `J(FL)=35`.
  `GOSUB 652` (subroutine `652-653`, a full two-slot swap of every player field) swaps
  the substitute (slot 12) into the injured player's slot, then swaps the now-displaced
  injured player from slot 12 into the free reserve slot. This only happens if `MP`
  (one-substitution-per-match flag) is not already set; otherwise the team simply plays
  with ten men (`"TEAM HAVE TEN MEN"`).
- `4663` (**red card**): sets `u(FL)=2`, `J(FL)=83` (suspended), swaps the sent-off
  player directly into the free reserve slot (no substitute comes on).
- After either branch (if before minute 81): `GOSUB 332` recomputes the team's
  defence/mid/attack ratings (`bc`/`bb`/`bd`) from the *current* squad slots 2–11, and
  `GOSUB 4509` re-rolls the shot/goal model for the remainder of the match using those
  updated ratings — a player lost to injury or a red card can change the rest of the
  match's odds, not just the scoresheet.
- `892` (lines 669-676) computes `a3` = count of occupied reserve slots (13–20). Used
  elsewhere (`3217`, the extra-training injury path) to force a retirement when the bench
  is already full — not directly part of the in-match incident, but the same
  "no free reserve slot" guard appears here too.

The original has no concept of a yellow card.

---

## Design decisions for the new behaviour

- **Suspension is tracked in matches, not calendar weeks.** The game already advances
  exactly one match per `WeeklyTickService.Process` call (bye weeks aren't a concept
  here), so decrementing once per weekly tick is equivalent to once per match — the same
  convention already used for `ContractWeeks`.
- **Yellow cards are an independent, new roll**, separate from the existing single
  red-card/injury incident roll. A "reasonable" chance is one independent check per
  starting slot (1–12) per match, tuned so a team picks up roughly 0–2 cards per match on
  average — see Step 4 for the exact constant.
- **A second yellow card in the same match for an already-booked player is not modelled
  as a red card.** The original game has no second-yellow-equals-red rule, and adding one
  is out of scope here — see "What does NOT need implementing".
- **Squad mutation now happens inside `MatchEngineService`**, consistent with
  `RecordOurGoal`/`RecordOpponentGoal` already mutating the squad they're given.
- **Goal events for the remainder of the match are re-rolled, not discarded outright**,
  after a red card or unreplaced injury — this is the faithful translation of "excluded
  from subsequent match calculations" given this engine's pre-computed-event-list
  architecture (see Step 3).

---

## Step 1 — `Player` model additions

### `TheManager.Models/Player.cs`

Add three new fields alongside the existing contract fields:

```csharp
/// <summary>Weeks remaining before an injured player is available again. Corresponds to u(I) in FOOT.BAS.</summary>
public int WeeksInjured { get; set; }

/// <summary>Matches remaining of a suspension (red card or accumulated yellow cards).</summary>
public int SuspensionMatchesRemaining { get; set; }

/// <summary>Yellow cards picked up this season. Resets to 0 at season start and whenever it reaches 5 (triggering a suspension).</summary>
public int YellowCardsThisSeason { get; set; }
```

Add a derived helper next to `IsTransferListed`/`IsStar`:

```csharp
/// <summary>True if the player is fit and not serving a suspension.</summary>
public bool IsAvailable => WeeksInjured == 0 && SuspensionMatchesRemaining == 0;
```

---

## Step 2 — Squad mutation on injury / red card

### `TheManager.Services/MatchEngineService.cs` — `ResolveIncident`

Change the signature to take a `ref bool substitutionUsed` (tracks the one-sub-per-match
limit across calls within a single match — mirrors BASIC's `MP` flag) and to actually
mutate `squad`:

```csharp
public IncidentResult? ResolveIncident(
    Player?[] squad,
    bool      incidentBeforeMinute81,
    ref bool  substitutionUsed,
    int       physioSkillPercent = 0)
```

Keep the existing slot pick, eligibility checks, and free-reserve-slot scan unchanged
(`playerSlot`, `redCardRoll`, the minute-81 suppression, and the "no free reserve slot →
return null" guard all stay as they are today).

**Red card branch** — after building the `IncidentResult`, before returning it:

```csharp
player.SuspensionMatchesRemaining = 3;
squad[freeReserveSlot] = player;
squad[playerSlot]      = null;   // slot now empty for the rest of the match
```

**Injury branch** — after computing `injuryWeeks`:

```csharp
player.WeeksInjured = injuryWeeks;

if (!substitutionUsed)
{
    var sub = squad[GoalkeeperSlot + 11];   // slot 12
    squad[playerSlot] = sub;                // substitute takes the injured player's place
    squad[freeReserveSlot] = player;        // injured player parked in reserves
    substitutionUsed = true;
}
else
{
    squad[freeReserveSlot] = player;
    squad[playerSlot] = null;               // no sub left — team plays a player short
}
```

(`freeReserveSlot` is the index found by the existing bench-scan loop — capture it in a
local variable instead of discarding it once a free slot is confirmed to exist.)

Both branches leave `IncidentResult` unchanged in shape (`Type`, `PlayerSlot`,
`PlayerName`, `WeeksOut`) — callers that only display a message continue to work
unmodified.

---

## Step 3 — Re-roll the remaining match after personnel changes

Red cards and un-replaced injuries change the team's ratings, which must feed back into
the goal simulation for the rest of the match (mirrors `GOSUB 332` + `GOSUB 4509`).
Injuries with a substitute available also change ratings (the sub's skill differs from
the original starter's) but the player count stays at 11, so the recompute path is the
same either way — only ratings change, not headcount logic.

### `TheManager.Services/MatchEngineService.cs` — extract a windowed goal-generation helper

Refactor the goal/shot generation block inside `SetupMatch` (the "Our shot count" /
"Opponent shot count" / "Convert shots into goals" / "Assign goal minutes" sections) into
a private helper:

```csharp
private (List<GoalEvent> goals, int ourGoals, int opponentGoals) GenerateGoalsForWindow(
    MatchSetupInput input, int windowStartMinute, int windowEndMinute)
```

`SetupMatch` calls this once for the full match window (`2, matchLength`), unchanged in
behaviour. A new method reuses it for the post-incident window:

```csharp
/// <summary>
/// Re-rolls the goal model for the remainder of a match after a red card or
/// unreplaced injury has changed the team's ratings. Mirrors BASIC subroutine
/// 4509 (lines 3756-3793), called after 332 recomputes ratings.
/// </summary>
public (List<GoalEvent> goals, int ourGoals, int opponentGoals) ContinueMatchAfterIncident(
    MatchSetupInput updatedInput, int fromMinute, int matchLength)
    => GenerateGoalsForWindow(updatedInput, fromMinute, matchLength);
```

### `TheManager.Services/GameService.cs` — `PlayMatch`

After `var sim = _engine.SetupMatch(matchInput);` and before iterating `sim.GoalEvents`:

```csharp
bool substitutionUsed = false;
var  matchIncidents   = new List<MatchIncident>();   // Step 7 — live-feed notifications
var  newSuspensions   = new List<SuspensionNotice>(); // Step 8 — weekly news notifications

if (sim.IncidentMinute > 0)
{
    var incident = _engine.ResolveIncident(
        _gameState.Squad, sim.IncidentMinute < 81, ref substitutionUsed,
        physioSkillPercent: _gameState.Physio?.SkillPercent ?? 0);

    if (incident != null)
    {
        matchIncidents.Add(new MatchIncident
        {
            Minute     = sim.IncidentMinute,
            PlayerName = incident.PlayerName,
            Type       = incident.Type,
            WeeksOut   = incident.WeeksOut
        });

        if (incident.Type == IncidentType.RedCard)
            newSuspensions.Add(new SuspensionNotice
            {
                PlayerName = incident.PlayerName,
                MatchesOut = 3,
                Reason     = SuspensionReason.RedCard
            });

        // Drop any pre-rolled goals that hadn't "happened" yet at the incident
        // minute — they're superseded by the re-roll below.
        sim.GoalEvents.RemoveAll(g => g.Minute >= sim.IncidentMinute);

        var updatedRatings = PlayerService.CalculateTeamRatings(_gameState.Squad);
        var updatedInput   = matchInput with
        {
            OurGoalkeeperSkill = updatedRatings.GoalkeeperRating,
            OurDefence         = updatedRatings.DefenceRating,
            OurMid             = updatedRatings.MidRating,
            OurAttack          = updatedRatings.AttackRating,
        };

        var (extraGoals, extraOurGoals, extraOppGoals) =
            _engine.ContinueMatchAfterIncident(updatedInput, sim.IncidentMinute, sim.MatchLength);

        sim.GoalEvents.AddRange(extraGoals);
        sim.OurGoalCount      = sim.GoalEvents.Count(g => g.IsOurGoal);
        sim.OpponentGoalCount = sim.GoalEvents.Count(g => !g.IsOurGoal);
    }
}
```

(`matchIncidents` and `newSuspensions` are declared once here and reused by the
yellow-card loop in Step 4 and the `MatchResult` construction in Steps 7–8.)

(`MatchSetupInput` needs to be a `record`/support `with`-expressions, or the equivalent
manual copy-and-reassign — either is fine; pick whichever matches the existing class's
mutability style.)

The rest of `PlayMatch`'s goal-iteration loop (`RecordOurGoal`/`RecordOpponentGoal`,
scoreline building) is unchanged — it already reads from `sim.GoalEvents` and from
`_gameState.Squad`, and the squad now correctly reflects the substitution/sending-off by
the time any post-incident goal is scored.

---

## Step 4 — Yellow cards

### `TheManager.Models/MatchSimulation.cs`

Add a list of yellow-card events, parallel to `IncidentMinute`. Each event carries both
the minute and the slot, because the slot must be decided once at roll time (not
re-derived later):

```csharp
/// <summary>Slot/minute pairs at which a starting player (1-12) picks up a yellow card.</summary>
public List<YellowCardEvent> YellowCardEvents { get; set; } = new();
```

```csharp
public class YellowCardEvent
{
    public int Minute { get; set; }
    public int Slot   { get; set; }   // 1-12, resolved at roll time
}
```

### `TheManager.Services/MatchEngineService.cs` — `SetupMatch`

After the existing crowd-incident roll, add an independent per-slot roll for slots 1–12:

```csharp
private const int YellowCardChancePercent = 6;   // ~0.7 cards/match on average (12 slots × 6%)

// ── Yellow cards (new — no BASIC equivalent) ──────────────────────────────
var yellowCardEvents = new List<YellowCardEvent>();
for (int slot = 1; slot <= 12; slot++)
{
    if (_random.Next(100) < YellowCardChancePercent)
        yellowCardEvents.Add(new YellowCardEvent { Slot = slot, Minute = 2 + _random.Next(matchLength - 2) });
}
```

Store on the result: `YellowCardEvents = yellowCardEvents`.

(Slots are resolved against the *starting* lineup at roll time, before any in-match
substitution/sending-off. This is a known simplification — see "What does NOT need
implementing".)

### `TheManager.Services/MatchEngineService.cs` — new method

```csharp
/// <summary>
/// Books a player for a yellow card. Returns null if the slot is empty/no longer
/// in the lineup (e.g. already subbed off or sent off). Increments the season
/// tally and applies a 1-match suspension at 5 cards, resetting the tally
/// afterwards.
/// </summary>
public YellowCardOutcome? ApplyYellowCard(Player?[] squad, int slot)
{
    if (slot < 1 || slot > 12) return null;
    var player = squad[slot];
    if (player == null) return null;

    player.YellowCardsThisSeason++;
    bool suspensionImposed = player.YellowCardsThisSeason >= 5;
    if (suspensionImposed)
    {
        player.SuspensionMatchesRemaining = Math.Max(player.SuspensionMatchesRemaining, 1);
        player.YellowCardsThisSeason = 0;
    }

    return new YellowCardOutcome(player.Name, suspensionImposed);
}
```

```csharp
public record YellowCardOutcome(string PlayerName, bool SuspensionImposed);
```

`SuspensionImposed` lets the caller (`GameService.PlayMatch`) raise the weekly-news
suspension notice described in Step 8, without re-reading `YellowCardsThisSeason` itself.

### `TheManager.Services/GameService.cs` — `PlayMatch`

Apply yellow cards in minute order alongside goals (no rating recompute — a yellow card
has no immediate playing effect):

```csharp
foreach (var ev in sim.YellowCardEvents)
{
    var outcome = _engine.ApplyYellowCard(_gameState.Squad, ev.Slot);
    if (outcome == null) continue;   // player no longer in the lineup — skip silently

    matchIncidents.Add(new MatchIncident
    {
        Minute = ev.Minute, PlayerName = outcome.PlayerName, Type = IncidentType.YellowCard
    });

    if (outcome.SuspensionImposed)
        newSuspensions.Add(new SuspensionNotice
        {
            PlayerName = outcome.PlayerName,
            MatchesOut = 1,
            Reason     = SuspensionReason.AccumulatedYellowCards
        });
}
```

---

## Step 5 — Suspension and injury countdown

### `TheManager.Services/WeeklyTickService.cs` — `Process`

Alongside the existing contract-weeks decrement loop (`for (int i = 1; i <= 20; i++)`),
decrement the two new counters for every occupied slot 1–20:

```csharp
if (p.WeeksInjured > 0)                 p.WeeksInjured--;
if (p.SuspensionMatchesRemaining > 0)   p.SuspensionMatchesRemaining--;
```

This can be folded into the existing loop body rather than added as a second pass.

---

## Step 6 — Reset yellow cards at season start

### `TheManager.Services/PlayerService.cs` — `ApplyEndOfSeasonSkillUpdate`

This already loops over squad slots 1–20 once per season (called from
`SeasonService.WrapUpSeason` step 8). Add the reset there:

```csharp
player.YellowCardsThisSeason = 0;
```

`WeeksInjured` and `SuspensionMatchesRemaining` are **not** reset here — a suspension or
injury picked up in the last match of a season should still apply into the new season,
matching how the original's `u(I)` is never zeroed at season-end either.

---

## Step 7 — Live match notifications for injuries and cards

Yellow cards, red cards, and injuries must all be announced as they happen during the
minute-by-minute match playback — the same animated timeline that already reveals goals
and half-time. `matchIncidents` (built in Steps 3 and 4 above) carries exactly the data
needed for this.

### `TheManager.Models/MatchResult.cs`

Add a list of card/injury events for display, parallel to `Goals`:

```csharp
/// <summary>Cards and injuries that occurred during the match, in chronological order.</summary>
public List<MatchIncident> Incidents { get; set; } = new();
```

```csharp
public class MatchIncident
{
    public int    Minute     { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public IncidentType Type { get; set; }   // Injury, RedCard, or YellowCard (extend enum — see below)
    public int    WeeksOut   { get; set; }   // injury only
}
```

### `TheManager.Models/Enums/IncidentType.cs`

```csharp
public enum IncidentType { Injury, RedCard, YellowCard }
```

### `TheManager.Services/GameService.cs` — `PlayMatch`

Assign the accumulated list when building the result:

```csharp
return new MatchResult
{
    // ... existing fields ...
    Incidents = matchIncidents
};
```

### `TheManager.Console/Screens/PlayMatchScreen.cs` — `ShowResult`

In the per-minute animation loop, alongside the existing goal-event handling, fire any
incident whose `Minute` matches and append a line to `events` (same `(Minute, Text,
Color)` tuple list already used for goals/half-time), so it appears in the live feed at
the moment it happens, not just in the post-match summary:

```csharp
foreach (var inc in result.Incidents.Where(i => i.Minute == min))
{
    string text  = inc.Type switch
    {
        IncidentType.Injury     => $"{inc.PlayerName.Trim()} INJURED ({inc.WeeksOut} wk)",
        IncidentType.RedCard    => $"{inc.PlayerName.Trim()} SENT OFF",
        IncidentType.YellowCard => $"{inc.PlayerName.Trim()} booked",
        _ => ""
    };
    string color = inc.Type == IncidentType.YellowCard ? "yellow" : "red";
    events.Add((minuteStr, Markup.Escape(text), color));
}
```

### `TheManager.Console/Screens/SquadScreen.cs` — `AddSection`

Show unavailability in the Name column, same pattern as `IsRetiring`/`RET`:

```csharp
string name = player is null              ? "[dim]—[/]"
            : player.WeeksInjured > 0                ? $"[red]{player.Name} (inj {player.WeeksInjured}w)[/]"
            : player.SuspensionMatchesRemaining > 0   ? $"[red]{player.Name} (susp {player.SuspensionMatchesRemaining})[/]"
            : player.IsTransferListed                 ? $"[red]{player.Name}[/]"
            : player.IsStar                           ? $"[yellow]{player.Name}[/]"
            : player.Name;
```

### `TheManager.Console/Screens/SquadScreen.cs` — `BuildSquadTable`: new "YC" column

Add a dedicated column for the season's accumulated yellow-card count, between `Games`
and `Wage`:

```csharp
.AddColumn(new TableColumn("[dim]#[/]").RightAligned())
.AddColumn(new TableColumn("[dim]Pos[/]"))
.AddColumn(new TableColumn("[bold]Name[/]"))
.AddColumn(new TableColumn("[dim]Skill[/]").RightAligned())
.AddColumn(new TableColumn("[dim]Age[/]").RightAligned())
.AddColumn(new TableColumn("[dim]Temper[/]").RightAligned())
.AddColumn(new TableColumn("[dim]Games[/]").RightAligned())
.AddColumn(new TableColumn("[dim]YC[/]").RightAligned())
.AddColumn(new TableColumn("[dim]Wage[/]").RightAligned())
.AddColumn(new TableColumn("[dim]Ctr[/]").RightAligned());
```

### `TheManager.Console/Screens/SquadScreen.cs` — `AddSection`: populate the new column

Add the matching cell in the per-player row:

```csharp
table.AddRow(
    $"[dim]{slot}[/]",
    pos,
    name,
    SkillCell(player),
    age,
    player?.Temper.ToString()                ?? "[dim]—[/]",
    player?.GamesPlayed.ToString()            ?? "[dim]—[/]",
    YellowCardCell(player),
    wage,
    contract);
```

```csharp
private static string YellowCardCell(Player? player)
{
    if (player is null) return "[dim]—[/]";
    return player.YellowCardsThisSeason >= 4
        ? $"[red]{player.YellowCardsThisSeason}[/]"   // one booking away from suspension
        : player.YellowCardsThisSeason.ToString();
}
```

The section-header rows (the `[bold dim] {title}[/]` rows for "SUBSTITUTE"/"RESERVES")
gain one more blank `new Markup("")` placeholder to match the new column count.

`YellowCardsThisSeason` resets to 0 at the start of each new season via Step 6's
`ApplyEndOfSeasonSkillUpdate` reset — no separate reset logic is needed for this column.

---

## Step 8 — Announce new suspensions in the weekly news

A red card or a 5th yellow card should also be called out in the post-match weekly news
block — the same area on `PlayMatchScreen` that already reports `SCOUT NEWS` and
`CONTRACT EXPIRIES` — so the manager finds out even if they weren't watching the live
minute-by-minute feed. `newSuspensions` (built in Steps 3 and 4 above) carries exactly
the data needed for this.

### `TheManager.Models/SuspensionNotice.cs`

```csharp
/// <summary>A suspension newly imposed during this match, for the weekly news block.</summary>
public class SuspensionNotice
{
    public string           PlayerName { get; set; } = string.Empty;
    public int               MatchesOut { get; set; }
    public SuspensionReason Reason     { get; set; }
}
```

### `TheManager.Models/Enums/SuspensionReason.cs`

```csharp
public enum SuspensionReason { RedCard, AccumulatedYellowCards }
```

### `TheManager.Models/MatchResult.cs`

```csharp
/// <summary>Suspensions newly imposed this match (red card or 5th yellow card).</summary>
public List<SuspensionNotice> NewSuspensions { get; set; } = new();
```

### `TheManager.Services/GameService.cs` — `PlayMatch`

Assign alongside `Incidents` when building the result:

```csharp
return new MatchResult
{
    // ... existing fields ...
    Incidents      = matchIncidents,
    NewSuspensions = newSuspensions
};
```

### `TheManager.Console/Screens/PlayMatchScreen.cs` — `ShowResult`

Display after the existing `CONTRACT EXPIRIES` block, following the same style:

```csharp
if (result.NewSuspensions.Count > 0)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("  [bold dim]SUSPENSIONS[/]");
    foreach (var s in result.NewSuspensions)
    {
        string reason = s.Reason == SuspensionReason.RedCard
            ? "sent off"
            : "five yellow cards";
        AnsiConsole.MarkupLine(
            $"  [red]{Markup.Escape(s.PlayerName.Trim())}[/] suspended for " +
            $"{s.MatchesOut} match{(s.MatchesOut == 1 ? "" : "es")} ({reason})");
    }
}
```

This is purely a notification — the actual `SuspensionMatchesRemaining` value was already       
set on the `Player` when the incident was resolved (Steps 2 and 4); this step only
surfaces that it happened.

---

## Worked example

1. Kick-off: `SetupMatch` rolls `IncidentMinute = 63` (red card or injury) and
   `YellowCardEvents = [{Minute: 22, Slot: 8}]`.
2. Minute 22: `ApplyYellowCard(squad, 8)` — slot 8 player's `YellowCardsThisSeason` goes
   from 2 to 3. Displayed as "booked".
3. Minute 63: `ResolveIncident` rolls an injury for slot 5 (a defender). No substitution
   used yet this match, so the substitute (slot 12) swaps into slot 5, and the injured
   defender is parked in reserve slot 14 with `WeeksInjured = 6`.
4. `sim.GoalEvents` with `Minute >= 63` are discarded. `PlayerService.CalculateTeamRatings`
   recomputes `DefenceRating` using the substitute's skill instead of the injured
   defender's. `ContinueMatchAfterIncident` re-rolls goals for minutes 63–`matchLength`
   using the updated rating.
5. Full time: the squad screen shows the defender as `(inj 6w)` in the reserves section;
   `WeeklyTickService.Process` decrements this to 5 the following week, and so on until
   it reaches 0.
6. Later in the season, the same defender picks up two more yellow cards (now at 5):
   `SuspensionMatchesRemaining = 1`, `YellowCardsThisSeason` resets to 0. He's unavailable
   for next week's match, then `IsAvailable` returns `true` again. The match's
   `NewSuspensions` list gets a `SuspensionNotice { PlayerName, MatchesOut = 1, Reason =
   AccumulatedYellowCards }`, and the post-match weekly news block on `PlayMatchScreen`
   shows "DEFENDER suspended for 1 match (five yellow cards)" alongside that week's scout
   news and contract expiries.
7. Separately, a red card later in the season sets `SuspensionMatchesRemaining = 3` for
   the sent-off player directly (independent of the yellow-card count), and the weekly
   news block shows "PLAYER suspended for 3 matches (sent off)".

---

## What does NOT need implementing

- **A second yellow card converting to a red card** within the same match — not present
  in BASIC, and not requested. `ApplyYellowCard` has no awareness of red cards.
- **Re-resolving yellow card eligibility against the live, post-incident lineup.** Slots
  for yellow cards are fixed at `SetupMatch` time against the starting 12; if a card's
  slot has since been vacated (injury/red card earlier in the match), `ApplyYellowCard`
  simply no-ops for that event. Re-deriving "who's actually on the pitch" at each yellow
  card's minute would require interleaving incident resolution and yellow-card rolls in
  strict minute order — a larger restructure not justified by how rarely two incidents
  land on the same player in the same match.
- **Opponent cards/injuries.** Exactly as in the original, incidents only ever apply to
  the managed club's own squad — the opponent is an abstracted rating, not a tracked
  squad.
- **Suspension affecting lineup selection/auto-pick.** This spec only tracks
  `IsAvailable`/`SuspensionMatchesRemaining`/`WeeksInjured` as data; nothing here changes
  how the squad screen lets the manager pick a starting XI (no existing auto-validation
  prevents fielding an unavailable player today, and adding that guard is a separate
  concern).
- **BASIC's flat 2-week red card ban** — explicitly replaced with a 3-match ban per this
  spec's requirements.
- **The `4664` BASIC branch** (a substitute who has *already* come on this match getting
  sent off, handled as a special case in the original with no ratings recompute). This
  spec's `ResolveIncident` treats any occupant of slots 1–12 uniformly — including a
  substitute who came on earlier in the same match — so the special-cased branch is
  superseded rather than ported.
- **`a3`/reserve-occupancy-driven retirement** (BASIC line 3217, the extra-training path)
  — unrelated to in-match incidents; already tracked separately as a known gap in the
  player-retirement spec area, not part of this spec.

---

## Affected files

| File | Change |
|---|---|
| `TheManager.Models/Player.cs` | Add `WeeksInjured`, `SuspensionMatchesRemaining`, `YellowCardsThisSeason`, `IsAvailable` |
| `TheManager.Models/MatchSimulation.cs` | Add `YellowCardEvents` (replacing a bare minute list) |
| `TheManager.Models/MatchSetupInput.cs` | Convert to a `record` (or support manual copy) so `GameService` can derive an updated-ratings copy |
| `TheManager.Models/MatchResult.cs` | Add `Incidents` and `NewSuspensions` lists |
| `TheManager.Models/MatchIncident.cs` | New model: minute, player name, type, weeks out |
| `TheManager.Models/SuspensionNotice.cs` | New model: player name, matches out, reason |
| `TheManager.Models/Enums/IncidentType.cs` | Add `YellowCard` |
| `TheManager.Models/Enums/SuspensionReason.cs` | New enum: `RedCard`, `AccumulatedYellowCards` |
| `TheManager.Services/MatchEngineService.cs` | `ResolveIncident` mutates squad and takes `ref bool substitutionUsed`; extract `GenerateGoalsForWindow`; add `ContinueMatchAfterIncident`; add yellow-card roll in `SetupMatch`; add `ApplyYellowCard` returning `YellowCardOutcome` |
| `TheManager.Services/GameService.cs` | `PlayMatch` calls `ResolveIncident`, re-rolls post-incident goals, applies yellow cards, accumulates `matchIncidents`/`newSuspensions`, builds `MatchResult.Incidents`/`NewSuspensions` |
| `TheManager.Services/WeeklyTickService.cs` | Decrement `WeeksInjured` and `SuspensionMatchesRemaining` per squad slot |
| `TheManager.Services/PlayerService.cs` | `ApplyEndOfSeasonSkillUpdate` resets `YellowCardsThisSeason` to 0 |
| `TheManager.Console/Screens/PlayMatchScreen.cs` | Display incidents live during the minute-by-minute animation; display `NewSuspensions` in the post-match weekly news block alongside scout news and contract expiries |
| `TheManager.Console/Screens/SquadScreen.cs` | Show `(inj Nw)` / `(susp N)` in the Name column; add a "YC" column showing `YellowCardsThisSeason` |
