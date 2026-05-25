namespace TheManager.Models;

/// <summary>
/// Postponed fixture pair. Up to 18 stored at once.
/// Corresponds to N(2,18) in FOOT.BAS.
/// </summary>
public class PostponedFixture
{
    /// <summary>Index into Y$ for the home team. Corresponds to N(1,I).</summary>
    public int HomeTeamIndex { get; set; }

    /// <summary>Index into Y$ for the away team. Corresponds to N(2,I).</summary>
    public int AwayTeamIndex { get; set; }
}
