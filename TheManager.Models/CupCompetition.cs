namespace TheManager.Models;

/// <summary>
/// State of either the League Cup (J=1) or FA Cup (J=3) for the current season.
///
/// Rounds map to OJ (League Cup) or OK (FA Cup) in FOOT.BAS:
///   1–5 = early rounds, 6 = QF, 7 = SF, 8 = Final, 9 = Winner.
///   0 = eliminated / not entered yet.
/// </summary>
public class CupCompetition
{
    public CupType Type { get; set; }

    /// <summary>
    /// Highest round the managed club has reached. 0 = out / not started.
    /// Corresponds to OJ (League Cup) or OK (FA Cup).
    /// </summary>
    public CupRound CurrentRound { get; set; }

    /// <summary>
    /// Fixtures for all ties in the current round (not just the player's tie).
    /// Populated by the draw routine (subroutine 1237 in FOOT.BAS).
    /// </summary>
    public List<CupFixture> CurrentRoundFixtures { get; set; } = new();

    /// <summary>
    /// Season fixture log (up to 8 entries per cup).
    /// Format: "H" or "A" + opponent name (9 chars) + div char + score chars.
    /// Corresponds to A$(CupIndex, I) in FOOT.BAS where CupIndex=1 for LC, 2 for FA.
    /// </summary>
    public string[] FixtureLog { get; set; } = new string[8];

    /// <summary>
    /// Internal round tracker used by the match engine for score recording.
    /// Corresponds to MT (League Cup) or MS (FA Cup).
    /// </summary>
    public int RoundTracker { get; set; }

    // ── Derived ───────────────────────────────────────────────────────────────

    public bool IsEliminated => CurrentRound == CupRound.NotEntered;
    public bool IsWinner     => CurrentRound == CupRound.Winner;

    public string RoundName => CurrentRound switch
    {
        CupRound.QuarterFinal => "Quarter Final",
        CupRound.SemiFinal    => "Semi Final",
        CupRound.Final        => "Final",
        CupRound.Winner       => "Winner",
        CupRound.NotEntered   => "—",
        _                     => $"Round {(int)CurrentRound}"
    };
}
