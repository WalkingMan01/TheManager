namespace TheManager.Models;

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
