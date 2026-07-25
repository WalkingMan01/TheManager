using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Pre-computes all goal events before kick-off and resolves in-match incidents
/// (injuries, red cards, yellow cards).
///
/// Goal simulation front-loads every event into a timed list during
/// <see cref="SetupMatch"/>, which the presentation layer fires at the appropriate
/// match minute without any display logic here.
/// </summary>
public class MatchEngineService
{
    private readonly Random _random;

    // ── Constants ─────────────────────────────────────────────────────────────

    private const int    MaxGoalEvents             = 8;    // event-array capacity
    private const int    GoalkeeperSlot             = 1;    // squad index of the GK
    private const double PhysioEffectiveness        = 0.6;  // fraction of physio skill applied to injury reduction
    private const double SkillBoostPerGoal          = 0.04; // skill gain awarded to the scorer
    private const int    TemperIncidentDenominator  = 472;  // probability denominator for crowd incidents
    private const int    YellowCardChancePercent    = 6;    // per starting slot, per match (~0.7 cards/match on average)
    private const int    GoalStartMinute            = 2;

    public MatchEngineService(Random? random = null)
    {
        _random = random ?? new Random();
    }

    // ── Pre-match setup ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds the full list of timed goal events for a match.
    /// Call this once before the match "starts"; events drive score updates.
    ///
    /// Algorithm:
    ///   1. Derive ourShotCount from attack/mid vs opponent GK/morale/form.
    ///   2. Derive opponentShotCount from opponent attack vs our GK/morale.
    ///   3. Each shot is tested against the opposing GK's integer skill.
    ///   4. If total goals exceed <see cref="MaxGoalEvents"/>, reduce evenly.
    ///   5. For cup second legs, subtract the first-leg scores so the event
    ///      list represents only the goals scored in this match.
    ///   6. Spread each goal randomly across match minutes.
    ///   7. Roll a single crowd/discipline incident (injury or red card) and an
    ///      independent set of yellow cards for the match.
    /// </summary>
    public MatchSimulation SetupMatch(MatchSetupInput input)
    {
        int matchLength = 90 + _random.Next(4);   // 90–93 minutes

        var (goalEvents, ourGoals, opponentGoals) = GenerateGoalsForWindow(input, GoalStartMinute, matchLength);

        // For cup second legs, drop the first-leg's already-counted goals so
        // the event list represents only the goals scored in this match.
        int dropOurGoals      = Math.Min(ourGoals,      input.PreviousLegOurScore);
        int dropOpponentGoals = Math.Min(opponentGoals, input.PreviousLegTheirScore);
        RemoveFirstGoals(goalEvents, isOurGoal: true,  count: dropOurGoals);
        RemoveFirstGoals(goalEvents, isOurGoal: false, count: dropOpponentGoals);

        int adjustedOurGoals      = ourGoals      - dropOurGoals;
        int adjustedOpponentGoals = opponentGoals - dropOpponentGoals;

        // ── Crowd / discipline incident minute ────────────────────────────────
        // Higher combined temper raises the chance of an incident occurring.
        int incidentMinute    = 0;
        int temperCombined    = input.OurTemper + input.OpponentTemper;
        int crowdIncidentRoll = _random.Next(TemperIncidentDenominator);
        if (crowdIncidentRoll < temperCombined)
            incidentMinute = 2 + _random.Next(80);   // minute 2–81

        // ── Yellow cards (new — no BASIC equivalent) ────────────────────────────
        var yellowCardEvents = new List<YellowCardEvent>();
        for (int slot = 1; slot <= 12; slot++)
        {
            if (_random.Next(100) < YellowCardChancePercent)
                yellowCardEvents.Add(new YellowCardEvent { Slot = slot, Minute = 2 + _random.Next(matchLength - 2) });
        }

        return new MatchSimulation
        {
            MatchLength       = matchLength,
            GoalEvents        = goalEvents,
            IncidentMinute    = incidentMinute,
            OurGoalCount      = adjustedOurGoals,
            OpponentGoalCount = adjustedOpponentGoals,
            YellowCardEvents  = yellowCardEvents
        };
    }

