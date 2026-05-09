namespace TheManager.Models;

/// <summary>
/// State of the club's European campaign.
///
/// The internal gz variable in FOOT.BAS encodes both round and leg:
///   0       = not in European competition
///   3,5,7   = first leg of rounds 1, 2, semi-final
///   4,6,8   = second leg of rounds 1, 2, semi-final
///   9       = final (or penalty shoot-out resolution)
///   gz/2    = effective round number for display (JE)
///
/// be encodes which competition (0=none, 1=European, 2=UEFA, 3=CupWinners).
/// </summary>
public class EuropeanCompetition
{
    /// <summary>
    /// Which European competition the club is in. Corresponds to be.
    /// </summary>
    public EuropeanCompetitionType Type { get; set; }

    /// <summary>
    /// Internal round/leg counter. Odd = first leg, even = second leg.
    /// Range 3–9. Corresponds to gz.
    /// </summary>
    public int RoundState { get; set; }

    /// <summary>
    /// Aggregate goals scored in the current tie. Corresponds to jb.
    /// Reset each round.
    /// </summary>
    public int AggregateFor { get; set; }

    /// <summary>
    /// Aggregate goals conceded in the current tie. Corresponds to jc.
    /// </summary>
    public int AggregateAgainst { get; set; }

    // ── Derived helpers ───────────────────────────────────────────────────────

    public bool IsActive => Type != EuropeanCompetitionType.None && RoundState > 0;

    public bool IsFirstLeg => RoundState is 3 or 5 or 7;

    /// <summary>Display round number (1, 2, or 3 for SF). Corresponds to JE = INT(gz/2).</summary>
    public int DisplayRound =>
        RoundState == 9 ? 4 :                           // final
        (RoundState / 2) + (RoundState % 2 == 0 ? 0 : 0); // INT(gz/2)

    public string CompetitionName => Type switch
    {
        EuropeanCompetitionType.European   => "European Cup",
        EuropeanCompetitionType.UEFA       => "U.E.F.A. Cup",
        EuropeanCompetitionType.CupWinners => "European Cup Winners",
        _                                  => string.Empty
    };
}
