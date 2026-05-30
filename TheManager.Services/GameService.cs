using TheManager.Models;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.Services;

/// <summary>
/// Top-level game coordinator: owns the single <see cref="Random"/> instance and
/// the <see cref="GameState"/> aggregate, and sequences the per-match pipeline.
///
/// Heavy logic has been extracted to dedicated services:
///   Weekly tick  → <see cref="WeeklyTickService"/>
///   End of season → <see cref="SeasonService.WrapUpSeason"/>
///   League table  → <see cref="LeagueService.InitialiseTable"/>
/// </summary>
public class GameService
{
    private const int MORALE_SHIFT_DIVISOR = 6;

    private GameState    _gameState;
    private readonly Random       _random;
    private readonly MatchEngineService  _engine;

    // Sole cross-call state: did we lose the previous match?
    // Read at the start of PlayMatch, written at the end.
    private bool _lostLastMatch;

    public required string Team    { get; init; }
    public required string Manager { get; init; }
    public Division Division       { get; init; } = Division.Four;

    public GameState State => _gameState;
    public Random    Random => _random;
    public Random    Rng    => _random;   // alias for callers that use Rng

    public GameService()
    {
        _gameState = new GameState();
        _random    = new Random();
        _engine    = new MatchEngineService();
    }

    // ── Game startup ──────────────────────────────────────────────────────────

    public void StartGame()
    {
        InitializationService.SetupNewGame(_gameState, Team, Division, Manager, _random);
        FixtureSchedulerService.GetSeasonFixtures(_gameState);
        LeagueService.InitialiseTable(_gameState);
    }

    // ── Match pipeline ────────────────────────────────────────────────────────

    public MatchResult PlayMatch()
    {
        var scheduled = FixtureSchedulerService.GetCurrentMatch(_gameState);

        if (scheduled.MatchType == MatchType.EndOfSeason)
        {
            RunEndOfSeason();
            return new MatchResult { WasEndOfSeason = true };
        }

        bool   isHome       = scheduled.IsHomeGame;
        string opponentName = scheduled.OpponentName;
        bool   isCupWeek    = scheduled.MatchType is MatchType.LeagueCup or MatchType.FACup;

        // ── Ratings ───────────────────────────────────────────────────────────
        var ourRatings = PlayerService.CalculateTeamRatings(_gameState.Squad);

        scheduled.OpponentRatings ??= OpponentRatingService.Estimate(
            opponentName,
            _gameState.CurrentLeague,
            _gameState.Club.Division,
            _gameState.DifficultyLevel,
            cupRound: 0,
            isCupMatch: false,
            _random);

        var matchInput = OpponentRatingService.BuildMatchInput(
            ourRatings, scheduled.OpponentRatings, _gameState.Club,
            isHome, _lostLastMatch, lineupChanges: 0);

        var sim = _engine.SetupMatch(matchInput);

        // ── Goal events ───────────────────────────────────────────────────────
        int ourScore   = 0;
        int theirScore = 0;
        var matchGoals = new List<MatchGoal>();

        foreach (var ev in sim.GoalEvents.OrderBy(g => g.Minute))
        {
            if (ev.IsOurGoal)
            {
                ourScore++;
                string? scorer = _engine.RecordOurGoal(_gameState.Squad);
                matchGoals.Add(new MatchGoal { Minute = ev.Minute, IsOurGoal = true, Scorer = scorer });
            }
            else
            {
                theirScore++;
                _engine.RecordOpponentGoal(_gameState.Squad);
                matchGoals.Add(new MatchGoal { Minute = ev.Minute, IsOurGoal = false });
            }
        }

        bool weWon      = ourScore > theirScore;
        bool weDrew     = ourScore == theirScore;
        bool weLost     = ourScore < theirScore;
        bool cleanSheet = theirScore == 0;

        // ── Post-match updates ────────────────────────────────────────────────
        PlayerService.ApplyPostMatchSkillChanges(_gameState.Squad, weWon, weLost, cleanSheet);

        // BASIC 3305: win → me += INT(me/2), loss → me -= INT(me/2), draw → unchanged
        if      (weWon)  _gameState.Club.TeamMorale += _gameState.Club.TeamMorale / MORALE_SHIFT_DIVISOR;
        else if (weLost) _gameState.Club.TeamMorale -= _gameState.Club.TeamMorale / MORALE_SHIFT_DIVISOR;
        _gameState.Club.TeamMorale = Math.Max(2, Math.Min(99, _gameState.Club.TeamMorale));

        // ── League recording ──────────────────────────────────────────────────
        List<OtherFixtureResult> otherFixtures = [];
        if (!isCupWeek)
        {
            string home = isHome ? _gameState.Club.Name : opponentName;
            int    hScr = isHome ? ourScore             : theirScore;
            string away = isHome ? opponentName         : _gameState.Club.Name;
            int    aScr = isHome ? theirScore           : ourScore;
            LeagueService.RecordResult(_gameState.CurrentLeague, home, hScr, away, aScr);
            otherFixtures = LeagueService.SimulateOtherFixtures(
                _gameState.CurrentLeague,
                _gameState.AllTeamNames,
                _gameState.Club.Division,
                _gameState.Club.Name,
                _gameState.FixturesPlayed,
                _gameState.Club.PointsPerWin,
                _random);
        }

        scheduled.OurScore   = ourScore;
        scheduled.TheirScore = theirScore;

        // ── Build context for the weekly tick ─────────────────────────────────
        var ctx = new MatchContext(
            WonLeagueMatch:       weWon && !isCupWeek,
            WonCupMatch:          weWon && isCupWeek,
            LostLastMatch:        weLost,
            WasHomeGame:          isHome,
            OpponentLeaguePosition: scheduled.OpponentRatings.LeaguePosition);

        _lostLastMatch = weLost;

        FixtureSchedulerService.AdvanceWeek(_gameState);
        var tick = WeeklyTickService.Process(_gameState, ctx, _random);

        return new MatchResult
        {
            OurClubName   = _gameState.Club.Name,
            OpponentName  = opponentName,
            IsHomeGame    = isHome,
            OurScore      = ourScore,
            TheirScore    = theirScore,
            MatchLength   = sim.MatchLength,
            Goals         = matchGoals,
            OtherFixtures = otherFixtures,
            ScoutFindings = tick.ScoutFindings
        };
    }

