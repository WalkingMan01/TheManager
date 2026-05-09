namespace FootballBoss.Models;

/// <summary>
/// Base class for all non-player staff. Maps to I$(N) name + Y(1-6, N) attributes
/// in FOOT.BAS.
///
/// Staff index layout in BASIC:
///   1 = Coach      (NL flag)
///   2 = Physio     (NM flag)
///   3-5 = Scouts   (NN count, max 3)
///   6-12 = Youth players (NO count, max 7)
/// </summary>
public abstract class StaffMember
{
    /// <summary>Name (up to 8 characters). Corresponds to I$(I).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Weekly salary in pounds. Corresponds to Y(1,I).</summary>
    public double WeeklySalary { get; set; }

    /// <summary>
    /// Skill / effectiveness percentage 1–99.
    /// Corresponds to Y(2,I).
    /// </summary>
    public int SkillPercent { get; set; }
}

/// <summary>
/// First-team coach. Improves training effectiveness.
/// Present when NL=1 in FOOT.BAS.
/// </summary>
public class Coach : StaffMember { }

/// <summary>
/// Club physio. Reduces player injury duration.
/// The physio's SkillPercent reduces weeks injured (see line 4657 in FOOT.BAS:
/// reduction = INT(RND*32) adjusted by (60/100)*Y(2,2) skill factor).
/// Present when NM=1 in FOOT.BAS.
/// </summary>
public class Physio : StaffMember { }

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

    /// <summary>
    /// Position the scout is looking for. Corresponds to Y(4,I).
    /// </summary>
    public PlayerPosition LookingForPosition { get; set; }

    /// <summary>
    /// Minimum current-form rating the scout will flag (1–9).
    /// Corresponds to Y(5,I).
    /// </summary>
    public int LookingForForm { get; set; }

    public bool IsAssigned => AssignedTeamIndex > 0;
}

/// <summary>
/// Youth team player. Up to 7 allowed (NO=0–7).
/// Corresponds to I$(6–12) / Y(1–6, 6–12).
/// Progress is tracked until SkillPercent reaches PotentialSkillPercent,
/// at which point the coach flags them as fully developed.
/// </summary>
public class YouthPlayer : StaffMember
{
    // Note: inherited SkillPercent (Y(2,I)) = current ability, 0–59 initially

    /// <summary>
    /// Maximum potential (35–99). When SkillPercent reaches this value the
    /// youth player is as good as they will get. Corresponds to Y(3,I).
    /// </summary>
    public int PotentialSkillPercent { get; set; }

    /// <summary>Preferred playing position. Corresponds to Y(4,I).</summary>
    public PlayerPosition Position { get; set; }

    /// <summary>Age in years (typically 16–18). Corresponds to Y(5,I).</summary>
    public int Age { get; set; }

    /// <summary>Temper rating 0–9. Corresponds to Y(6,I).</summary>
    public int Temper { get; set; }

    /// <summary>
    /// True when the player has reached their maximum potential.
    /// Triggers the "as good as possible" message in the youth screen.
    /// </summary>
    public bool HasReachedPotential => SkillPercent >= PotentialSkillPercent;

    /// <summary>
    /// Player must be at or above 50% skill to be promoted to the first team
    /// (see line 4998 in FOOT.BAS: IF Y(2,HC+5) &lt; 50 THEN 4901).
    /// </summary>
    public bool IsEligibleForPromotion => SkillPercent >= 50;
}
