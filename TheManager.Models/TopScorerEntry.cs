namespace TheManager.Models;

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
