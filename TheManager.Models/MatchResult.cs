namespace TheManager.Models;

/// <summary>The full outcome of a played match, returned by GameService.PlayMatch.</summary>
public class MatchResult
{
    /// <summary>True when the week triggered end-of-season processing rather than a playable fixture.</summary>
    public bool WasEndOfSeason { get; set; }

    public string OurClubName  { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public bool   IsHomeGame   { get; set; }
    public int    OurScore     { get; set; }
    public int    TheirScore   { get; set; }

    /// <summary>The matchday this result was produced for — used to tell a two-legged
    /// tie's second leg apart from its first (e.g. play-off semi-finals).</summary>
    public int Week { get; set; }

    /// <summary>Actual match duration in minutes (90–93). Used to drive the UI clock.</summary>
    public int MatchLength { get; set; } = 90;

    /// <summary>All goal events in chronological order.</summary>
    public List<MatchGoal> Goals { get; set; } = new();

    /// <summary>Other league fixtures played in the same week (empty for cup matches).</summary>
    public List<OtherFixtureResult> OtherFixtures { get; set; } = new();

    /// <summary>Players discovered by scouts during this week's report pass.</summary>
    public List<ScoutFinding> ScoutFindings { get; set; } = new();

    /// <summary>
    /// Players whose contracts expired this week, held for a last-chance renewal offer.
    /// The UI removes non-signers from the squad; this list is empty when ManagerSacked is true.
    /// </summary>
    public List<(int Slot, Player Player)> ExpiringPlayers { get; set; } = new();

    /// <summary>Cards and injuries that occurred during the match, in chronological order.</summary>
    public List<MatchIncident> Incidents { get; set; } = new();

    /// <summary>
    /// Itemised weekly finance breakdown from this week's tick. Null only when
    /// the week ended the season instead of running a tick.
    /// </summary>
    public WeeklyReport? FinanceReport { get; set; }

    /// <summary>Suspensions newly imposed this match (red card or 5th yellow card).</summary>
    public List<SuspensionNotice> NewSuspensions { get; set; } = new();

    // ── Rest days & cup ───────────────────────────────────────────────────────

    /// <summary>
    /// True when the matchday had no fixture for us (Division One league gap, or
    /// a cup matchday after elimination). Only the weekly tick ran.
    /// </summary>
    public bool WasRestDay { get; set; }

    /// <summary>The match type this result was produced for.</summary>
    public MatchType MatchType { get; set; } = MatchType.League;

    /// <summary>Cup round display name (e.g. "Round 3", "Semi Final") for cup matchdays.</summary>
    public string? CupRoundName { get; set; }

    /// <summary>Penalty shootout, kick by kick. Null unless the cup tie was drawn.</summary>
    public PenaltyShootoutResult? Shootout { get; set; }

    /// <summary>All completed ties from this cup round (including ours), for the news section.</summary>
    public List<CupFixture> CupResults { get; set; } = new();

    /// <summary>True when this cup tie is played at Wembley (semi-final or final).</summary>
    public bool IsNeutralVenue { get; set; }

    // ── Sacking ───────────────────────────────────────────────────────────────

    /// <summary>True when the manager was sacked this week due to poor form.</summary>
    public bool ManagerSacked { get; set; }

    /// <summary>The reason given for the sacking. Non-null when <see cref="ManagerSacked"/> is true.</summary>
    public string? SackingReason { get; set; }

    /// <summary>The new club the manager has joined. Non-null when <see cref="ManagerSacked"/> is true.</summary>
    public string? NewClubName { get; set; }

    /// <summary>The division of the new club. Non-null when <see cref="ManagerSacked"/> is true.</summary>
    public Division? NewClubDivision { get; set; }
}
