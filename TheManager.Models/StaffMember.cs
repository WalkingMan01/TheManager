namespace TheManager.Models;

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