    /// <summary>
    /// Re-rolls the goal model for the remainder of a match after a red card or
    /// unreplaced injury has changed the team's ratings. Mirrors BASIC subroutine
    /// 4509 (lines 3756-3793), called after 332 recomputes ratings.
    /// </summary>
    public (List<GoalEvent> goals, int ourGoals, int opponentGoals) ContinueMatchAfterIncident(
        MatchSetupInput updatedInput, int fromMinute, int matchLength)
        => GenerateGoalsForWindow(updatedInput, fromMinute, matchLength);

    // ── Incident handling ─────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a crowd/discipline incident into an injury or red card, mutating
    /// <paramref name="squad"/> to reflect the outcome (substitute brought on for
    /// an injury, sent-off/injured player parked in a reserve slot). Returns
    /// <see langword="null"/> if the incident has no effect (e.g. no eligible
    /// player, no reserve slot available, or suppressed by timing roll).
    /// </summary>
    /// <param name="substitutionUsed">
    /// Tracks whether this match's single substitution has already been used
    /// (mirrors BASIC's <c>MP</c> flag). Pass the same variable across all calls
    /// within one match; an injury consumes it if it's still available.
    /// </param>
    /// <param name="physioSkillPercent">
    /// Physio skill 1–99, or 0 if no physio is employed. Reduces injury duration:
    ///   reduction = floor(abs(weeks) / 100 × floor(<see cref="PhysioEffectiveness"/> × physioSkill))
    ///   weeks     = max(1, abs(weeks) − reduction)
    /// </param>
    public IncidentResult? ResolveIncident(
        Player?[] squad,
        bool incidentBeforeMinute81,
        ref bool substitutionUsed,
        int physioSkillPercent = 0)
    {
        int playerSlot  = 1 + _random.Next(18);   // slots 1–18
        int redCardRoll = 1 + _random.Next(2);     // 1 or 2

        // After minute 80, a roll of 2 suppresses the incident entirely.
        if (!incidentBeforeMinute81 && redCardRoll == 2)
            return null;

        // Only first-team players (slots 1–12) can be affected.
        if (playerSlot > 12) return null;

        var player = squad[playerSlot];
        if (player == null || player.Position == PlayerPosition.None)
            return null;

        // A free reserve slot (13–20) is required to receive the player coming
        // out of the lineup; if none exists no swap can be made and the
        // incident is ignored.
        int freeReserveSlot = -1;
        for (int i = 13; i <= 20; i++)
        {
            if (squad[i] == null) { freeReserveSlot = i; break; }
        }
        if (freeReserveSlot < 0) return null;

        // Before minute 81, a roll of 2 results in a red card.
        if (incidentBeforeMinute81 && redCardRoll == 2)
        {
            player.SuspensionMatchesRemaining = 3;
            squad[freeReserveSlot] = player;
            squad[playerSlot]      = null;   // slot now empty for the rest of the match

            return new IncidentResult
            {
                Type       = IncidentType.RedCard,
                PlayerSlot = playerSlot,
                PlayerName = player.Name
            };
        }

        // Otherwise: injury. Duration is reduced by physio skill.
        int rawWeeks     = _random.Next(32) - 8;
        int physioFactor = (int)(PhysioEffectiveness * physioSkillPercent);
        int reduction    = (int)(Math.Abs(rawWeeks) / 100.0 * physioFactor);
        int injuryWeeks  = Math.Max(1, Math.Abs(rawWeeks) - reduction);

        player.WeeksInjured    = injuryWeeks;
        squad[freeReserveSlot] = player;   // injured player parked in reserves either way

        if (!substitutionUsed)
        {
            int subSlot = GoalkeeperSlot + 11;   // slot 12
            squad[playerSlot] = squad[subSlot];  // substitute takes the injured player's place
            squad[subSlot]    = null;            // vacate the substitute's old slot
            substitutionUsed  = true;
        }
        else
        {
            // No sub left — the injured player leaves with no replacement, so
            // the team plays the rest of the match a player short.
            squad[playerSlot] = null;
        }

        return new IncidentResult
        {
            Type       = IncidentType.Injury,
            PlayerSlot = playerSlot,
            PlayerName = player.Name,
            WeeksOut   = injuryWeeks
        };
    }

