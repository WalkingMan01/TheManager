namespace TheManager.Models;

/// <summary>
/// League standings for the player's division (20 teams in Div 1, 24 teams in Divs 2–4).
/// Sorted by the BASIC routine at line 3957 (insertion sort by points then GD then GF).
/// </summary>
public class LeagueTable
{
    public Division Division { get; set; }

    /// <summary>Entries in current standings order (20 for Div 1, 24 for Divs 2–4).</summary>
    public List<LeagueEntry> Entries { get; set; } = new(24);

    /// <summary>
    /// Result string for each league week, format "SR"
    /// where S = opponent score and R = our score (ASCII digits).
    /// Sized for the largest possible season (46 weeks). Corresponds to W$(I) in FOOT.BAS.
    /// </summary>
    public string[] WeeklyResults { get; set; } = new string[46];
}
