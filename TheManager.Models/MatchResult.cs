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

    /// <summary>All goal events in chronological order.</summary>
    public List<MatchGoal> Goals { get; set; } = new();

    /// <summary>Other league fixtures played in the same week (empty for cup matches).</summary>
    public List<OtherFixtureResult> OtherFixtures { get; set; } = new();
}

/// <summary>A single goal with its minute, team, and optional scorer name.</summary>
public class MatchGoal
{
    public int     Minute    { get; set; }

    /// <summary>True = scored by the managed club; false = scored by the opponent.</summary>
    public bool    IsOurGoal { get; set; }

    /// <summary>Player name when IsOurGoal is true; null when the opponent scored.</summary>
    public string? Scorer    { get; set; }
}

/// <summary>Result of a simulated fixture for another division team on the same match day.</summary>
public class OtherFixtureResult
{
    public string HomeTeam  { get; set; } = string.Empty;
    public int    HomeScore { get; set; }
    public string AwayTeam  { get; set; } = string.Empty;
    public int    AwayScore { get; set; }
}
