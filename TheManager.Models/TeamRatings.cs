namespace TheManager.Models;

/// <summary>
/// Positional strength ratings computed from the starting eleven.
/// Populated by <see cref="TheManager.Services.PlayerService.CalculateTeamRatings"/>.
/// </summary>
public class TeamRatings
{
    /// <summary>Goalkeeper integer skill (BA). Range 0–9.</summary>
    public int GoalkeeperRating { get; set; }

    /// <summary>Defence rating after divisor and cap (bc). Range 0–9.</summary>
    public int DefenceRating { get; set; }

    /// <summary>Midfield rating after divisor and cap (bb). Range 0–9.</summary>
    public int MidRating { get; set; }

    /// <summary>Attack rating after divisor and cap (bd). Range 0–9.</summary>
    public int AttackRating { get; set; }

    /// <summary>
    /// Formation code: hundreds = defenders, tens = midfielders, units = attackers
    /// (e.g. 442 = 4 defenders, 4 midfielders, 2 attackers). Corresponds to mu.
    /// </summary>
    public int FormationCode { get; set; }

    /// <summary>Aggregate temper of starting eleven. Corresponds to pu.</summary>
    public int TeamTemper { get; set; }
}
