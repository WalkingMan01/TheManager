namespace TheManager.Models;

/// <summary>Result of a simulated fixture for another division team on the same match day.</summary>
public class OtherFixtureResult
{
    public string HomeTeam  { get; set; } = string.Empty;
    public int    HomeScore { get; set; }
    public string AwayTeam  { get; set; } = string.Empty;
    public int    AwayScore { get; set; }
}
