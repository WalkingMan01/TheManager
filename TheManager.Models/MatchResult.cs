namespace TheManager.Models;

/// <summary>The full outcome of a played match, returned by GameService.PlayMatch.</summary>
public class MatchResult
{
    /// <summary>True when the week triggered end-of-season processing rather than a playable fixture.</summary>
    public bool WasEndOfSeason { get; set; }

    public string OurClubName  { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public bool   IsHomeGame   { get; set; }
    public int    OurScore     { get; set; }
    public int    TheirScore   { get; set; }

    /// <summary>Actual match duration in minutes (90–93). Used to drive the UI clock.</summary>
    public int MatchLength { get; set; } = 90;

    /// <summary>All goal events in chronological order.</summary>
    public List<MatchGoal> Goals { get; set; } = new();

    /// <summary>Other league fixtures played in the same week (empty for cup matches).</summary>
    public List<OtherFixtureResult> OtherFixtures { get; set; } = new();
}
