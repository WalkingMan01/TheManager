namespace TheManager.Models;

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
