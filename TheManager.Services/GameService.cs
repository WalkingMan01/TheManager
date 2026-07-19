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

    public GameService(Random? random = null)
    {
        _gameState = new GameState();
        _random    = random ?? new Random();
        _engine    = new MatchEngineService(_random);
    }

    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    private GameService(GameState loadedState, Random? random)
    {
        _gameState = loadedState;
        _random    = random ?? new Random();
        _engine    = new MatchEngineService(_random);
        Team       = loadedState.Club.Name.Trim();
        Manager    = loadedState.Club.ManagerName;
        Division   = loadedState.Club.Division;
    }

    /// <summary>
    /// Creates a <see cref="GameService"/> from a previously loaded <see cref="GameState"/>.
    /// Use this path instead of <c>new GameService() { … }</c> + <see cref="StartGame"/>
    /// when restoring a saved game — <c>StartGame</c> must not be called on this instance.
    /// </summary>
    public static GameService FromSave(GameState loadedState, Random? random = null)
        => new(loadedState, random);

    // ── Game startup ──────────────────────────────────────────────────────────

    public void StartGame()
    {
        InitializationService.SetupNewGame(_gameState, Team, Division, Manager, _random);
        _gameState.Fixtures = FixtureSchedulerService.BuildSeasonCalendar(_gameState.Club.Division, _gameState.Club.Name, _gameState.AllTeamNames);
        _gameState.CurrentLeague = LeagueService.InitialiseTable(_gameState.Club.Division, _gameState.AllTeamNames);
    }

    // ── Match pipeline ────────────────────────────────────────────────────────

    /// <summary>
    /// On an FA Cup matchday, ensures the round is drawn and copies the player's
    /// tie onto the calendar entry so screens can show it before kick-off.
    /// Idempotent; a no-op on other matchdays.
    /// </summary>
    public void PrepareCupMatchday()
    {
        var scheduled = FixtureSchedulerService.GetCurrentMatch(_gameState.CurrentWeek, _gameState.Fixtures);
        if (scheduled.MatchType != MatchType.FACup || scheduled.WasPlayed) return;

        int roundIndex = Array.IndexOf(Constants.FACupMatchdays, _gameState.CurrentWeek);
        if (roundIndex < 0) roundIndex = _gameState.FACup.RoundHistory.Count;

        CupService.EnsureRoundDrawn(_gameState.FACup, _gameState.AllTeamNames, _gameState.Club.Name, roundIndex, _random);

        var tie = CupService.FindPlayerFixture(_gameState.FACup.CurrentRoundFixtures, _gameState.Club.Name);
        if (tie == null) return;

        bool tieIsHome = tie.HomeTeam.Trim().Equals(_gameState.Club.Name.Trim(), StringComparison.OrdinalIgnoreCase);
        scheduled.OpponentName      = tieIsHome ? tie.AwayTeam : tie.HomeTeam;
        scheduled.OpponentTeamIndex = tieIsHome ? tie.AwayTeamIndex : tie.HomeTeamIndex;
        scheduled.IsHomeGame        = tieIsHome;
    }

    public MatchResult PlayMatch()
    {
        var scheduled = FixtureSchedulerService.GetCurrentMatch(_gameState.CurrentWeek, _gameState.Fixtures);

        if (scheduled.MatchType == MatchType.EndOfSeason)
        {
            RunEndOfSeason();
            return new MatchResult { WasEndOfSeason = true };
        }

        if (scheduled.MatchType == MatchType.NoFixture)
            return PlayRestDay(scheduled);

        // ── Cup matchday pre-processing ───────────────────────────────────────
        var      cup          = _gameState.FACup;
        CupRound cupRound     = CupRound.NotEntered;
        CupFixture? playerTie = null;
        bool     neutralVenue = false;

        if (scheduled.MatchType == MatchType.FACup)
        {
            int roundIndex = Array.IndexOf(Constants.FACupMatchdays, _gameState.CurrentWeek);
            if (roundIndex < 0) roundIndex = cup.RoundHistory.Count;

            CupService.EnsureRoundDrawn(cup, _gameState.AllTeamNames, _gameState.Club.Name, roundIndex, _random);
            cupRound  = CupService.RoundForIndex(roundIndex);
            playerTie = CupService.FindPlayerFixture(cup.CurrentRoundFixtures, _gameState.Club.Name);

            if (playerTie == null)
            {
                // Eliminated or not yet entered — the round happens without us.
                var roundResults = cup.CurrentRoundFixtures.Count > 0
                    ? CupService.CompleteRound(cup, _random)
                    : [];
                return PlayRestDay(scheduled, roundResults, CupService.RoundDisplayName(cupRound));
            }

            // Fill the calendar placeholder so the fixtures screen shows the tie.
            bool tieIsHome = playerTie.HomeTeam.Trim()
                .Equals(_gameState.Club.Name.Trim(), StringComparison.OrdinalIgnoreCase);
            scheduled.OpponentName      = tieIsHome ? playerTie.AwayTeam : playerTie.HomeTeam;
            scheduled.OpponentTeamIndex = tieIsHome ? playerTie.AwayTeamIndex : playerTie.HomeTeamIndex;
            scheduled.IsHomeGame        = tieIsHome;
            neutralVenue                = CupService.IsNeutralVenue(cupRound);
        }

        bool   isHome       = scheduled.IsHomeGame;
        string opponentName = scheduled.OpponentName;
        bool   isCupWeek    = scheduled.MatchType != MatchType.League;

        // ── Ratings ───────────────────────────────────────────────────────────
        var ourRatings = PlayerService.CalculateTeamRatings(_gameState.Squad);

        scheduled.OpponentRatings ??= OpponentRatingService.Estimate(
            opponentName,
            _gameState.CurrentLeague,
            _gameState.Club.Division,
            _gameState.DifficultyLevel,
            opponentDivision: isCupWeek
                ? CupService.GetDivisionForTeamIndex(scheduled.OpponentTeamIndex)
                : 0,
            isCupMatch: isCupWeek,
            _random);

        var matchInput = OpponentRatingService.BuildMatchInput(
            ourRatings, scheduled.OpponentRatings, _gameState.Club,
            isHome, _lostLastMatch, lineupChanges: 0);

        var sim = _engine.SetupMatch(matchInput);

        // ── Injuries / red cards / yellow cards ─────────────────────────────────
        bool substitutionUsed = false;
        var  matchIncidents   = new List<MatchIncident>();
        var  newSuspensions   = new List<SuspensionNotice>();

        if (sim.IncidentMinute > 0)
        {
            var incident = _engine.ResolveIncident(
                _gameState.Squad, sim.IncidentMinute < 81, ref substitutionUsed,
                physioSkillPercent: _gameState.Physio?.SkillPercent ?? 0);

            if (incident != null)
            {
                matchIncidents.Add(new MatchIncident
                {
                    Minute     = sim.IncidentMinute,
                    PlayerName = incident.PlayerName,
                    Type       = incident.Type,
                    WeeksOut   = incident.WeeksOut
                });

                if (incident.Type == IncidentType.RedCard)
                    newSuspensions.Add(new SuspensionNotice
                    {
                        PlayerName = incident.PlayerName,
                        MatchesOut = 3,
                        Reason     = SuspensionReason.RedCard
                    });

                // Drop any pre-rolled goals that hadn't "happened" yet at the
                // incident minute — they're superseded by the re-roll below.
                int removedOurGoals      = sim.GoalEvents.RemoveAll(g => g.Minute >= sim.IncidentMinute && g.IsOurGoal);
                int removedOpponentGoals = sim.GoalEvents.RemoveAll(g => g.Minute >= sim.IncidentMinute && !g.IsOurGoal);

                var updatedRatings = PlayerService.CalculateTeamRatings(_gameState.Squad);
                var updatedInput   = matchInput with
                {
                    OurGoalkeeperSkill = updatedRatings.GoalkeeperRating,
                    OurDefence         = updatedRatings.DefenceRating,
                    OurMid             = updatedRatings.MidRating,
                    OurAttack          = updatedRatings.AttackRating,
                };

                var (extraGoals, _, _) =
                    _engine.ContinueMatchAfterIncident(updatedInput, sim.IncidentMinute, sim.MatchLength);

                sim.GoalEvents.AddRange(extraGoals);
                sim.OurGoalCount      += extraGoals.Count(g => g.IsOurGoal)  - removedOurGoals;
                sim.OpponentGoalCount += extraGoals.Count(g => !g.IsOurGoal) - removedOpponentGoals;
            }
        }

        foreach (var ev in sim.YellowCardEvents)
        {
            var outcome = _engine.ApplyYellowCard(_gameState.Squad, ev.Slot);
            if (outcome == null) continue;   // player no longer in the lineup — skip silently

            matchIncidents.Add(new MatchIncident
            {
                Minute = ev.Minute, PlayerName = outcome.PlayerName, Type = IncidentType.YellowCard
            });

            if (outcome.SuspensionImposed)
                newSuspensions.Add(new SuspensionNotice
                {
                    PlayerName = outcome.PlayerName,
                    MatchesOut = 1,
                    Reason     = SuspensionReason.AccumulatedYellowCards
                });
        }

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

        // ── Penalty shootout (drawn cup tie — replays are abstracted away) ────
        PenaltyShootoutResult? shootout = null;
        if (isCupWeek && ourScore == theirScore)
            shootout = PenaltyShootoutService.Run(
                _gameState.Squad, opponentName, scheduled.OpponentRatings, _random);

        bool weWon      = ourScore > theirScore || (shootout?.WeWon ?? false);
        bool weDrew     = ourScore == theirScore && shootout == null;
        bool weLost     = !weWon && !weDrew;
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
            var (divStart, _) = Constants.DivisionRange(_gameState.Club.Division);
            int teamCount      = Constants.TeamCount(_gameState.Club.Division);
            int playerDivIdx   = Array.FindIndex(_gameState.AllTeamNames, divStart, teamCount,
                                     t => t.Trim().Equals(_gameState.Club.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                                 - divStart;
            int opponentDivIdx = scheduled.OpponentTeamIndex - divStart;
            int leagueRound    = FixtureSchedulerService.FindLeagueRound(
                                     Math.Max(0, playerDivIdx), Math.Max(0, opponentDivIdx), teamCount);

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

        // ── Cup recording ─────────────────────────────────────────────────────
        List<CupFixture> cupResults = [];
        if (isCupWeek && playerTie != null)
        {
            bool tieIsHome = scheduled.IsHomeGame;
            playerTie.HomeScore = tieIsHome ? ourScore : theirScore;
            playerTie.AwayScore = tieIsHome ? theirScore : ourScore;
            playerTie.Winner    = weWon ? _gameState.Club.Name : opponentName;

            if (shootout != null)
            {
                playerTie.WonOnPenalties = true;
                playerTie.HomePenalties  = tieIsHome ? shootout.OurScore   : shootout.TheirScore;
                playerTie.AwayPenalties  = tieIsHome ? shootout.TheirScore : shootout.OurScore;

                scheduled.WonOnPenalties = true;
                scheduled.OurPenalties   = shootout.OurScore;
                scheduled.TheirPenalties = shootout.TheirScore;
            }

            // Highest round reached; the exit round stays on record if we lost.
            cup.CurrentRound           = cupRound;
            _gameState.Club.FACupRound = cupRound;

            if (weWon)
            {
                cup.RoundTracker++;
                if (cupRound == CupRound.Final)
                {
                    cup.CurrentRound           = CupRound.Winner;
                    _gameState.Club.FACupRound = CupRound.Winner;
                }
            }

            cupResults = CupService.CompleteRound(cup, _random);
        }

        // ── Build context for the weekly tick ─────────────────────────────────
        var ctx = new MatchContext(
            WonLeagueMatch:       weWon && !isCupWeek,
            WonCupMatch:          weWon && isCupWeek,
            LostLastMatch:        weLost,
            WasHomeGame:          isHome && !neutralVenue,
            OpponentLeaguePosition: scheduled.OpponentRatings.LeaguePosition,
            IsCupMatch:           isCupWeek);

        _lostLastMatch = weLost;

        var matchday = FixtureSchedulerService.AdvanceMatchday(_gameState.CurrentWeek, _gameState.FixturesPlayed,
                       Constants.FixturesInSeason(_gameState.Club.Division), wasLeagueFixture: !isCupWeek);
        _gameState.CurrentWeek               = matchday.CurrentWeek;
        _gameState.FixturesPlayed            = matchday.FixturesPlayed;
        _gameState.MatchesRemainingThisSeason = matchday.MatchesRemainingThisSeason;
        var tick = WeeklyTickService.Process(_gameState, ctx, _random);

        // ── Wembley gate (semi-final and final: neutral venue, split 50/50) ───
        if (neutralVenue)
        {
            double wembleyAttendance = cupRound == CupRound.Final ? 100_000 : 80_000;
            double ourGateShare      = wembleyAttendance * _gameState.Club.TicketPriceInPounds / 2;
            _gameState.Finances.BankBalance         += ourGateShare;
            _gameState.Finances.LastMatchAttendance  = wembleyAttendance;
            _gameState.Finances.LastMatchGateMoney   = ourGateShare;
        }

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
            MatchType       = scheduled.MatchType,
            CupRoundName    = isCupWeek ? CupService.RoundDisplayName(cupRound) : null,
            Shootout        = shootout,
            CupResults      = cupResults,
            IsNeutralVenue  = neutralVenue,
            ScoutFindings   = tick.ScoutFindings,
            ExpiringPlayers = managerSacked ? [] : tick.ExpiringPlayers,
            Incidents       = matchIncidents,
            NewSuspensions  = newSuspensions,
            FinanceReport   = tick.FinanceReport,
            ManagerSacked   = managerSacked,
            SackingReason   = sackingReason,
            NewClubName     = newClubName,
            NewClubDivision = newClubDiv
        };
    }

    // ── Rest days ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A matchday with no fixture for us: a Division One league gap, or a cup
    /// matchday after elimination (mirrors BASIC AAA=1 — straight to the weekly
    /// tick). The calendar advances; the league counters do not.
    /// </summary>
    private MatchResult PlayRestDay(
        ScheduledMatch    scheduled,
        List<CupFixture>? cupResults   = null,
        string?           cupRoundName = null)
    {
        var ctx = new MatchContext(
            WonLeagueMatch: false,
            WonCupMatch:    false,
            LostLastMatch:  _lostLastMatch,
            WasHomeGame:    false,
            OpponentLeaguePosition: 0);

        var matchday = FixtureSchedulerService.AdvanceMatchday(_gameState.CurrentWeek, _gameState.FixturesPlayed,
                           Constants.FixturesInSeason(_gameState.Club.Division), wasLeagueFixture: false);
        _gameState.CurrentWeek                = matchday.CurrentWeek;
        _gameState.FixturesPlayed             = matchday.FixturesPlayed;
        _gameState.MatchesRemainingThisSeason = matchday.MatchesRemainingThisSeason;
        var tick = WeeklyTickService.Process(_gameState, ctx, _random);

        bool     managerSacked = tick.Crisis.ManagerSacked;
        string?  sackingReason = managerSacked ? tick.Crisis.Summary : null;
        string?  newClubName   = null;
        Division? newClubDiv   = null;

        if (managerSacked)
            (newClubName, newClubDiv) = TransitionToNewClubAfterSacking();

        return new MatchResult
        {
            OurClubName     = _gameState.Club.Name,
            WasRestDay      = true,
            MatchType       = scheduled.MatchType,
            CupRoundName    = cupRoundName,
            CupResults      = cupResults ?? [],
            ScoutFindings   = tick.ScoutFindings,
            ExpiringPlayers = managerSacked ? [] : tick.ExpiringPlayers,
            FinanceReport   = tick.FinanceReport,
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
        var (divStart, _) = Constants.DivisionRange(newDivision);
        int teamCount     = Constants.TeamCount(newDivision);
        string ourName    = _gameState.Club.Name.Trim();
        string newClub;
        int    attempt    = 0;
        do
        {
            newClub = _gameState.AllTeamNames[divStart + _random.Next(teamCount)];
            attempt++;
        }
        while (newClub.Trim() == ourName && attempt < teamCount);

        int newPosition = 1 + _random.Next(teamCount);

        InitializationService.JoinNewClubMidSeason(
            _gameState, newClub, newDivision, newPosition, _random);

        // Regenerate the remaining calendar from the current matchday onwards:
        // the new club's league fixtures land on the same non-cup matchdays, so
        // cup rounds stay on their fixed dates.
        int startWeek = _gameState.CurrentWeek;

        _gameState.Fixtures = FixtureSchedulerService
            .BuildSeasonCalendar(newDivision, _gameState.Club.Name, _gameState.AllTeamNames)
            .Where(f => f.Week >= startWeek)
            .ToList();

        // Re-derive the league counters from what is actually left to play.
        int remainingLeagueFixtures = _gameState.Fixtures.Count(f => f.MatchType == MatchType.League);
        int maxFixtures             = Constants.FixturesInSeason(newDivision);
        _gameState.MatchesRemainingThisSeason = remainingLeagueFixtures;
        _gameState.FixturesPlayed             = maxFixtures - remainingLeagueFixtures;

        // Keep the existing mid-season standings but replace team names with
        // those from the new division, and record the new division.
        LeagueService.SwapDivisionTeams(_gameState.CurrentLeague, newDivision, _gameState.AllTeamNames);
        // Seed historical form slots with draws rather than nulls so the
        // sacking check (CalculateFormPoints ≤ 6) cannot re-trigger immediately
        // against a manager who just joined a new club. FixturesPlayed is
        // preserved (season clock continues) so every slot the form window
        // will inspect needs a non-null value; a 0-0 draw (1 pt) keeps the
        // accumulated form above the firing threshold of 6.
        var freshResults = new string[Constants.FixturesInSeason(newDivision)];
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

        _gameState.MatchesRemainingThisSeason = Constants.FixturesInSeason(_gameState.Club.Division);

        // Rebuild the new season's cup brackets and draw round 1.
        _gameState.LeagueCup.Bracket = CupService.SetupInitialBracket();
        _gameState.LeagueCup.RoundHistory.Clear();
        _gameState.LeagueCup.CurrentRoundFixtures =
            CupService.DrawRound(_gameState.LeagueCup.Bracket, _gameState.AllTeamNames, _random);

        _gameState.FACup.Bracket = CupService.SetupInitialBracket();
        _gameState.FACup.RoundHistory.Clear();
        _gameState.FACup.CurrentRoundFixtures =
            CupService.DrawRound(_gameState.FACup.Bracket, _gameState.AllTeamNames, _random);

        // Regenerate the fixture calendar and league table for the new season.
        _gameState.CurrentOpponentIndex = FixtureSchedulerService.GetDivisionStartIndex(_gameState.Club.Division);
        _gameState.Fixtures = FixtureSchedulerService.BuildSeasonCalendar(_gameState.Club.Division, _gameState.Club.Name, _gameState.AllTeamNames);
        _gameState.CurrentLeague = LeagueService.InitialiseTable(_gameState.Club.Division, _gameState.AllTeamNames);

        _lostLastMatch = false;
    }
}