    /// <summary>
    /// Books a player for a yellow card. Returns null if the slot is empty/no
    /// longer in the lineup (e.g. already subbed off or sent off). Increments
    /// the season tally and applies a 1-match suspension at 5 cards, resetting
    /// the tally afterwards.
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

    // ── Goal scoring ──────────────────────────────────────────────────────────

    /// <summary>
    /// Credits a goal to a squad player: picks the scorer, updates their season
    /// stats and skill, and returns their name. Returns <see langword="null"/>
    /// only when slots 2–11 hold no players at all.
    /// </summary>
    public string? RecordOurGoal(Player?[] squad)
    {
        // 2/3 of the time an attacker scores; 1/3 of the time a non-attacker.
        // If the preferred category has no one on the pitch (sent off, injured,
        // unusual formation), fall back to the other so a scorer is always
        // credited rather than the goal going unattributed.
        int? scorerSlot = _random.Next(3) == 0
            ? PickNonAttackerSlot(squad) ?? PickAttackerSlot(squad)
            : PickAttackerSlot(squad) ?? PickNonAttackerSlot(squad);

        if (scorerSlot == null) return null;
        var scorer = squad[scorerSlot.Value];
        if (scorer == null) return null;

        scorer.SeasonGoals++;
        scorer.Appearances++;
        scorer.Skill += SkillBoostPerGoal;
        PlayerService.RecalculateStatus(scorer);
        return scorer.Name;
    }

