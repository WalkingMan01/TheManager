namespace TheManager.Models;

/// <summary>Domain-wide constants shared across all layers.</summary>
public static class Constants
{
    /// <summary>League fixtures per season in Division One (20 teams). Corresponds to cJ=38 in FOOT.BAS.</summary>
    public const int WeeksInSeason = 38;

    /// <summary>League fixtures per season in Divisions Two–Four (24 teams: 23 opponents × 2 = 46).</summary>
    public const int WeeksInSeasonLargeDivision = 46;

    /// <summary>Returns the number of teams in the given division.</summary>
    public static int TeamCount(Division division)
        => division == Division.One ? 20 : 24;

    /// <summary>Returns the number of league fixtures in a full season for the given division.</summary>
    public static int FixturesInSeason(Division division)
        => division == Division.One ? WeeksInSeason : WeeksInSeasonLargeDivision;

    /// <summary>
    /// Returns the inclusive [Start, End] team-index range within AllTeamNames for the given division.
    /// Div 1 = [1–20], Div 2 = [21–44], Div 3 = [45–68], Div 4 = [69–92].
    /// </summary>
    public static (int Start, int End) DivisionRange(Division division) => division switch
    {
        Division.One   => (1,  20),
        Division.Two   => (21, 44),
        Division.Three => (45, 68),
        Division.Four  => (69, 92),
        _ => throw new ArgumentOutOfRangeException(nameof(division))
    };
}
