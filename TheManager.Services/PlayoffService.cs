using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Builds and resolves the promotion play-off (Championship/League One: 3rd–6th;
/// League Two: 4th–7th) for the division the player's club is playing in.
///
/// No FOOT.BAS equivalent — the original predates the English play-off system.
/// See docs/specs/promotion-playoffs.md.
/// </summary>
public static class PlayoffService
{
    /// <summary>
    /// Builds the 4-team play-off field starting from the position directly below
    /// the automatic promotion spots (0-based index <paramref name="autoSpots"/>
    /// through <c>autoSpots + 3</c>): 3rd vs 6th / 4th vs 5th when
    /// <paramref name="autoSpots"/> is 2 (Championship, League One), or 4th vs 7th /
    /// 5th vs 6th when it's 3 (League Two). Each pair is
    /// <c>(HigherSeed, LowerSeed)</c> — not a fixed home/away, since a two-legged
    /// tie has both teams host a leg.
    /// </summary>
    public static (string HigherSeedA, string LowerSeedA, string HigherSeedB, string LowerSeedB) BuildSemiFinals(
        LeagueTable table, int autoSpots)
    {
        string higherA = table.Entries[autoSpots].TeamName.Trim();
        string lowerA  = table.Entries[autoSpots + 3].TeamName.Trim();
        string higherB = table.Entries[autoSpots + 1].TeamName.Trim();
        string lowerB  = table.Entries[autoSpots + 2].TeamName.Trim();

        return (higherA, lowerA, higherB, lowerB);
    }

    /// <summary>
    /// Read-only check for whether <paramref name="clubName"/> currently sits in
    /// one of the four play-off spots for <paramref name="division"/>, based on
    /// the (already sorted) final table. Mirrors the field-building logic in
    /// <c>GameService.SchedulePlayoffIfNeeded</c> without any side effects — used
    /// by the UI to preview, ahead of the "end of season" prompt, whether the
    /// club's season is actually about to continue into a play-off.
    /// </summary>
    public static bool IsClubInPlayoffContention(LeagueTable table, Division division, string clubName)
    {
        if (division == Division.One) return false;

        int autoSpots = Constants.AutomaticPromotionSpots(division);
        if (table.Entries.Count < autoSpots + 4) return false;

        var (higherA, lowerA, higherB, lowerB) = BuildSemiFinals(table, autoSpots);
        string name = clubName.Trim();

        return higherA.Equals(name, StringComparison.OrdinalIgnoreCase)
            || lowerA.Equals(name, StringComparison.OrdinalIgnoreCase)
            || higherB.Equals(name, StringComparison.OrdinalIgnoreCase)
            || lowerB.Equals(name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves a play-off tie not involving the player as a single result — used
    /// for (a) a semi-final the player isn't part of, standing in for the full two
    /// legs, and (b) the Wembley final, which really is single-match. Score
    /// weighted by the gap between the two teams' final league positions (closer
    /// positions = more even game), mirroring
    /// <see cref="LeagueService.SimulateOtherFixtures"/>'s position-weighted
    /// formula rather than <see cref="CupService.SimulateTie"/>'s division-gap
    /// formula (both teams are in the same division here). A level result goes to
    /// penalties, reusing <see cref="CupService.SimulateTie"/>'s shootout-odds
    /// shape (edge to the better-placed side).
    /// </summary>
    public static PlayoffTieResult SimulateTie(
        string homeTeam, int homePosition,
        string awayTeam, int awayPosition,
        Random rng)
    {
        int homeScore = Math.Max(0, 1 + rng.Next(8) + (homePosition < awayPosition ? 2 : 0) - 5);
        int awayScore = Math.Max(0, 1 + rng.Next(7) + (awayPosition < homePosition ? 2 : 0) - 5);

        var result = new PlayoffTieResult
        {
            HomeScore = homeScore,
            AwayScore = awayScore
        };

        if (homeScore != awayScore)
        {
            result.Winner = homeScore > awayScore ? homeTeam : awayTeam;
            return result;
        }

        // Level — decided on penalties. Slight edge to the better-placed side.
        int  homeChance   = Math.Clamp(50 + (awayPosition - homePosition) * 5, 5, 95);
        bool homeWinsPens = rng.Next(100) < homeChance;

        int winnerPens = 3 + rng.Next(3);                          // 3–5
        int loserPens  = Math.Max(0, winnerPens - 1 - rng.Next(2)); // 1–2 behind

        result.WonOnPenalties = true;
        result.HomePenalties  = homeWinsPens ? winnerPens : loserPens;
        result.AwayPenalties  = homeWinsPens ? loserPens : winnerPens;
        result.Winner         = homeWinsPens ? homeTeam : awayTeam;
        return result;
    }
}

/// <summary>Result of a single-match/aggregate play-off tie resolved by <see cref="PlayoffService.SimulateTie"/>.</summary>
public class PlayoffTieResult
{
    public string Winner   { get; set; } = string.Empty;
    public int    HomeScore { get; set; }
    public int    AwayScore { get; set; }
    public bool   WonOnPenalties { get; set; }
    public int?   HomePenalties  { get; set; }
    public int?   AwayPenalties  { get; set; }
}
