namespace FootballBoss.Models;

/// <summary>A single timed goal event.</summary>
public class GoalEvent
{
    /// <summary>Minute the goal is scored. Corresponds to B(I) in FOOT.BAS.</summary>
    public int Minute  { get; set; }

    /// <summary>1 = scored by us, 2 = scored by opponent. Corresponds to c(I).</summary>
    public int Scorer  { get; set; }
}
