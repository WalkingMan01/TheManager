namespace TheManager.Models;

/// <summary>A single cup fixture (one tie between two clubs).</summary>
public class CupFixture
{
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public Division HomeDivision { get; set; }
    public Division AwayDivision { get; set; }

    /// <summary>Null until the match has been played.</summary>
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }

    /// <summary>True when this is a replay after a draw. Corresponds to dt flag.</summary>
    public bool IsReplay { get; set; }
}
