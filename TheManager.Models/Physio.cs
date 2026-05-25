namespace TheManager.Models;

/// <summary>
/// Club physio. Reduces player injury duration.
/// The physio's SkillPercent reduces weeks injured (see line 4657 in FOOT.BAS:
/// reduction = INT(RND*32) adjusted by (60/100)*Y(2,2) skill factor).
/// Present when NM=1 in FOOT.BAS.
/// </summary>
public class Physio : StaffMember { }
