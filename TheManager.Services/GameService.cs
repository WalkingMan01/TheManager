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
    private const int MinimumMorale = 10;

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

    /// <summary>
    /// On the end-of-season matchday, if the club finished in a play-off spot,
    /// builds the play-off field and schedules leg 1 straight away — the
    /// play-offs are still part of the season, so the fixture (and the normal
    /// Check Match / Play Match options) should already be on the calendar
    /// before the player acts, rather than being hidden behind a separate
    /// "continue" gate. Idempotent; a no-op once the leg is scheduled, once
    /// the play-off has been resolved silently (club not involved), or on any
    /// matchday that already has its own fixture.
    /// </summary>
    public void PreparePlayoffMatchday()
    {
        var scheduled = FixtureSchedulerService.GetCurrentMatch(_gameState.CurrentWeek, _gameState.Fixtures);
        if (scheduled.MatchType != MatchType.EndOfSeason) return;

        LeagueService.Sort(_gameState.CurrentLeague, _gameState.Club.PointsPerWin);
        _gameState.Club.LeaguePosition = Math.Max(1,
            _gameState.CurrentLeague.Entries
                .FindIndex(e => e.TeamName.Trim() == _gameState.Club.Name.Trim()) + 1);

        SchedulePlayoffIfNeeded();
    }

    public MatchResult PlayMatch()
    {
        var scheduled = FixtureSchedulerService.GetCurrentMatch(_gameState.CurrentWeek, _gameState.Fixtures);

        if (scheduled.MatchType == MatchType.EndOfSeason)
        {
            bool seasonEnded = RunEndOfSeason();
            if (seasonEnded)
                return new MatchResult { WasEndOfSeason = true };

            // The season isn't over — a play-off leg was just scheduled for this
            // same matchday. Play it now instead of returning a blank result with
            // no opponent; nothing (no tick, no match) happened on this call.
            return PlayMatch();
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
        else if (scheduled.MatchType == MatchType.Playoff)
        {
            // Opponent and venue were already resolved when this fixture was
            // appended (leg 1 in RunEndOfSeason, leg 2/final later in this method).
            neutralVenue = scheduled.Week == Constants.PlayoffFinalMatchday;
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

        // ── Penalty shootout (drawn cup tie / play-off — replays are abstracted away) ─
        // Play-off leg 1 is never decisive on its own — no shootout there even if
        // drawn. Leg 2 goes to a shootout only if the two-leg AGGREGATE is level
        // (no extra time in the semi-final — straight to penalties); everywhere
        // else (FA Cup rounds, the play-off final) a draw on the night decides it.
        bool isPlayoffLeg1 = scheduled.MatchType == MatchType.Playoff
            && scheduled.Week == Constants.PlayoffSemiFinalFirstLegMatchday;
        bool isPlayoffLeg2 = scheduled.MatchType == MatchType.Playoff
            && scheduled.Week == Constants.PlayoffSemiFinalSecondLegMatchday;

        int aggregateOurs   = ourScore;
        int aggregateTheirs = theirScore;
        if (isPlayoffLeg2)
        {
            aggregateOurs   += _gameState.Playoff.FirstLegOurScore   ?? 0;
            aggregateTheirs += _gameState.Playoff.FirstLegTheirScore ?? 0;
        }

        bool goesToShootoutIfLevel = isPlayoffLeg2 ? aggregateOurs == aggregateTheirs : ourScore == theirScore;

        PenaltyShootoutResult? shootout = null;
        if (isCupWeek && !isPlayoffLeg1 && goesToShootoutIfLevel)
            shootout = PenaltyShootoutService.Run(
                _gameState.Squad, opponentName, scheduled.OpponentRatings, _random);

        bool weWon      = ourScore > theirScore || (shootout?.WeWon ?? false);
        bool weDrew     = ourScore == theirScore && shootout == null;
        bool weLost     = !weWon && !weDrew;

        // Tie-advancement outcome for the semi-final (aggregate/shootout-based),
        // distinct from weWon above (which reflects only this leg's own result —
        // used for morale, exactly like any other match).
        bool tieWonByUs = isPlayoffLeg2 && (shootout != null ? shootout.WeWon : aggregateOurs > aggregateTheirs);

        // ── Post-match updates ────────────────────────────────────────────────
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

        if (shootout != null)
        {
            scheduled.WonOnPenalties = true;
            scheduled.OurPenalties   = shootout.OurScore;
            scheduled.TheirPenalties = shootout.TheirScore;
        }

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
            bool isFinal = cupRound == CupRound.Final
                || (scheduled.MatchType == MatchType.Playoff && scheduled.Week == Constants.PlayoffFinalMatchday);
            double wembleyAttendance = isFinal ? 100_000 : 80_000;
            double ourGateShare      = wembleyAttendance * _gameState.Club.TicketPriceInPounds / 2;
            _gameState.Finances.BankBalance         += ourGateShare;
            _gameState.Finances.LastMatchAttendance  = wembleyAttendance;
            _gameState.Finances.LastMatchGateMoney   = ourGateShare;
        }

        // ── Play-off progression (leg 1 → leg 2 → final → season wrap-up) ─────
        bool seasonWrappedThisMatch = false;
        if (scheduled.MatchType == MatchType.Playoff)
        {
            if (scheduled.Week == Constants.PlayoffSemiFinalFirstLegMatchday)
            {
                _gameState.Playoff.FirstLegOurScore   = ourScore;
                _gameState.Playoff.FirstLegTheirScore = theirScore;

                var leg2 = new ScheduledMatch
                {
                    MatchType         = MatchType.Playoff,
                    Week              = Constants.PlayoffSemiFinalSecondLegMatchday,
                    OpponentName      = opponentName,
                    OpponentTeamIndex = scheduled.OpponentTeamIndex,
                    IsHomeGame        = _gameState.Playoff.PlayerIsHigherSeed
                };
                _gameState.Fixtures = _gameState.Fixtures.Append(leg2).ToList();
            }
            else if (scheduled.Week == Constants.PlayoffSemiFinalSecondLegMatchday)
            {
                if (tieWonByUs)
                {
                    var final = new ScheduledMatch
                    {
                        MatchType         = MatchType.Playoff,
                        Week              = Constants.PlayoffFinalMatchday,
                        OpponentName      = _gameState.Playoff.OtherSemiFinalWinner,
                        OpponentTeamIndex = FindTeamIndex(_gameState.Playoff.OtherSemiFinalWinner),
                        IsHomeGame        = false
                    };
                    _gameState.Fixtures = _gameState.Fixtures.Append(final).ToList();
                }
                else
                {
                    // Eliminated on aggregate/penalties — our division is unchanged.
                    // Resolve the final between our conqueror and the other semi's
                    // winner so the third promotion slot is still decided.
                    var conquerorPosition = FindLeaguePositionInCurrentTable(opponentName);
                    var otherWinnerPosition = FindLeaguePositionInCurrentTable(_gameState.Playoff.OtherSemiFinalWinner);
                    var final = PlayoffService.SimulateTie(
                        opponentName, conquerorPosition,
                        _gameState.Playoff.OtherSemiFinalWinner, otherWinnerPosition,
                        _random);

                    _gameState.Playoff.Winner = final.Winner;
                    FinishSeason();
                    seasonWrappedThisMatch = true;
                }
            }
            else if (scheduled.Week == Constants.PlayoffFinalMatchday)
            {
                _gameState.Playoff.Winner = weWon ? _gameState.Club.Name.Trim() : opponentName.Trim();
                FinishSeason();
                seasonWrappedThisMatch = true;
            }
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
            Week            = scheduled.Week,
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
            NewClubDivision = newClubDiv,
            WasEndOfSeason  = seasonWrappedThisMatch
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
            OpponentLeaguePosition: 0,
            MatchPlayed:    false);

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

    /// <summary>
    /// Fires when a matchday has no scheduled fixture. Computes the final league
    /// position and, for Championship/League One/League Two, checks whether a
    /// promotion play-off (docs/specs/promotion-playoffs.md) needs to be set up
    /// before the season can be wrapped up. Returns true if the season actually
    /// ended this call, false if a play-off leg 1 fixture was just scheduled
    /// instead (the season isn't over yet — more matchdays are coming).
    /// </summary>
    private bool RunEndOfSeason()
    {
        // Compute the player's final league position and cache it on Club
        // so SeasonService.WrapUpSeason can read it.
        LeagueService.Sort(_gameState.CurrentLeague, _gameState.Club.PointsPerWin);
        _gameState.Club.LeaguePosition = Math.Max(1,
            _gameState.CurrentLeague.Entries
                .FindIndex(e => e.TeamName.Trim() == _gameState.Club.Name.Trim()) + 1);

        if (SchedulePlayoffIfNeeded())
            return false; // leg 1 is now on the calendar; season isn't over yet

        FinishSeason();
        return true;
    }

    /// <summary>
    /// If the player's club is in a play-off division (Championship/League
    /// One/League Two), builds the play-off field from the final table. When the
    /// player's club is one of the four contenders, resolves the other semi-final
    /// silently and schedules leg 1 for the player to play, returning true.
    /// Otherwise (Division One, or the player finished outside the play-off
    /// field) the whole play-off — both semis and the final — is resolved
    /// silently right here and this returns false, so <see cref="RunEndOfSeason"/>
    /// wraps up the season immediately, exactly as before this feature existed.
    /// </summary>
    private bool SchedulePlayoffIfNeeded()
    {
        // Already prepared this season — don't re-simulate/re-append on a
        // repeat call (e.g. PreparePlayoffMatchday running again before the
        // player has acted, or RunEndOfSeason running after preparation
        // already resolved things).
        if (_gameState.Playoff.Active) return true;
        if (_gameState.Playoff.IsResolved) return false;

        var division = _gameState.Club.Division;
        if (division == Division.One) return false;

        int autoSpots = Constants.AutomaticPromotionSpots(division);
        var table     = _gameState.CurrentLeague;
        if (table.Entries.Count < autoSpots + 4) return false; // safety guard for a short/legacy table

        var (higherA, lowerA, higherB, lowerB) = PlayoffService.BuildSemiFinals(table, autoSpots);
        string ourName = _gameState.Club.Name.Trim();

        bool inTieA = higherA.Equals(ourName, StringComparison.OrdinalIgnoreCase) || lowerA.Equals(ourName, StringComparison.OrdinalIgnoreCase);
        bool inTieB = higherB.Equals(ourName, StringComparison.OrdinalIgnoreCase) || lowerB.Equals(ourName, StringComparison.OrdinalIgnoreCase);

        if (!inTieA && !inTieB)
        {
            // Resolve the whole play-off silently — the player isn't involved.
            var semiA = PlayoffService.SimulateTie(higherA, autoSpots + 1, lowerA, autoSpots + 4, _random);
            var semiB = PlayoffService.SimulateTie(higherB, autoSpots + 2, lowerB, autoSpots + 3, _random);
            var final = PlayoffService.SimulateTie(semiA.Winner, 0, semiB.Winner, 0, _random);
            _gameState.Playoff.Winner = final.Winner;
            return false;
        }

        bool   playerIsHigherSeed;
        string opponent;
        PlayoffTieResult otherSemi;

        if (inTieA)
        {
            playerIsHigherSeed = higherA.Equals(ourName, StringComparison.OrdinalIgnoreCase);
            opponent           = playerIsHigherSeed ? lowerA : higherA;
            otherSemi          = PlayoffService.SimulateTie(higherB, autoSpots + 2, lowerB, autoSpots + 3, _random);
        }
        else
        {
            playerIsHigherSeed = higherB.Equals(ourName, StringComparison.OrdinalIgnoreCase);
            opponent           = playerIsHigherSeed ? lowerB : higherB;
            otherSemi          = PlayoffService.SimulateTie(higherA, autoSpots + 1, lowerA, autoSpots + 4, _random);
        }

        _gameState.Playoff.Active               = true;
        _gameState.Playoff.PlayerInvolved       = true;
        _gameState.Playoff.PlayerIsHigherSeed   = playerIsHigherSeed;
        _gameState.Playoff.OtherSemiFinalWinner = otherSemi.Winner;

        var leg1 = new ScheduledMatch
        {
            MatchType         = MatchType.Playoff,
            Week              = Constants.PlayoffSemiFinalFirstLegMatchday,
            OpponentName      = opponent,
            OpponentTeamIndex = FindTeamIndex(opponent),
            IsHomeGame        = !playerIsHigherSeed // lower seed hosts leg 1
        };
        _gameState.Fixtures = _gameState.Fixtures.Append(leg1).ToList();

        return true;
    }

    /// <summary>Locates a team's index in <see cref="GameState.AllTeamNames"/> by name.</summary>
    private int FindTeamIndex(string teamName)
    {
        string trimmed = teamName.Trim();
        for (int i = 1; i < _gameState.AllTeamNames.Length; i++)
            if (_gameState.AllTeamNames[i].Trim().Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                return i;
        return 0;
    }

    /// <summary>Looks up a team's 1-based position in the current league table (mid-table fallback if not found).</summary>
    private int FindLeaguePositionInCurrentTable(string teamName)
    {
        string trimmed = teamName.Trim();
        int index = _gameState.CurrentLeague.Entries.FindIndex(
            e => e.TeamName.Trim().Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index + 1 : 10;
    }

    /// <summary>
    /// Wraps up the completed season: manager rating, prize money, share price,
    /// season history, promotion/relegation (including any play-off just
    /// resolved), youth aging, skill drift, and full state reset for the new
    /// season.
    /// </summary>
    private void FinishSeason()
    {
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
