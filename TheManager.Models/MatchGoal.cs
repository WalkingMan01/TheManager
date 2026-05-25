namespace TheManager.Models;

/// <summary>A single goal with its minute, team, and optional scorer name.</summary>
public class MatchGoal
{
    public int Minute { get; set; }

    /// <summary>True = scored by the managed club; false = scored by the opponent.</summary>
    public bool IsOurGoal { get; set; }

    /// <summary>Player name when IsOurGoal is true; null when the opponent scored.</summary>
    public string? Scorer { get; set; }
}
