namespace TheManager.Models;

/// <summary>
/// Represents a single player. Maps to the parallel arrays V$, T, H, G, x
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
    private double _skill;

    // ── Identity ─────────────────────────────────────────────────────────────

    /// <summary>Name, max 8 characters. Corresponds to V$(I).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Playing position. Corresponds to T(I).</summary>
    public PlayerPosition Position { get; set; }

    // ── Skill / Age ───────────────────────────────────────────────────────────

    private double _potentialSkill = 9.9;

    /// <summary>
    /// Hidden ceiling on Skill — the best this player can ever become (1.1–9.9).
    /// Never drops once assigned and is never displayed in the UI. No FOOT.BAS
    /// equivalent (new mechanic); the youth analogue is Y(3,I) /
    /// <see cref="YouthPlayer.PotentialSkillPercent"/>.
    /// Defaults to 9.9 (uncapped) so pre-existing save files load unchanged.
    /// NOTE: declared before Skill so JSON deserialisation assigns it first.
    /// </summary>
    public double PotentialSkill
    {
        get => _potentialSkill;
        set => _potentialSkill = Math.Clamp(value, 1.1, 9.9);
    }

    /// <summary>
    /// Hidden age (26–30) at which the player is expected to reach their peak;
    /// sizes the headroom rolled at creation. Ages 26–30 are all peak years —
    /// decline only begins past 30. Never displayed. No FOOT.BAS equivalent.
    /// 0 = not yet assigned (legacy save) — assigned on load.
    /// </summary>
    public int PeakAge { get; set; }

    /// <summary>
    /// Skill rating 1.0–9.9+. Star players have Skill > 9.7 (status = Star, J=105).
    /// Capped at the player's hidden <see cref="PotentialSkill"/>.
    /// Corresponds to H(I).
    /// </summary>
    public double Skill
    {
        get => _skill;
        set
        {
            _skill = Math.Clamp(value, 1.1, _potentialSkill);
        }
    }

    /// <summary>
    /// Age in years (positive = settled, negative = transfer-listed).
    /// Display should always show Math.Abs(Age). Corresponds to G(I).
    /// </summary>
    public int Age { get; set; }

    /// <summary>Absolute age for display purposes.</summary>
    public int DisplayAge => Math.Abs(Age);

    /// <summary>True if the player is transfer-listed. Corresponds to G(I)&lt;0.</summary>
    public bool IsTransferListed => Age < 0;

    // ── Performance statistics ────────────────────────────────────────────────

    /// <summary>Temper rating 0–9. Low = calm, high = volatile. Corresponds to E(3,I).</summary>
    public int Temper { get; set; }

    /// <summary>
    /// Goals scored this season (outfield) or goals conceded (GK).
    /// Corresponds to E(1,I).
    /// </summary>
    public int SeasonGoals { get; set; }

    /// <summary>True if the player has announced retirement and will be removed at the end of the season.</summary>
    public bool IsRetiring { get; set; }

    /// <summary>Appearances this season. Corresponds to E(2,I).</summary>
    public int Appearances { get; set; }

    // ── Contract ─────────────────────────────────────────────────────────────

    /// <summary>Weekly wage in pounds. Corresponds to V(1,I) in FOOT.BAS.</summary>
    public double WeeklyWage { get; set; }

    /// <summary>Contract weeks remaining (counts down each week). Corresponds to V(2,I) in FOOT.BAS.</summary>
    public int ContractWeeks { get; set; }

    // ── Discipline / availability ────────────────────────────────────────────

    /// <summary>Weeks remaining before an injured player is available again. Corresponds to u(I) in FOOT.BAS.</summary>
    public int WeeksInjured { get; set; }

    /// <summary>Matches remaining of a suspension (red card or accumulated yellow cards).</summary>
    public int SuspensionMatchesRemaining { get; set; }

    /// <summary>Yellow cards picked up this season. Resets to 0 at season start and whenever it reaches 5 (triggering a suspension).</summary>
    public int YellowCardsThisSeason { get; set; }

    /// <summary>True if the player is fit and not serving a suspension.</summary>
    public bool IsAvailable => WeeksInjured == 0 && SuspensionMatchesRemaining == 0;

    // ── Misc ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Games played this season / source-club index when player is a transfer target.
    /// Corresponds to x(I).
    /// </summary>
    public int GamesPlayed { get; set; }

    /// <summary>
    /// Quoted transfer fee while the player sits in a transfer-market slot (21–23).
    /// Rolled once when the player is scouted so the fee shown in reports matches
    /// the fee paid; cleared when a deal is committed. 0 = not quoted.
    /// No FOOT.BAS equivalent (the original rolled the price at purchase time).
    /// </summary>
    public double AskingPrice { get; set; }

    // ── Derived helpers ───────────────────────────────────────────────────────

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