    // ── End-of-season ─────────────────────────────────────────────────────────

    private void RunEndOfSeason()
    {
        // Compute the player's final league position and cache it on Club
        // so SeasonService.WrapUpSeason can read it.
        LeagueService.Sort(_gameState.CurrentLeague, _gameState.Club.PointsPerWin);
        _gameState.Club.LeaguePosition = Math.Max(1,
            _gameState.CurrentLeague.Entries
                .FindIndex(e => e.TeamName.Trim() == _gameState.Club.Name.Trim()) + 1);

        // Season wrap-up: manager rating, prize money, share price, season history,
        // promotion/relegation, youth aging, skill drift, state reset.
        SeasonService.WrapUpSeason(_gameState, _random);

        // Cup round trackers on Club are reset separately — WrapUpSeason records
        // the rounds reached before SeasonService.ResetMatchState clears them.
        _gameState.Club.LeagueCupRound = CupRound.NotEntered;
        _gameState.Club.FACupRound     = CupRound.NotEntered;

        ScoutReportService.ClearScoutMarket(_gameState);
        _gameState.TransferMarket.PlayersBeingSought.Clear();

        _gameState.MatchesRemainingThisSeason = 38;

        // Draw the new season's cup brackets.
        var lcBracket = CupService.SetupInitialBracket(_gameState.AllTeamNames, _random);
        _gameState.LeagueCup.CurrentRoundFixtures =
            [..CupService.DrawRound(lcBracket, _gameState.AllTeamNames, _random)
                     .Select(ToCupFixture)];

        var faBracket = CupService.SetupInitialBracket(_gameState.AllTeamNames, _random);
        _gameState.FACup.CurrentRoundFixtures =
            [..CupService.DrawRound(faBracket, _gameState.AllTeamNames, _random)
                     .Select(ToCupFixture)];

        // Regenerate the fixture calendar and league table for the new season.
        FixtureSchedulerService.ResetOpponentPointer(_gameState);
        FixtureSchedulerService.GetSeasonFixtures(_gameState);
        LeagueService.InitialiseTable(_gameState);

        _lostLastMatch = false;
    }

    private static CupFixture ToCupFixture(CupFixturePair pair) => new()
    {
        HomeTeam     = pair.HomeTeamName,
        AwayTeam     = pair.AwayTeamName,
        HomeDivision = (Division)Math.Clamp(pair.HomeDivision, 1, 4),
        AwayDivision = (Division)Math.Clamp(pair.AwayDivision, 1, 4)
    };
}
