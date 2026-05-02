namespace FootballBoss.Models;

/// <summary>
/// Represents a single player. Maps to the parallel arrays V$, T, H, G, J, E, V, u, x
/// indexed 1-28 in FOOT.BAS.
///
/// Squad slot conventions (BASIC array index):
///   1-11  = first team (1=GK, 2-5=DEF, 6-8=MID, 9-11=ATK)
///   12    = substitute
///   13-20 = reserves
///   21-23 = transfer targets (clubs are selling)
///   24-26 = players other clubs want from us
///   27-28 = temporary slots used during negotiation
/// </summary>
public class Player
{
    // ── Identity ─────────────────────────────────────────────────────────────

    /// <summary>Name, max 8 characters. Corresponds to V$(I).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Playing position. Corresponds to T(I).</summary>
    public PlayerPosition Position { get; set; }

    // ── Skill / Age ───────────────────────────────────────────────────────────

    /// <summary>
    /// Skill rating 1.0–9.9+. Star players have Skill > 9.7 (status = Star, J=105).
    /// Corresponds to H(I).
    /// </summary>
    public double Skill { get; set; }

    /// <summary>
    /// Age in years (positive = settled, negative = transfer-listed).
    /// Display should always show Math.Abs(Age). Corresponds to G(I).
    /// </summary>
    public int Age { get; set; }

    /// <summary>True when the player has requested a transfer (Age stored negative).</summary>
    public bool IsTransferListed => Age < 0;

    /// <summary>Absolute age for display purposes.</summary>
    public int DisplayAge => Math.Abs(Age);

    // ── Status ────────────────────────────────────────────────────────────────

    /// <summary>Availability status. Corresponds to J(I) ASCII codes.</summary>
    public PlayerStatus Status { get; set; }

    /// <summary>
    /// Weeks remaining for injury, suspension, loan, or unavailability.
    /// Corresponds to u(I).
    /// </summary>
    public int WeeksUnavailable { get; set; }

    // ── Performance statistics ────────────────────────────────────────────────

    /// <summary>
    /// Goals scored this season for outfield players.
    /// For goalkeepers this counter increments when the opposition scores
    /// (effectively goals conceded while on pitch). Corresponds to E(1,I).
    /// Displayed in yellow for goalkeepers in the injuries/stats screen.
    /// </summary>
    public int Goals { get; set; }

    /// <summary>Appearances this season. Corresponds to E(2,I).</summary>
    public int Appearances { get; set; }

    /// <summary>Temper rating 0–9. Low = calm, high = volatile. Corresponds to E(3,I).</summary>
    public int Temper { get; set; }

    // ── Contract / Wages ──────────────────────────────────────────────────────

    /// <summary>Weekly wage in pounds. Corresponds to V(1,I).</summary>
    public double WeeklyWage { get; set; }

    /// <summary>Weeks remaining on current contract. Corresponds to V(2,I).</summary>
    public int ContractWeeksRemaining { get; set; }

    // ── Misc ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Games played this season / source-club index when player is a transfer target.
    /// Corresponds to x(I).
    /// </summary>
    public int GamesPlayed { get; set; }

    // ── Derived helpers ───────────────────────────────────────────────────────

    public bool IsAvailable =>
        Status is PlayerStatus.Normal
                or PlayerStatus.Improving
                or PlayerStatus.Recovering
                or PlayerStatus.Star;

    public bool IsStar => Skill > 9.7;

    /// <summary>Integer skill displayed to the user. Corresponds to INT(H(I)).</summary>
    public int DisplaySkill => (int)Skill;

    /// <summary>
    /// Contribution to the team's positional strength rating used in the match engine.
    /// Divisors come from BASIC lines 381–386: DEF/3.9, MID/2.9, ATK/2.9, all capped at 9.
    /// </summary>
    public double PositionalStrengthContribution => Position switch
    {
        PlayerPosition.Goalkeeper => DisplaySkill,          // BA = INT(H(1))
        PlayerPosition.Defender   => Skill / 3.9,           // bc before cap
        PlayerPosition.Midfielder => Skill / 2.9,           // bb before cap
        PlayerPosition.Attacker   => Skill / 2.9,           // bd before cap
        _                         => 0
    };
}
