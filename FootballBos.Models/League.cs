namespace FootballBoss.Models;

/// <summary>
/// One team's row in the league standings.
/// Maps to the parallel arrays T$, P, W, D, F(1/2) in FOOT.BAS.
/// </summary>
public class LeagueEntry
{
    /// <summary>Team name. Corresponds to T$(N).</summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>Matches played. Corresponds to P(N).</summary>
    public int Played { get; set; }

    /// <summary>Wins. Corresponds to W(N).</summary>
    public int Won { get; set; }

    /// <summary>Draws. Corresponds to D(N).</summary>
    public int Drawn { get; set; }

    public int Lost => Played - Won - Drawn;

    /// <summary>Goals scored. Corresponds to F(1,N).</summary>
    public int GoalsFor { get; set; }

    /// <summary>Goals conceded. Corresponds to F(2,N).</summary>
    public int GoalsAgainst { get; set; }

    public int GoalDifference => GoalsFor - GoalsAgainst;

    public int Points(int pointsPerWin) => Won * pointsPerWin + Drawn;
}

/// <summary>
/// The 20-team league standings for the player's division.
/// Sorted by the BASIC routine at line 3957 (insertion sort by points then GD then GF).
/// </summary>
public class LeagueTable
{
    public Division Division { get; set; }

    /// <summary>20 entries in current standings order.</summary>
    public List<LeagueEntry> Entries { get; set; } = new(20);

    /// <summary>
    /// Result string for each of the 38 league weeks, format "SR"
    /// where S = opponent score and R = our score (ASCII digits).
    /// Corresponds to W$(I) in FOOT.BAS.
    /// </summary>
    public string[] WeeklyResults { get; set; } = new string[38];
}

/// <summary>
/// Top-scorer entry for one slot in the division top-4 chart.
/// Maps to L$(J,K), I(1,J,K), I(2,J,K) in FOOT.BAS.
/// </summary>
public class TopScorerEntry
{
    /// <summary>Player name. Corresponds to L$(J,K).</summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>Goals scored. Corresponds to I(1,J,K).</summary>
    public int Goals { get; set; }

    /// <summary>Club team name. Corresponds to Y$(I(2,J,K)).</summary>
    public string ClubName { get; set; } = string.Empty;
}

/// <summary>
/// Top 4 scorers for a single division.
/// </summary>
public class DivisionTopScorers
{
    public Division Division { get; set; }

    /// <summary>Up to 4 entries, sorted descending by goals.</summary>
    public List<TopScorerEntry> Scorers { get; set; } = new(4);
}

/// <summary>
/// Postponed fixture pair. Up to 18 stored at once.
/// Corresponds to N(2,18) in FOOT.BAS.
/// </summary>
public class PostponedFixture
{
    /// <summary>Index into Y$ for the home team. Corresponds to N(1,I).</summary>
    public int HomeTeamIndex { get; set; }

    /// <summary>Index into Y$ for the away team. Corresponds to N(2,I).</summary>
    public int AwayTeamIndex { get; set; }
}
