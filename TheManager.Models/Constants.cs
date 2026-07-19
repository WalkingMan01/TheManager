namespace TheManager.Models;

/// <summary>Domain-wide constants shared across all layers.</summary>
public static class Constants
{
    /// <summary>League fixtures per season in Division One (20 teams). Corresponds to cJ=38 in FOOT.BAS.</summary>
    public const int WeeksInSeason = 38;

    /// <summary>League fixtures per season in Divisions Two–Four (24 teams: 23 opponents × 2 = 46).</summary>
    public const int WeeksInSeasonLargeDivision = 46;

    /// <summary>
    /// Total matchdays in a season, all divisions. The FA Cup Final falls on the
    /// last matchday. Extends the CI=1–59 cycle in FOOT.BAS.
    /// </summary>
    public const int SeasonMatchdays = 54;

    /// <summary>
    /// FA Cup matchdays: eight rounds, evenly spaced, final on the season's last
    /// matchday. R1=12, R2=18, R3=24, R4=30, R5=36, QF=42, SF=48, Final=54.
    /// Replaces the CI = 16,23,…,58 cup slots in FOOT.BAS.
    /// </summary>
    public static readonly int[] FACupMatchdays = [12, 18, 24, 30, 36, 42, 48, 54];

    /// <summary>Returns the number of teams in the given division.</summary>
    public static int TeamCount(Division division)
        => division == Division.One ? 20 : 24;

    /// <summary>Returns the number of league fixtures in a full season for the given division.</summary>
    public static int FixturesInSeason(Division division)
        => division == Division.One ? WeeksInSeason : WeeksInSeasonLargeDivision;

    // ── Ground capacity & attendance (docs/specs/gate-receipts-ground-capacity.md) ─

    /// <summary>±10% jitter applied to the fallback ground capacity at seed time.</summary>
    public const double GroundCapacityJitterFraction = 0.10;

    /// <summary>Occupancy ceiling for the club in 1st place (100%), all divisions.</summary>
    public const double OccupancyCeiling = 1.00;

    /// <summary>Width of the per-position occupancy band (2 points), all divisions.</summary>
    public const double OccupancyBandWidth = 0.02;

    /// <summary>Occupancy bump for a home cup tie (+2%, capped at 100%), all divisions.</summary>
    public const double OccupancyCupBump = 0.02;

    /// <summary>
    /// Occupancy ceiling for the club in last place. The ceiling slides linearly
    /// from <see cref="OccupancyCeiling"/> at 1st to this value at the bottom.
    /// Division One bottoms out at 90.5% (band 88.5–90.5%); lower divisions at
    /// 62% (band 60–62%) — attendance never drops below 60% of capacity.
    /// </summary>
    public static double OccupancyCeilingAtBottom(Division division)
        => division == Division.One ? 0.905 : 0.62;

    /// <summary>Attendance at or above this fraction of capacity counts as a sell-out
    /// (away fans don't always fill their allocation).</summary>
    public const double SellOutFraction = 0.99;

    /// <summary>
    /// Fallback ground capacity for a club name not in <see cref="TeamData"/>'s
    /// real-ground table, near each division's real-world median.
    /// </summary>
    public static int FallbackGroundCapacity(Division division) => division switch
    {
        Division.One   => 30_000,
        Division.Two   => 22_000,
        Division.Three => 12_000,
        _              => 8_000
    };

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
