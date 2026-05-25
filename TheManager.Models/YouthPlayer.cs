namespace TheManager.Models;

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
