namespace TheManager.Models;

/// <summary>
/// Tracks an in-progress two-legged promotion semi-final plus single-match final
/// for the division the player's club is playing in. No FOOT.BAS equivalent — see
/// docs/specs/promotion-playoffs.md.
/// </summary>
public class PlayoffState
{
    /// <summary>True once the play-off has been set up for this season (after matchday 54).</summary>
    public bool Active { get; set; }

    /// <summary>True if the player's club finished in the play-off field and is contesting it.</summary>
    public bool PlayerInvolved { get; set; }

    /// <summary>True if the player's club is the higher seed of its semi-final (hosts leg 2,
    /// the decisive leg). False means the player hosts leg 1 and travels for leg 2.</summary>
    public bool PlayerIsHigherSeed { get; set; }

    /// <summary>Our goals in leg 1. Null until leg 1 has been played.</summary>
    public int? FirstLegOurScore { get; set; }

    /// <summary>Opponent goals in leg 1. Null until leg 1 has been played.</summary>
    public int? FirstLegTheirScore { get; set; }

    /// <summary>
    /// The winner of the semi-final not involving the player's club, resolved
    /// immediately (as a single aggregate result — see <see cref="PlayoffService"/>)
    /// once the final table is known. Opponent in the final if the player wins their semi.
    /// </summary>
    public string OtherSemiFinalWinner { get; set; } = string.Empty;

    /// <summary>Name of the club promoted via the play-off, once decided.</summary>
    public string Winner { get; set; } = string.Empty;

    public bool IsResolved => !string.IsNullOrEmpty(Winner);
}
