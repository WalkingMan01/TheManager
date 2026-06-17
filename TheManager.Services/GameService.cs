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
    private const int MoraleShiftDivisor = 6;
    private const int MinimumMorale = 50;

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

    public GameService()
    {
        _gameState = new GameState();
        _random    = new Random();
        _engine    = new MatchEngineService();
    }

    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    private GameService(GameState loadedState)
    {
        _gameState = loadedState;
        _random    = new Random();
        _engine    = new MatchEngineService();
        Team       = loadedState.Club.Name.Trim();
        Manager    = loadedState.Club.ManagerName;
        Division   = loadedState.Club.Division;
    }

    /// <summary>
    /// Creates a <see cref="GameService"/> from a previously loaded <see cref="GameState"/>.
    /// Use this path instead of <c>new GameService() { … }</c> + <see cref="StartGame"/>
    /// when restoring a saved game — <c>StartGame</c> must not be called on this instance.
    /// </summary>
    public static GameService FromSave(GameState loadedState) => new(loadedState);

    // ── Game startup ──────────────────────────────────────────────────────────

    public void StartGame()
    {
        InitializationService.SetupNewGame(_gameState, Team, Division, Manager, _random);
        _gameState.Fixtures = FixtureSchedulerService.GenerateSeasonFixtures(_gameState.Club.Division, _gameState.Club.Name, _gameState.AllTeamNames);
        _gameState.CurrentLeague = LeagueService.InitialiseTable(_gameState.Club.Division, _gameState.AllTeamNames);
    }

    // ── Match pipeline ────────────────────────────────────────────────────────

    public MatchResult PlayMatch()
    {
        var scheduled = FixtureSchedulerService.GetCurrentMatch(_gameState.CurrentWeek, _gameState.Fixtures);

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
        if      (weWon)  _gameState.Club.TeamMorale += _gameState.Club.TeamMorale / MoraleShiftDivisor;
        else if (weLost) _gameState.Club.TeamMorale -= _gameState.Club.TeamMorale / MoraleShiftDivisor;
        _gameState.Club.TeamMorale = Math.Max(MinimumMorale, Math.Min(99, _gameState.Club.TeamMorale));

        // ── League recording ──────────────────────────────────────────────────
        List<OtherFixtureResult> otherFixtures = [];
        if (!isCupWeek)
        {
            string home = isHome ? _gameState.Club.Name : opponentName;
            int    hScr = isHome ? ourScore             : theirScore;
            string away = isHome ? opponentName         : _gameState.Club.Name;
            int    aScr = isHome ? theirScore           : ourScore;
            LeagueService.RecordResult(_gameState.CurrentLeague, home, hScr, away, aScr);

            // Record the result for form tracking before FixturesPlayed is incremented.
            if (_gameState.FixturesPlayed < _gameState.CurrentLeague.WeeklyResults.Length)
                _gameState.CurrentLeague.WeeklyResults[_gameState.FixturesPlayed] =
                    LeagueService.EncodeResultString(ourScore, theirScore);

            // Find the circle-method round for this specific matchup so that
            // SimulateOtherFixtures skips the correct pair. FixturesPlayed is
            // an H/A interleaved count and does not map 1-to-1 to round numbers.
            int divStart       = (int)_gameState.Club.Division * 20 - 19;
            int playerDivIdx   = Array.FindIndex(_gameState.AllTeamNames, divStart, 20,
                                     t => t.Trim().Equals(_gameState.Club.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                                 - divStart;
            int opponentDivIdx = scheduled.OpponentTeamIndex - divStart;
            int leagueRound    = FixtureSchedulerService.FindLeagueRound(
                                     Math.Max(0, playerDivIdx), Math.Max(0, opponentDivIdx));

            otherFixtures = LeagueService.SimulateOtherFixtures(
                _gameState.CurrentLeague,
                _gameState.AllTeamNames,
                _gameState.Club.Division,
                _gameState.Club.Name,
                leagueRound,
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

        var week = FixtureSchedulerService.AdvanceWeek(_gameState.CurrentWeek, _gameState.FixturesPlayed);
        _gameState.CurrentWeek               = week.CurrentWeek;
        _gameState.FixturesPlayed            = week.FixturesPlayed;
        _gameState.MatchesRemainingThisSeason = week.MatchesRemainingThisSeason;
        var tick = WeeklyTickService.Process(_gameState, ctx, _random);

        bool    managerSacked  = tick.Crisis.ManagerSacked;
        string? sackingReason  = managerSacked ? tick.Crisis.Summary : null;
        string? newClubName    = null;
        Division? newClubDiv   = null;

        if (managerSacked)
            (newClubName, newClubDiv) = TransitionToNewClubAfterSacking();

        return new MatchResult
        {
            OurClubName     = _gameState.Club.Name,
            OpponentName    = opponentName,
            IsHomeGame      = isHome,
            OurScore        = ourScore,
            TheirScore      = theirScore,
            MatchLength     = sim.MatchLength,
            Goals           = matchGoals,
            OtherFixtures   = otherFixtures,
            ScoutFindings   = tick.ScoutFindings,
            DepartedPlayers = tick.DepartedPlayers,
            ManagerSacked   = managerSacked,
            SackingReason   = sackingReason,
            NewClubName     = newClubName,
            NewClubDivision = newClubDiv
        };
    }

    // ── Post-sacking club transition ──────────────────────────────────────────

    // ── Voluntary resignation ─────────────────────────────────────────────────

    /// <summary>
    /// The manager resigns and is placed at a new lower-division club.
    /// Identical in effect to an involuntary sacking.
    /// </summary>
    public (string NewClubName, Division NewClubDivision) SackMyself()
        => TransitionToNewClubAfterSacking();

    private (string clubName, Division division) TransitionToNewClubAfterSacking()
    {
        // Pick a division strictly lower than current (or stay in Div 4).
        int currentDivNum = (int)_gameState.Club.Division;
        int newDivNum     = currentDivNum == 4
            ? 4
            : currentDivNum + 1 + _random.Next(4 - currentDivNum);
        var newDivision   = (Division)newDivNum;

        // Pick a random club from that division, excluding the current club.
        int    divStart  = newDivNum * 20 - 19;
        string ourName   = _gameState.Club.Name.Trim();
        string newClub;
        int    attempt   = 0;
        do
        {
            newClub = _gameState.AllTeamNames[divStart + _random.Next(20)];
            attempt++;
        }
        while (newClub.Trim() == ourName && attempt < 20);

        int newPosition = 1 + _random.Next(20);

        InitializationService.JoinNewClubMidSeason(
            _gameState, newClub, newDivision, newPosition, _random);

        // Regenerate remaining fixtures from the current week onwards.
        var allFixtures = FixtureSchedulerService.GenerateSeasonFixtures(
            newDivision, _gameState.Club.Name, _gameState.AllTeamNames);

        int remaining = _gameState.MatchesRemainingThisSeason;
        int startWeek = _gameState.CurrentWeek;

        _gameState.Fixtures = allFixtures
            .TakeLast(remaining)
            .Select((f, i) => new ScheduledMatch
            {
                Week              = startWeek + i,
                MatchType         = f.MatchType,
                OpponentName      = f.OpponentName,
                OpponentTeamIndex = f.OpponentTeamIndex,
                IsHomeGame        = f.IsHomeGame
            })
            .ToList();

        // Keep the existing mid-season standings but replace team names with
        // those from the new division, and record the new division.
        LeagueService.SwapDivisionTeams(_gameState.CurrentLeague, newDivision, _gameState.AllTeamNames);
        // Seed historical form slots with draws rather than nulls so the
        // sacking check (CalculateFormPoints ≤ 6) cannot re-trigger immediately
        // against a manager who just joined a new club. FixturesPlayed is
        // preserved (season clock continues) so every slot the form window
        // will inspect needs a non-null value; a 0-0 draw (1 pt) keeps the
        // accumulated form above the firing threshold of 6.
        var freshResults = new string[38];
        for (int i = 0; i < _gameState.FixturesPlayed && i < freshResults.Length; i++)
            freshResults[i] = LeagueService.EncodeResultString(0, 0);
        _gameState.CurrentLeague.WeeklyResults = freshResults;

        _lostLastMatch = false;

        return (newClub.Trim(), newDivision);
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

        for (int slot = 21; slot <= 23; slot++) _gameState.Squad[slot] = null;
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
        _gameState.CurrentOpponentIndex = FixtureSchedulerService.GetDivisionStartIndex(_gameState.Club.Division);
        _gameState.Fixtures = FixtureSchedulerService.GenerateSeasonFixtures(_gameState.Club.Division, _gameState.Club.Name, _gameState.AllTeamNames);
        _gameState.CurrentLeague = LeagueService.InitialiseTable(_gameState.Club.Division, _gameState.AllTeamNames);

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
