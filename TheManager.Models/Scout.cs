namespace TheManager.Models;

/// <summary>
/// Scout tasked with finding players matching a position and skill-tier target.
/// Up to 3 scouts allowed (NN=0–3). Corresponds to I$(3–5) / Y(1–5, 3–5).
/// </summary>
public class Scout : StaffMember
{
    /// <summary>Position the scout is looking for. Corresponds to Y(4,I).</summary>
    public PlayerPosition LookingForPosition { get; set; }

    /// <summary>
    /// Target skill tier (1–9) the scout will look for.
    /// Determines which division the scout searches in each week.
    /// Corresponds to Y(5,I).
    /// </summary>
    public int LookingForForm { get; set; }

    /// <summary>True once the player has set position and skill-tier criteria.</summary>
    public bool HasCriteria => LookingForPosition != PlayerPosition.None && LookingForForm > 0;
}