    /// <summary>
    /// Records that the opponent scored, incrementing the GK's conceded-goals
    /// counter and appearances.
    /// </summary>
    public void RecordOpponentGoal(Player?[] squad)
    {
        var gk = squad[GoalkeeperSlot];
        if (gk == null) return;

        gk.SeasonGoals++;
        gk.Appearances++;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Derives shot counts from the input ratings, converts them to goals, and
    /// spreads the resulting goal events across the given minute window.
    /// Shared by the pre-match full-window roll (<see cref="SetupMatch"/>) and
    /// the post-incident continuation (<see cref="ContinueMatchAfterIncident"/>).
    /// </summary>
    private (List<GoalEvent> goals, int ourGoals, int opponentGoals) GenerateGoalsForWindow(
        MatchSetupInput input, int windowStartMinute, int windowEndMinute)
    {
        // ── Our shot count ────────────────────────────────────────────────────
        // Deviation from BASIC (RND*3): widened to 1–5 so the high-morale shot
        // bonus fires 1 in 5, matching the opponent's roll.
        int ourMoraleRoll = 1 + _random.Next(5);   // 1–5

        int ourShotCount = Math.Max(0,
            Math.Min(input.OurAttack, input.OurMid)
            - input.OpponentGoalkeeperSkill / 2
            + (input.IsHomeGame                                 ?  1 : 0)
            - (input.LostLastMatch                              ?  1 : 0)
            - (input.LineupChanges > 3                          ?  1 : 0)
            + (ourMoraleRoll == 1 && input.OurMorale > 80      ?  1 : 0)
            - (ourMoraleRoll == 3 && input.OurMorale < 30      ?  1 : 0)
            - (input.OpponentTemper - input.OurTemper > 29     ?  1 : 0));

        // ── Opponent shot count ───────────────────────────────────────────────
        int opponentMoraleRoll = 1 + _random.Next(5);   // 1–5

        int opponentShotCount = Math.Max(0,
            Math.Min(input.OpponentAttack, input.OpponentMid)
            - input.OurDefence / 2
            - (input.OurDefence == input.OurMid && input.OurMid == input.OurAttack ?  1 : 0)
            + (!input.IsHomeGame                                     ?  1 : 0)
            + (opponentMoraleRoll == 1 && input.OpponentMorale > 80 ?  1 : 0)
            - (opponentMoraleRoll == 3 && input.OpponentMorale < 30 ?  1 : 0)
            - (input.OurTemper - input.OpponentTemper > 29          ?  1 : 0));

        // ── Convert shots into goals ──────────────────────────────────────────
        // Division 2 applies a +1 dice bonus for the opponent to reflect higher
        // attacking quality in that tier.
        int ourGoals      = CountGoals(ourShotCount,      input.OpponentGoalkeeperSkill, divisionBonus: 0);
        int opponentGoals = CountGoals(opponentShotCount, input.OurGoalkeeperSkill,      divisionBonus: input.Division == 2 ? 1 : 0);

        // Reduce to fit the fixed-size event array.
        while (ourGoals + opponentGoals > MaxGoalEvents)
        {
            if (ourGoals      > 0) ourGoals--;
            if (opponentGoals > 0) opponentGoals--;
        }

        // ── Assign goal minutes ───────────────────────────────────────────────
        var goalEvents = new List<GoalEvent>();
        AssignGoalMinutes(goalEvents, ourGoals,      windowStartMinute, windowEndMinute, isOurGoal: true);
        AssignGoalMinutes(goalEvents, opponentGoals, windowStartMinute, windowEndMinute, isOurGoal: false);

        return (goalEvents, ourGoals, opponentGoals);
    }

    private int CountGoals(int shotCount, int opposingGoalkeeperSkill, int divisionBonus)
    {
        int goals = 0;
        for (int shot = 0; shot < shotCount; shot++)
        {
            int diceRoll = _random.Next(MaxGoalEvents) + divisionBonus;
            if (diceRoll > opposingGoalkeeperSkill / 2)
                goals++;
        }
        return goals;
    }

    private void AssignGoalMinutes(
        List<GoalEvent> goalEvents, int goalCount,
        int windowStartMinute, int windowEndMinute, bool isOurGoal)
    {
        int windowWidth = Math.Max(1, windowEndMinute - windowStartMinute);
        for (int goal = 0; goal < goalCount; goal++)
        {
            int minute = windowStartMinute + _random.Next(windowWidth);
            goalEvents.Add(new GoalEvent { Minute = minute, IsOurGoal = isOurGoal });
        }
    }

    // Removes up to `count` events matching `isOurGoal` from the list — used to
    // apply the cup second-leg carry-over after goals have already been assigned
    // minutes (statistically equivalent to not generating them in the first place).
    private static void RemoveFirstGoals(List<GoalEvent> goalEvents, bool isOurGoal, int count)
    {
        for (int removed = 0; removed < count; removed++)
        {
            int index = goalEvents.FindIndex(g => g.IsOurGoal == isOurGoal);
            if (index < 0) break;
            goalEvents.RemoveAt(index);
        }
    }

    // Picks a non-empty, non-attacker slot from 2–11.
    // Picks a random non-attacker slot from 2–11, or null if none exist.
    private int? PickNonAttackerSlot(Player?[] squad)
    {
        var candidates = new List<int>();
        for (int slot = 2; slot <= 11; slot++)
        {
            var p = squad[slot];
            if (p != null && p.Position != PlayerPosition.Attacker)
                candidates.Add(slot);
        }
        return candidates.Count > 0 ? candidates[_random.Next(candidates.Count)] : null;
    }

    // Picks a random attacker slot from 2–11, or null if none exist.
    private int? PickAttackerSlot(Player?[] squad)
    {
        var candidates = new List<int>();
        for (int slot = 2; slot <= 11; slot++)
        {
            if (squad[slot]?.Position == PlayerPosition.Attacker)
                candidates.Add(slot);
        }
        return candidates.Count > 0 ? candidates[_random.Next(candidates.Count)] : null;
    }
}

/// <summary>Outcome of <see cref="MatchEngineService.ApplyYellowCard"/>.</summary>
public record YellowCardOutcome(string PlayerName, bool SuspensionImposed);
