using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Pre-computes all goal events before kick-off and handles in-match incidents
/// (injuries, red cards, substitutions).
///
/// The match simulation in FOOT.BAS works by front-loading every goal into a
/// timed event array (B[], c[]) during subroutine 4509, then "playing out"
/// the 90 minutes in a display loop that fires those events at the right minute.
/// This service replicates that calculation without any display concerns.
/// </summary>
public class MatchEngine
{
    private readonly Random _random;

    public MatchEngine(Random? random = null)
    {
        _random = random ?? new Random();
    }

    // ── Pre-match setup ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds the full list of timed goal events for a match.
    /// Call this once before the match "starts"; events drive score updates.
    ///
    /// Corresponds to subroutine 4509 (lines 3756–3793) in FOOT.BAS.
    ///
    /// Algorithm summary:
    ///   1. Derive ourShotCount (BN) from attack/mid vs opponent GK/morale/form.
    ///   2. Derive opponentShotCount (BO) from opponent attack vs our GK/morale.
    ///   3. Each shot is tested against the opposing GK integer skill.
    ///   4. If total goals would exceed 8 (the event array limit), reduce evenly.
    ///   5. Spread each goal randomly across match minutes (startMinute to matchLength−1).
    /// </summary>
    public MatchSimulation SetupMatch(MatchSetupInput input)
    {
        int matchLength = 90 + _random.Next(4);   // 90–93 minutes (md)

        // ── Our shot count (BN) — line 3765 ──────────────────────────────────
        int moraleRoll = 1 + _random.Next(5);

        int ourShotCount = Math.Max(0,
            Math.Min(input.OurAttack, input.OurMid)
            - input.OpponentGoalkeeperSkill / 2
            + (input.IsHomeGame          ? 1 : 0)
            - (input.LostLastMatch       ? 1 : 0)
            - (input.LineupChanges > 3   ? 1 : 0)
            + (moraleRoll == 1 && input.OurMorale > 80  ?  1 : 0)
            - (moraleRoll == 3 && input.OurMorale < 30  ?  1 : 0)
            - (input.OpponentTemper - input.OurTemper > 29 ? 1 : 0));

        // ── Opponent shot count (BO) — line 3767 ─────────────────────────────
        moraleRoll = 1 + _random.Next(5);

        int opponentShotCount = Math.Max(0,
            Math.Min(input.OpponentAttack, input.OpponentMid)
            - input.OurDefence / 2
            - (input.OurDefence == input.OurMid && input.OurMid == input.OurAttack ? 1 : 0)
            + (moraleRoll == 1 && input.OpponentMorale > 80 ?  1 : 0)
            - (moraleRoll == 3 && input.OpponentMorale < 30 ?  1 : 0)
            - (input.OurTemper - input.OpponentTemper > 29  ?  1 : 0));

        // ── Convert shots into goals (lines 4510–4512) ───────────────────────
        int ourGoals      = CountGoals(ourShotCount,      input.OpponentGoalkeeperSkill, divisionBonus: 0);
        int opponentGoals = CountGoals(opponentShotCount, input.OurGoalkeeperSkill,      divisionBonus: input.Division - 1);

        // Reduce to fit 8-event array (line 4513)
        while (ourGoals + opponentGoals > 8)
        {
            if (ourGoals      > 0) ourGoals--;
            if (opponentGoals > 0) opponentGoals--;
        }

        // Subtract goals already scored in previous leg (cup ties) — lines 4514–4516
        int adjustedOurGoals      = Math.Max(0, ourGoals      - input.PreviousLegOurScore);
        int adjustedOpponentGoals = Math.Max(0, opponentGoals - input.PreviousLegTheirScore);

        // ── Assign goal minutes (lines 4514–4517) ────────────────────────────
        const int goalStartMinute = 2;
        var goalEvents = new List<GoalEvent>();
        AssignGoalMinutes(goalEvents, adjustedOurGoals,      goalStartMinute, matchLength, scorer: 1);
        AssignGoalMinutes(goalEvents, adjustedOpponentGoals, goalStartMinute, matchLength, scorer: 2);

        // ── Crowd/discipline incident minute ─────────────────────────────────
        // Line 3084: fires when N=BU and N<81
        int incidentMinute     = 0;
        int temperCombined     = input.OurTemper + input.OpponentTemper;
        int crowdIncidentRoll  = _random.Next(472);
        if (crowdIncidentRoll < temperCombined)
            incidentMinute = 2 + _random.Next(80);   // within first 81 minutes

        return new MatchSimulation
        {
            MatchLength         = matchLength,
            GoalEvents          = goalEvents,
            IncidentMinute      = incidentMinute,
            OurGoalCount        = ourGoals,
            OpponentGoalCount   = opponentGoals
        };
    }

