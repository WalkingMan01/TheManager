using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Processes weekly scout reports: each scout that has been given criteria (position
/// + skill tier) has a chance to identify a matching player and place them in the
/// transfer market slots (squad slots 21–23).
///
/// Corresponds to subroutine 312 in FOOT.BAS (lines 327–368).
/// </summary>
public static class ScoutReportService
{
    /// <summary>
    /// Runs a weekly report pass for all three scout slots.
    ///
    /// For each scout with criteria set:
    ///   Preserves any pending find already in the slot (not overwritten until actioned).
    ///   1/9 chance of no find this week.
    ///   Success probability scales with scout skill: 10% (skill 1) to 50% (skill 99).
    ///   On success: generate a player of the target skill level in squad slot 21/22/23.
    /// </summary>
    public static List<ScoutFinding> RunWeeklyReports(GameState gameState, Random rng)
    {
        var findings = new List<ScoutFinding>();

        for (int scoutSquadSlot = 21; scoutSquadSlot <= 23; scoutSquadSlot++)
        {
            int scoutListIdx = scoutSquadSlot - 21;   // 0, 1, 2

            if (scoutListIdx >= gameState.Scouts.Count) continue;
            var scout = gameState.Scouts[scoutListIdx];

            if (!scout.HasCriteria)
            {
                gameState.Squad[scoutSquadSlot] = null;
                continue;
            }

            // Preserve a pending find until the player actions or dismisses it
            if (gameState.Squad[scoutSquadSlot] is not null) continue;

            // 1/9 chance of no find this week
            if (1 + rng.Next(9) == 1) continue;

            // Scout skill (1–99 %) raises success probability from ~10 % to ~50 %
            int successThreshold = 10 + scout.SkillPercent * 40 / 100;
            if (rng.Next(100) >= successThreshold) continue;

            // Pick a team from the division that matches the target skill tier
            int targetDiv = SkillTierToDivision(scout.LookingForForm);
            int teamIndex = (targetDiv - 1) * 20 + 1 + rng.Next(20);
            if (teamIndex < 1 || teamIndex >= gameState.AllTeamNames.Length) continue;
            string sourceClub = gameState.AllTeamNames[teamIndex];
            if (string.IsNullOrWhiteSpace(sourceClub)) continue;

            // Skill found is within ±1 of the target tier, with a 0.0–0.4 decimal fraction
            double foundSkill = (scout.LookingForForm - 1) + rng.Next(3);
            foundSkill = Math.Max(1, Math.Min(9, foundSkill));
            foundSkill = foundSkill + rng.Next(5) / 10.0;

            var discoveredPlayer = new Player
            {
                Name        = NameGenerationService.GenerateName(rng),
                Position    = scout.LookingForPosition,
                Skill       = foundSkill,
                Age         = 22 + rng.Next(14),
                GamesPlayed = teamIndex,    // source-club index stored here per BASIC convention
                Temper      = Math.Max(0, Math.Min(9, -3 + rng.Next(17)))
            };
            PlayerService.RecalculateStatus(discoveredPlayer);

            gameState.Squad[scoutSquadSlot] = discoveredPlayer;

            findings.Add(new ScoutFinding
            {
                ScoutIndex      = scoutListIdx,
                ScoutName       = scout.Name.Trim(),
                SquadSlot       = scoutSquadSlot,
                Player          = discoveredPlayer,
                SourceClubIndex = teamIndex,
                SourceClubName  = sourceClub.Trim()
            });
        }

        return findings;
    }

    // ── Clear all scout-market slots ─────────────────────────────────────────

    /// <summary>
    /// Clears the three scout-market slots (21–23).
    /// Called at the start of a new season.
    /// </summary>
    public static void ClearScoutMarket(GameState gameState)
    {
        for (int slot = 21; slot <= 23; slot++)
            gameState.Squad[slot] = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Maps a 1–9 skill tier to the division most likely to contain that quality of player.</summary>
    private static int SkillTierToDivision(int form) => form switch
    {
        >= 7 => 1,
        >= 5 => 2,
        >= 3 => 3,
        _    => 4
    };
}
