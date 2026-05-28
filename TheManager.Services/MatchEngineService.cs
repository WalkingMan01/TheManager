using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Pre-computes all goal events before kick-off and resolves in-match incidents
/// (injuries, red cards).
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
    private const int    MaxPickAttempts            = 20;   // retry cap in slot-picker helpers

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
    /// </summary>
    public MatchSimulation SetupMatch(MatchSetupInput input)
    {
        int matchLength = 90 + _random.Next(4);   // 90–93 minutes

        // ── Our shot count ────────────────────────────────────────────────────
        int ourMoraleRoll = 1 + _random.Next(3);   // 1–3

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

        // For cup second legs, subtract the first-leg totals so only the
        // additional goals for this match are represented in the event list.
        int adjustedOurGoals      = Math.Max(0, ourGoals      - input.PreviousLegOurScore);
        int adjustedOpponentGoals = Math.Max(0, opponentGoals - input.PreviousLegTheirScore);

        // ── Assign goal minutes ───────────────────────────────────────────────
        const int goalStartMinute = 2;
        var goalEvents = new List<GoalEvent>();
        AssignGoalMinutes(goalEvents, adjustedOurGoals,      goalStartMinute, matchLength, isOurGoal: true);
        AssignGoalMinutes(goalEvents, adjustedOpponentGoals, goalStartMinute, matchLength, isOurGoal: false);

        // ── Crowd / discipline incident minute ────────────────────────────────
        // Higher combined temper raises the chance of an incident occurring.
        int incidentMinute    = 0;
        int temperCombined    = input.OurTemper + input.OpponentTemper;
        int crowdIncidentRoll = _random.Next(TemperIncidentDenominator);
        if (crowdIncidentRoll < temperCombined)
            incidentMinute = 2 + _random.Next(80);   // minute 2–81

        return new MatchSimulation
        {
            MatchLength       = matchLength,
            GoalEvents        = goalEvents,
            IncidentMinute    = incidentMinute,
            OurGoalCount      = adjustedOurGoals,
            OpponentGoalCount = adjustedOpponentGoals
        };
    }

    // ── Incident handling ─────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a crowd/discipline incident into an injury or red card.
    /// Returns <see langword="null"/> if the incident has no effect (e.g. no
    /// eligible player, no reserve slot available, or suppressed by timing roll).
    /// </summary>
    /// <param name="physioSkillPercent">
    /// Physio skill 1–99, or 0 if no physio is employed. Reduces injury duration:
    ///   reduction = floor(abs(weeks) / 100 × floor(<see cref="PhysioEffectiveness"/> × physioSkill))
    ///   weeks     = max(1, abs(weeks) − reduction)
    /// </param>
    public IncidentResult? ResolveIncident(
        Player?[] squad,
        bool incidentBeforeMinute81,
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

        // A free reserve slot (13–20) is required to receive the substitute;
        // if none exists no swap can be made and the incident is ignored.
        bool hasReserveSlot = false;
        for (int i = 13; i <= 20; i++)
        {
            if (squad[i] == null) { hasReserveSlot = true; break; }
        }
        if (!hasReserveSlot) return null;

        // Before minute 81, a roll of 2 results in a red card.
        if (incidentBeforeMinute81 && redCardRoll == 2)
            return new IncidentResult
            {
                Type       = IncidentType.RedCard,
                PlayerSlot = playerSlot,
                PlayerName = player.Name
            };

        // Otherwise: injury. Duration is reduced by physio skill.
        int rawWeeks    = _random.Next(32) - 8;
        int physioFactor = (int)(PhysioEffectiveness * physioSkillPercent);
        int reduction   = (int)(Math.Abs(rawWeeks) / 100.0 * physioFactor);
        int injuryWeeks = Math.Max(1, Math.Abs(rawWeeks) - reduction);

        return new IncidentResult
        {
            Type       = IncidentType.Injury,
            PlayerSlot = playerSlot,
            PlayerName = player.Name,
            WeeksOut   = injuryWeeks
        };
    }

    // ── Goal scoring ──────────────────────────────────────────────────────────

    /// <summary>
    /// Credits a goal to a squad player: picks the scorer, updates their season
    /// stats and skill, and returns their name. Returns <see langword="null"/>
    /// if no suitable player is found in the squad.
    /// </summary>
    public string? RecordOurGoal(Player?[] squad)
    {
        // 2/3 of the time an attacker scores; 1/3 of the time a non-attacker.
        int? scorerSlot = _random.Next(3) == 0
            ? PickNonAttackerSlot(squad)
            : PickAttackerSlot(squad);

        if (scorerSlot == null) return null;
        var scorer = squad[scorerSlot.Value];
        if (scorer == null) return null;

        // Negative SeasonGoals signals a retirement-track state; the counter
        // moves further negative rather than crossing back through zero.
        scorer.SeasonGoals += scorer.SeasonGoals >= 0 ? 1 : -1;
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

        // Negative SeasonGoals signals a retirement-track state (same as outfield).
        gk.SeasonGoals += gk.SeasonGoals >= 0 ? 1 : -1;
        gk.Appearances++;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

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
        int startMinute, int matchLength, bool isOurGoal)
    {
        for (int goal = 0; goal < goalCount; goal++)
        {
            int minute = startMinute + _random.Next(matchLength - 1);
            goalEvents.Add(new GoalEvent { Minute = minute, IsOurGoal = isOurGoal });
        }
    }

    // Picks a non-empty, non-attacker slot from 2–11.
    // Returns null if no match is found within MaxPickAttempts tries.
    private int? PickNonAttackerSlot(Player?[] squad)
    {
        for (int attempt = 0; attempt < MaxPickAttempts; attempt++)
        {
            int slot = 2 + _random.Next(10);
            var p = squad[slot];
            if (p != null && p.Position != PlayerPosition.Attacker)
                return slot;
        }
        return null;
    }

    // Picks an attacker slot from 2–11.
    // Returns null if no match is found within MaxPickAttempts tries.
    private int? PickAttackerSlot(Player?[] squad)
    {
        for (int attempt = 0; attempt < MaxPickAttempts; attempt++)
        {
            int slot = 2 + _random.Next(10);
            if (squad[slot]?.Position == PlayerPosition.Attacker)
                return slot;
        }
        return null;
    }
}
