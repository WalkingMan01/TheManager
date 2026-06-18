namespace TheManager.Models;

/// <summary>Result of <see cref="TheManager.Services.MatchEngine.SetupMatch"/>.</summary>
public class MatchSimulation
{
    public int             MatchLength        { get; set; }
    public List<GoalEvent> GoalEvents         { get; set; } = new();
    public int             IncidentMinute     { get; set; }   // 0 = no incident
    public int             OurGoalCount       { get; set; }
    public int             OpponentGoalCount  { get; set; }

    /// <summary>Slot/minute pairs at which a starting player (1-12) picks up a yellow card.</summary>
    public List<YellowCardEvent> YellowCardEvents { get; set; } = new();
}
