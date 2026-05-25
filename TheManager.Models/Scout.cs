namespace TheManager.Models;

/// <summary>
/// Scout assigned to watch a specific team.
/// Up to 3 scouts allowed (NN=0–3). Corresponds to I$(3–5) / Y(1–5, 3–5).
/// </summary>
public class Scout : StaffMember
{
    /// <summary>
    /// Index into the all-teams array (Y$) of the team being scouted.
    /// 0 means the scout has not been assigned. Corresponds to Y(3,I).
    /// </summary>
    public int AssignedTeamIndex { get; set; }

    /// <summary>Name of the team being watched (Y$(Y(3,I))).</summary>
    public string AssignedTeamName { get; set; } = string.Empty;

    /// <summary>Position the scout is looking for. Corresponds to Y(4,I).</summary>
    public PlayerPosition LookingForPosition { get; set; }

    /// <summary>
    /// Minimum current-form rating the scout will flag (1–9).
    /// Corresponds to Y(5,I).
    /// </summary>
    public int LookingForForm { get; set; }

    public bool IsAssigned => AssignedTeamIndex > 0;
}
