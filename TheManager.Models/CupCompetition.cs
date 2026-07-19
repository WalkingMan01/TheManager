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
    /// <summary>Bracket capacity: the FA Cup first round holds 80 teams. Extends L(64) in FOOT.BAS.</summary>
    public const int BracketSize = 80;

    public CupType Type { get; set; }

    /// <summary>
    /// Highest round the managed club has reached. 0 = out / not started.
    /// Corresponds to OJ (League Cup) or OK (FA Cup).
    /// </summary>
    public CupRound CurrentRound { get; set; }

    /// <summary>
    /// 80-slot knockout bracket of team indices still in the competition
    /// (1-based; 0 = empty slot). Extends L(64) in FOOT.BAS.
    /// </summary>
    public int[] Bracket { get; set; } = new int[BracketSize + 1];

    /// <summary>
    /// Completed rounds, in order: every tie with its final score. The winners of
    /// the last entry (plus the round-3 entrants, before round 3) are the teams
    /// still in the competition. No FOOT.BAS equivalent — the original kept only
    /// the player's own fixture log (A$).
    /// </summary>
    public List<CupRoundRecord> RoundHistory { get; set; } = new();

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

/// <summary>One completed cup round: the round and all of its finished ties.</summary>
public class CupRoundRecord
{
    public CupRound Round { get; set; }
    public List<CupFixture> Results { get; set; } = new();
}
