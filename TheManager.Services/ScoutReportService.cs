using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Processes weekly scout reports: each assigned scout has a chance to identify
/// a player matching their target criteria and place them in the transfer market
/// (squad slots 21–23).
///
/// Corresponds to subroutine 312 in FOOT.BAS (lines 327–368).
///
/// Three scout slots (indices 3–5 in the I$/Y arrays) report into squad slots
/// 21, 22, 23 respectively. A successful report generates a transfer-target
/// player (name, position, skill, age, temper) in that slot.
/// </summary>
public static class ScoutReportService
{
    /// <summary>
    /// Runs a weekly report pass for all three scout slots.
    ///
    /// BASIC subroutine 312 (lines 327–368):
    ///   For each scout I = 21..23  (I-18 = scout index 3..5 in Y arrays):
    ///     Random check: 1/9 chance of no find this week (RA=1 → skip).
    ///     If scout has no assigned team/position, clear slot and skip.
    ///     Derive a target skill range from the scout's skill% and target form.
    ///     Additional quality check: only 1/4 chance of proceeding even in range.
    ///     On success: generate a player in squad slot I (21/22/23).
    /// </summary>
    public static List<ScoutFinding> RunWeeklyReports(
        GameState gameState,
        Random    rng)
    {
        var findings = new List<ScoutFinding>();

        for (int scoutSquadSlot = 21; scoutSquadSlot <= 23; scoutSquadSlot++)
        {
            int scoutIndex = scoutSquadSlot - 18;   // scout index 3, 4, or 5

            // Validate scout exists and has an assignment
            if (scoutIndex - 3 >= gameState.Scouts.Count) continue;
            var scout = gameState.Scouts[scoutIndex - 3];

            if (!scout.IsAssigned || scout.LookingForPosition == PlayerPosition.None)
            {
                // Clear the slot — no active report (line 313: goto 315)
                gameState.Squad[scoutSquadSlot] = null;
                continue;
            }

            // 1/9 chance of no find this week (line 312: IF RA=1 THEN goto 315)
            if (1 + rng.Next(9) == 1)
            {
                gameState.Squad[scoutSquadSlot] = null;
                continue;
            }

            // ── Target skill range calculation (lines 313) ────────────────────
            // rd = deviation from 90% skill: rd = INT(ABS((skill/10 − 9)/2))
            // e.g. skill=70% → rd = INT(ABS((7-9)/2)) = 1
            int    scoutSkillDeviation = (int)(Math.Abs((scout.SkillPercent / 10.0 - 9) / 2));
            int    halfDeviation       = scoutSkillDeviation / 2;
            int    targetFormLow       = scout.LookingForForm - scoutSkillDeviation;
            int    targetFormHigh      = scout.LookingForForm + halfDeviation;

            // Randomise found skill within range
            int    skillRangeSpread    = Math.Max(1, targetFormHigh - targetFormLow + 1);
            double foundSkill          = targetFormLow + rng.Next(skillRangeSpread);

            // Clamp found skill to 1–9 and add decimal fraction
            foundSkill = Math.Max(1, Math.Min(9, foundSkill));
            foundSkill = foundSkill + (rng.Next(5) / 10.0);   // 0.0–0.4 decimal

            // ── Division quality gate (line 313: RB=INT(10-((Y(3)+19)/20))) ──
            // Targets from higher-division clubs are harder to find
            int divisionBonus     = (int)(10 - ((scout.AssignedTeamIndex + 19) / 20.0));
            int qualityLowerBound = divisionBonus - 5;

            // 3/4 chance of failing even in range (line 313: RA=1+RND*4; if RA>1 skip)
            bool qualityCheck =
                1 + rng.Next(4) == 1
                && scout.LookingForForm >= qualityLowerBound
                && scout.LookingForForm <= divisionBonus
                && foundSkill >= 1;

            if (!qualityCheck)
            {
                gameState.Squad[scoutSquadSlot] = null;
                continue;
            }

            // ── Generate the discovered player (line 314) ─────────────────────
            var discoveredPlayer = new Player
            {
                Name       = NameGenerationService.GenerateName(rng),
                Position   = scout.LookingForPosition,
                Skill      = foundSkill,
                Age        = 22 + rng.Next(14),          // G(I)=22+RND*14
                GamesPlayed = scout.AssignedTeamIndex,    // x(I)=Y(3,I-18) = team index
                Temper     = Math.Max(0, Math.Min(9, -3 + rng.Next(17)))
            };
            PlayerService.RecalculateStatus(discoveredPlayer);

            // Negate scout salary as a flag that this slot is occupied (line 314: Y(1,I-18) = −Y(1,I-18))
            // In C# we track this through the Squad array presence instead.
            gameState.Squad[scoutSquadSlot] = discoveredPlayer;

            findings.Add(new ScoutFinding
            {
                ScoutIndex      = scoutIndex,
                ScoutName       = scout.Name.Trim(),
                SquadSlot       = scoutSquadSlot,
                Player          = discoveredPlayer,
                SourceClubIndex = scout.AssignedTeamIndex,
                SourceClubName  = gameState.AllTeamNames[scout.AssignedTeamIndex].Trim()
            });
        }

        return findings;
    }

    // ── Clear all scout-market slots ─────────────────────────────────────────

    /// <summary>
    /// Clears the three scout-market slots (21–23).
    /// Called when starting a new season or after all targets have been actioned.
    /// </summary>
    public static void ClearScoutMarket(GameState gameState)
    {
        for (int slot = 21; slot <= 23; slot++)
            gameState.Squad[slot] = null;
    }
}

// ── Data classes ─────────────────────────────────────────────────────────────

/// <summary>A player discovered by a scout this week.</summary>
public class ScoutFinding
{
    public int    ScoutIndex      { get; set; }
    public string ScoutName       { get; set; } = string.Empty;
    public int    SquadSlot       { get; set; }   // 21, 22, or 23
    public Player Player          { get; set; } = new();
    public int    SourceClubIndex { get; set; }
    public string SourceClubName  { get; set; } = string.Empty;
}
