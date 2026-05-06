namespace FootballBoss.Models;

/// <summary>Result of <see cref="FootballBoss.Services.MatchEngine.SetupMatch"/>.</summary>
public class MatchSimulation
{
    public int             MatchLength        { get; set; }
    public List<GoalEvent> GoalEvents         { get; set; } = new();
    public int             IncidentMinute     { get; set; }   // 0 = no incident
    public int             OurGoalCount       { get; set; }
    public int             OpponentGoalCount  { get; set; }
}
