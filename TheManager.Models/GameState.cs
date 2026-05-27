namespace TheManager.Models;

/// <summary>
/// Root game state — everything needed to save/restore a game.
/// This is the top-level aggregate that maps to the full save file
/// written by the SAVEGAME routine in FOOT.BAS.
/// </summary>
public class GameState
{
    // ── Time / season tracking ────────────────────────────────────────────────

    /// <summary>Number of full seasons completed. Corresponds to ah.</summary>
    public int SeasonsPlayed { get; set; }

    /// <summary>
    /// Rolling season slot 1–10 (wraps after 10). Corresponds to ns.
    /// Used to index K(ns, *) season history.
    /// </summary>
    public int SeasonSlot { get; set; }

    /// <summary>
    /// Current fixture week index (1–59 in the fixture cycle).
    /// Corresponds to CI.
    /// </summary>
    public int CurrentWeek { get; set; }

    /// <summary>League matches remaining this season. Corresponds to cJ.</summary>
    public int MatchesRemainingThisSeason { get; set; }

    /// <summary>Total fixtures played so far. Corresponds to id.</summary>
    public int FixturesPlayed { get; set; }

    public IReadOnlyList<ScheduledMatch> Fixtures { get; set; } = new List<ScheduledMatch>();

    // ── Club / squad ──────────────────────────────────────────────────────────

    /// <summary>The managed club's properties.</summary>
    public Club Club { get; set; } = new();

    /// <summary>Club finances.</summary>
    public Finances Finances { get; set; } = new();

    /// <summary>
    /// 28-slot squad array mirroring the BASIC indexing:
    ///   [0]    = unused (BASIC is 1-based)
    ///   [1–11] = first team (1=GK, 2-5=DEF, 6-8=MID, 9-11=ATK)
    ///   [12]   = substitute
    ///   [13–20]= reserves
    ///   [21–23]= transfer targets (buying)
    ///   [24–26]= transfer targets (selling)
    ///   [27–28]= temporary negotiation slots
    /// </summary>
    public Player?[] Squad { get; set; } = new Player?[29];

    // ── Staff ─────────────────────────────────────────────────────────────────

    public Coach?  Coach  { get; set; }
    public Physio? Physio { get; set; }

    /// <summary>Up to 3 scouts. Corresponds to I$(3–5).</summary>
    public List<Scout> Scouts { get; set; } = new(3);

    /// <summary>Up to 7 youth players. Corresponds to I$(6–12).</summary>
    public List<YouthPlayer> YouthTeam { get; set; } = new(7);

    // ── League ────────────────────────────────────────────────────────────────

    /// <summary>
    /// All 96 team names across 4 divisions + 6 cup-only slots + reserve pool,
    /// plus Y$(97–106) for surname suffixes used in name generation.
    /// Corresponds to Y$(1–106).
    /// Div 1 = [1–20], Div 2 = [21–40], Div 3 = [41–60], Div 4 = [61–80],
    /// Neutral/cup [81–96], Suffixes [97–106 via E$(N)].
    /// </summary>
    public string[] AllTeamNames { get; set; } = new string[107];

    /// <summary>Current division league table for the managed club.</summary>
    public LeagueTable CurrentLeague { get; set; } = new();

    /// <summary>
    /// Top 4 scorers per division (4 divisions).
    /// Corresponds to L$(J,K) / I(2,4,4) arrays.
    /// </summary>
    public DivisionTopScorers[] DivisionTopScorers { get; set; } = new DivisionTopScorers[5];

    /// <summary>Postponed fixtures waiting to be replayed. Corresponds to N(2,18).</summary>
    public List<PostponedFixture> PostponedFixtures { get; set; } = new(18);

    // ── Cup competitions ──────────────────────────────────────────────────────

    public CupCompetition LeagueCup { get; set; } = new() { Type = CupType.LeagueCup };
    public CupCompetition FACup     { get; set; } = new() { Type = CupType.FACup };

    /// <summary>
    /// Active European competition. Null if the club is not in Europe.
    /// </summary>
    public EuropeanCompetition? European { get; set; }

    /// <summary>True while the club is on a European friendly tour. Corresponds to cp=1.</summary>
    public bool InEuropeanFriendlyTour { get; set; }

    // ── Transfer market ───────────────────────────────────────────────────────

    public TransferMarket TransferMarket { get; set; } = new();

    // ── Season history ────────────────────────────────────────────────────────

    /// <summary>Up to 10 completed seasons. Corresponds to K(10,8).</summary>
    public List<SeasonRecord> SeasonHistory { get; set; } = new(10);

    // ── Club records ──────────────────────────────────────────────────────────

    /// <summary>Name of record signing. Corresponds to O$(1).</summary>
    public string RecordSigningName      { get; set; } = string.Empty;

    /// <summary>Name of player sold for record fee. Corresponds to O$(2).</summary>
    public string RecordSaleName         { get; set; } = string.Empty;

    /// <summary>Player with most goals in a season. Corresponds to O$(3).</summary>
    public string HighestGoalscorerName  { get; set; } = string.Empty;

    /// <summary>Player with most appearances. Corresponds to O$(4).</summary>
    public string MostAppearancesName    { get; set; } = string.Empty;

    /// <summary>Highest-paid player. Corresponds to O$(5).</summary>
    public string HighestPaidPlayerName  { get; set; } = string.Empty;

    /// <summary>
    /// Last three clubs managed before this one (for Club History screen).
    /// Corresponds to O$(6–8).
    /// </summary>
    public string[] PreviousClubs { get; set; } = new string[3];

    // ── Fixture scheduling ────────────────────────────────────────────────────

    /// <summary>
    /// Rolling index into AllTeamNames pointing at the next league opponent.
    /// Cycles through the current division range (cM..cn), skipping the
    /// player's own club. Persisted to save file as the scalar V.
    /// </summary>
    public int CurrentOpponentIndex { get; set; }

    // ── Difficulty ────────────────────────────────────────────────────────────

    /// <summary>
    /// Difficulty level. JT=1 = hard, 0 = normal, -1 = easy.
    /// Corresponds to JT / AB.
    /// </summary>
    public int DifficultyLevel { get; set; } = -1;

    // ── In-progress match ─────────────────────────────────────────────────────

    /// <summary>
    /// Populated only while a match is being played (the PLAYMATCH section).
    /// Null at all other times.
    /// </summary>
    public MatchState? CurrentMatch { get; set; }
}