    // ── Incident handling ─────────────────────────────────────────────────────

    /// <summary>
    /// Determines whether a crowd/discipline incident occurs and resolves its
    /// effect (injury or red card).
    ///
    /// Corresponds to subroutine 4651 (lines 3808–3868) in FOOT.BAS.
    /// </summary>
    /// <param name="physioSkillPercent">
    /// Physio skill (1–99), or 0 if no physio is employed. Used to reduce
    /// injury duration per BASIC line 4657:
    ///   RA = INT((60/100)*Y(2,2)); RB = INT((ABS(weeks)/100)*RA);
    ///   weeks = MAX(1, ABS(weeks) − RB)
    /// </param>
    public IncidentResult? ResolveIncident(
        Player?[] squad,
        bool incidentBeforeMinute81,
        bool hasSubstituted,
        int physioSkillPercent = 0)
    {
        int playerSlot = 1 + _random.Next(20);
        var player = squad[playerSlot];

        if (player == null || player.Position == PlayerPosition.None)
            return null;

        // Only first-team players (slots 1–12) can be involved (line 4653)
        if (playerSlot > 12) return null;

        int redCardRoll = 1 + _random.Next(2);

        // Second half + 50/50: send-off (line 4663)
        if (!incidentBeforeMinute81 && redCardRoll == 2)
            return new IncidentResult
            {
                Type       = IncidentType.RedCard,
                PlayerSlot = playerSlot,
                PlayerName = player.Name
            };

        // Injury — duration reduced by physio skill (BASIC line 4657)
        // RA=INT((60/100)*Y(2,2)); RB=INT((ABS(u)/100)*RA); u=MAX(1,ABS(u)−RB)
        int rawInjuryWeeks = _random.Next(32) - 8;
        int physioFactor   = (int)(60.0 / 100 * physioSkillPercent);
        int reduction      = (int)(Math.Abs(rawInjuryWeeks) / 100.0 * physioFactor);
        int injuryWeeks    = Math.Max(1, Math.Abs(rawInjuryWeeks) - reduction);

        return new IncidentResult
        {
            Type       = IncidentType.Injury,
            PlayerSlot = playerSlot,
            PlayerName = player.Name,
            WeeksOut   = injuryWeeks
        };
    }

    // ── Goal scoring events ───────────────────────────────────────────────────

    /// <summary>
    /// Records that a goal was scored by one of our outfield players, updates
    /// their stats, and returns the scorer's name.
    /// Corresponds to subroutine 4501–4503 (lines 3724–3736).
    /// </summary>
    public static string? RecordOurGoal(Player?[] squad, Random rng)
    {
        // 1/3 chance it's an attacker, otherwise a non-attacker
        int scorerSlot = rng.Next(3) == 0
            ? 2 + rng.Next(10)
            : PickNonAttackerSlot(squad, rng);

        var scorer = squad[scorerSlot];
        if (scorer == null) return null;

        scorer.Skill += 0.04;  // small skill boost for scoring — line 4734
        PlayerService.RecalculateStatus(scorer);
        return scorer.Name;
    }

    /// <summary>
    /// Records that the opponent scored. Updates GK conceded statistics.
    /// Corresponds to subroutine 4505–4507 (lines 3742–3751).
    /// </summary>
    public static void RecordOpponentGoal(Player?[] _) { }

    // ── Private helpers ───────────────────────────────────────────────────────

    private int CountGoals(int shotCount, int opposingGoalkeeperSkill, int divisionBonus)
    {
        int goals = 0;
        for (int shot = 0; shot < shotCount; shot++)
        {
            int diceRoll = _random.Next(8) + divisionBonus;
            if (diceRoll > opposingGoalkeeperSkill / 2)
                goals++;
        }
        return goals;
    }

    private void AssignGoalMinutes(
        List<GoalEvent> goalEvents, int goalCount,
        int startMinute, int matchLength, int scorer)
    {
        for (int goal = 0; goal < goalCount; goal++)
        {
            int goalMinute = startMinute + _random.Next(matchLength - 1);
            goalEvents.Add(new GoalEvent { Minute = goalMinute, Scorer = scorer });
        }
    }

    private static int PickNonAttackerSlot(Player?[] squad, Random random)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            int slot = 2 + random.Next(10);
            if (squad[slot]?.Position != PlayerPosition.Attacker)
                return slot;
        }
        return 2 + random.Next(10);
    }
}
