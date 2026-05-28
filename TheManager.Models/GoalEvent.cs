namespace TheManager.Models;

/// <summary>A single timed goal event.</summary>
public class GoalEvent
{
    /// <summary>Minute the goal is scored. Corresponds to B(I) in FOOT.BAS.</summary>
    public int Minute  { get; set; }

    /// <summary>True if scored by our team; false if by the opponent. Corresponds to c(I).</summary>
    public bool IsOurGoal { get; set; }
}
