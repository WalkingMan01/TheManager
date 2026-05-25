namespace TheManager.Models;

/// <summary>Top 4 scorers for a single division.</summary>
public class DivisionTopScorers
{
    public Division Division { get; set; }

    /// <summary>Up to 4 entries, sorted descending by goals.</summary>
    public List<TopScorerEntry> Scorers { get; set; } = new(4);
}
