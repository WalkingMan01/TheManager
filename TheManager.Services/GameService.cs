using System;
using System.Collections.Generic;
using System.Text;
using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.Services
{
    public class GameService
    {
        private GameState _gameState;
        private readonly Random _random;
        private readonly MatchEngine _engine;

        // Per-match state used by the weekly tick
        private bool _lostLastMatch;
        private bool _wonLastLeagueMatch;
        private bool _wonLastCupMatch;
        private bool _lastMatchWasHome;
        private int _lastOpponentLeaguePos = 10;

        public required string Team { get; init; }
        public required string Manager { get; init; }
        public Division Division { get; init; } = Division.Four;
        public GameState State => _gameState;
        public Random Random   => _random;
        public GameService()
        {
            _gameState = new GameState();
            _random = new Random();
            _engine = new MatchEngine();
        }

        public void StartGame()
        {
            InitializationService.SetupNewGame(_gameState, Team, Division, Manager, _random);
            FixtureSchedulerService.GetSeasonFixtures(_gameState);

            InitLeagueTable();
        }

        public void InitLeagueTable()
        {
            int divStart = (int)_gameState.Club.Division * 20 - 19;
            _gameState.CurrentLeague = new LeagueTable { Division = _gameState.Club.Division };
            for (int i = 0; i < 20; i++)
                _gameState.CurrentLeague.Entries.Add(new LeagueEntry
                {
                    TeamName = _gameState.AllTeamNames[divStart + i]
                });
        }

        public MatchResult PlayMatch()
        {
            var scheduled = FixtureSchedulerService.GetCurrentMatch(_gameState);

            if (scheduled.MatchType == MatchType.EndOfSeason)
            {
                RunEndOfSeason();
                return new MatchResult { WasEndOfSeason = true };
            }

            // Get match opponent
            bool isHome     = scheduled.IsHomeGame;
            string opponentName = scheduled.OpponentName;
            bool isCupWeek  = scheduled.MatchType is MatchType.LeagueCup or MatchType.FACup;

            // Set up match
            var ourRatings = PlayerService.CalculateTeamRatings(_gameState.Squad);

            if (scheduled.OpponentRatings == null)
            {
                scheduled.OpponentRatings = OpponentRatingService.Estimate(
                opponentName,
                _gameState.CurrentLeague,
                _gameState.Club.Division,
                _gameState.DifficultyLevel, 0, false, _random);
            }

            var matchInput = OpponentRatingService.BuildMatchInput(
                ourRatings, scheduled.OpponentRatings, _gameState.Club,
                isHome, _lostLastMatch, lineupChanges: 0);

            var sim = _engine.SetupMatch(matchInput);

            // ── Process goal events ───────────────────────────────────────────────
            int ourScore   = 0;
            int theirScore = 0;
            var matchGoals = new List<MatchGoal>();

            foreach (var ev in sim.GoalEvents.OrderBy(g => g.Minute))
            {
                if (ev.Scorer == 1)
                {
                    ourScore++;
                    string? scorerName = MatchEngine.RecordOurGoal(_gameState.Squad, _random);
                    matchGoals.Add(new MatchGoal { Minute = ev.Minute, IsOurGoal = true, Scorer = scorerName });
                }
                else
                {
                    theirScore++;
                    MatchEngine.RecordOpponentGoal(_gameState.Squad);
                    matchGoals.Add(new MatchGoal { Minute = ev.Minute, IsOurGoal = false });
                }
            }

            bool weWon      = ourScore > theirScore;
            bool weDrew     = ourScore == theirScore;
            bool weLost     = ourScore < theirScore;
            bool cleanSheet = theirScore == 0;

            // ── Post-match processing ─────────────────────────────────────────────
            PlayerService.ApplyPostMatchSkillChanges(_gameState.Squad, weWon, weLost, cleanSheet);

            _gameState.Club.TeamMorale += weWon ? 5 : weDrew ? 1 : -7;
            _gameState.Club.TeamMorale = Math.Max(2, Math.Min(99, _gameState.Club.TeamMorale));

            List<OtherFixtureResult> otherFixtures = [];
            if (!isCupWeek)
            {
                string home = isHome ? _gameState.Club.Name : opponentName;
                int    hScr = isHome ? ourScore              : theirScore;
                string away = isHome ? opponentName          : _gameState.Club.Name;
                int    aScr = isHome ? theirScore            : ourScore;
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

            _lostLastMatch      = weLost;
            _wonLastLeagueMatch = weWon && !isCupWeek;
            _wonLastCupMatch    = weWon && isCupWeek;
            _lastMatchWasHome = isHome;
            _lastOpponentLeaguePos = scheduled.OpponentRatings.LeaguePosition;

            FixtureSchedulerService.AdvanceWeek(_gameState);
            WeeklyUpdate();

            return new MatchResult
            {
                OurClubName    = _gameState.Club.Name,
                OpponentName   = opponentName,
                IsHomeGame     = isHome,
                OurScore       = ourScore,
                TheirScore     = theirScore,
                Goals          = matchGoals,
                OtherFixtures  = otherFixtures
            };
        }

        private void RunEndOfSeason()
        {
            LeagueService.Sort(_gameState.CurrentLeague, _gameState.Club.PointsPerWin);
            int finalPos = Math.Max(1,
                _gameState.CurrentLeague.Entries
                    .FindIndex(e => e.TeamName.Trim() == _gameState.Club.Name.Trim()) + 1);

            var newDivision = SeasonService.DetermineNewDivision(finalPos, _gameState.Club.Division);

            int squadCount = Enumerable.Range(1, 20).Count(i => _gameState.Squad[i] != null);
            int rating = SeasonService.CalculateManagerRating(
                finalPos,
                (int)_gameState.Club.LeagueCupRound,
                (int)_gameState.Club.FACupRound,
                europeanRound: 0,
                squadPlayersRemaining: squadCount,
                bankBalance: _gameState.Finances.BankBalance,
                division: _gameState.Club.Division);

            _gameState.Club.ManagerRating = rating;

            //Console.Clear();
            //Header("END OF SEASON");
            //Console.WriteLine();
            //Console.WriteLine($"  Final league position: {finalPos}");

            //if (newDivision < _gameState.Club.Division)
            //{
            //    Console.ForegroundColor = ConsoleColor.Green;
            //    Console.WriteLine("  *** PROMOTED! ***"); Console.ResetColor();
            //}
            //else if (newDivision > _gameState.Club.Division)
            //{
            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine("  *** RELEGATED! ***"); Console.ResetColor();
            //}

            SeasonService.AwardLeaguePrizeMoney(_gameState.Finances, finalPos, _gameState.Club.Division);

            //Console.WriteLine($"  Manager rating: {rating}%");
            //Console.WriteLine($"  Bank balance:   {FormatMoney(_state.Finances.BankBalance)}");

            // Save history
            _gameState.SeasonHistory.Add(new SeasonRecord
            {
                SeasonNumber = _gameState.SeasonsPlayed + 1,
                FinalLeaguePosition = finalPos,
                Division = _gameState.Club.Division,
                LeagueCupRoundReached = (int)_gameState.Club.LeagueCupRound,
                FACupRoundReached = (int)_gameState.Club.FACupRound
            });
            if (_gameState.SeasonHistory.Count > 10)
                _gameState.SeasonHistory.RemoveAt(0);

            // Pause();

            _gameState.Club.Division = newDivision;
            SeasonService.SwapPromotedRelegatedTeams(_gameState.AllTeamNames, upperDivisionNumber: 1);
            SeasonService.SwapPromotedRelegatedTeams(_gameState.AllTeamNames, upperDivisionNumber: 3);

            _gameState.CurrentWeek = 1;
            _gameState.FixturesPlayed = 0;
            _gameState.MatchesRemainingThisSeason = 38;
            _gameState.SeasonsPlayed++;
            _gameState.Club.LeagueCupRound = CupRound.NotEntered;
            _gameState.Club.FACupRound = CupRound.NotEntered;
            FixtureSchedulerService.ResetOpponentPointer(_gameState);

            // New cup draws
            var lcBracket = CupService.SetupInitialBracket(_gameState.AllTeamNames, _random);
            _gameState.LeagueCup.CurrentRoundFixtures =
                [..CupService.DrawRound(lcBracket, _gameState.AllTeamNames, _random)
                         .Select(ToCupFixture)];

            var faBracket = CupService.SetupInitialBracket(_gameState.AllTeamNames, _random);
            _gameState.FACup.CurrentRoundFixtures =
                [..CupService.DrawRound(faBracket, _gameState.AllTeamNames, _random)
                         .Select(ToCupFixture)];

            InitLeagueTable();

            //Console.Clear();
            //Header("NEW SEASON");
            //Console.WriteLine($"  Division {(int)_gameState.Club.Division}  —  Good luck!");
            //Pause();
        }

        private void WeeklyUpdate()
        {
            PlayerService.UpdateSquadAppearances(_gameState.Squad);
            // ToDo: Need to update squad ratings somehow
            //PlayerService.ApplyWeeklySkillDrift(_gameState.Squad, _random);

            // Coach improves youth players weekly (BASIC lines 5408–5411)
            if (_gameState.Club.HasCoach && _gameState.Coach != null && _gameState.YouthTeam.Count > 0)
            {
                int ra = _gameState.Coach.SkillPercent / 10;
                int rb = ra > 0 ? _random.Next(ra) / 2 : 0;
                foreach (var youth in _gameState.YouthTeam)
                {
                    if (youth.SkillPercent + rb > youth.PotentialSkillPercent)
                        youth.SkillPercent = Math.Max(0, youth.SkillPercent - rb);
                    else
                        youth.SkillPercent = Math.Min(99, youth.SkillPercent + rb);
                }
            }

            _gameState.Club.ManagerContractWeeks = Math.Max(0, _gameState.Club.ManagerContractWeeks - 1);

            // Gate money (home only; BASIC bl = dn * nj)
            double attendance = 0;
            if (_lastMatchWasHome)
            {
                int divNum = (int)_gameState.Club.Division;
                int ourPos = Math.Max(1,
                    _gameState.CurrentLeague.Entries
                        .FindIndex(e => e.TeamName.Trim() == _gameState.Club.Name.Trim()) + 1);

                int dn = 50000 + _random.Next(10000);
                dn = (int)((double)dn / divNum) / divNum;
                dn -= (int)(1250.0 / divNum / divNum * ourPos);
                dn -= (int)(1250.0 / divNum / divNum * _lastOpponentLeaguePos);
                if (divNum == 1) dn += dn / 3;
                if (divNum < 3 && _gameState.Club.GroundImprovementCost > 0 && dn > 18721)
                    dn = 18721;
                attendance = Math.Max(500, dn + 1 + _random.Next(50));
            }

            double gateMoney = attendance * _gameState.Club.TicketPriceInPounds;
            _gameState.Finances.LastMatchAttendance = attendance;
            _gameState.Finances.LastMatchGateMoney = gateMoney;

            bool hasTestimonial = Enumerable.Range(1, 20)
                .Any(i => (_gameState.Squad[i]?.GamesPlayed ?? 0) > 400);

            int divNum2 = (int)_gameState.Club.Division;
            double staffWages = StaffService.TotalStaffWageBill(_gameState);
            var finInput = new WeeklyReportInput
            {
                GateMoney = gateMoney,
                PlayerWageBill = _gameState.Finances.PlayerWageBill + staffWages,
                InsurancePayout = _gameState.Finances.InsurancePayout,
                LeagueBonus = 200.0 / divNum2,
                CupBonus = 300.0 / divNum2,
                WonLeagueMatch = _wonLastLeagueMatch,
                WonCupMatch = _wonLastCupMatch,
                IsManagerOfMonthEligible = false,
                TestimonialPayment = hasTestimonial,
                Division = divNum2
            };

            var report = FinanceService.CalculateWeeklyReport(finInput, _gameState.Finances, _random);
            var crisis = FinancialCrisisService.Evaluate(_gameState, _random);
            var events = RandomEventService.EvaluateWeeklyEvents(_gameState, _random);
            string? resign = StaffService.CheckRandomResignation(_gameState, _random);

            // ── Show weekly news ──────────────────────────────────────────────────
            //Console.Clear();
            //Header($"WEEKLY NEWS  —  WEEK {_gameState.CurrentWeek - 1}");
            //Console.WriteLine();

            //if (_lastMatchWasHome && attendance > 0)
            //    Console.WriteLine($"  Attendance: {attendance:N0}   Gate: {FormatMoney(gateMoney)}");

            //PrintFinanceLine("Gate money", report.GateMoney);
            //PrintFinanceLine("Wages", -report.PlayerWageBill);
            //PrintFinanceLine("Running costs", -report.RunningCosts);
            //if (report.BankInterest > 0) PrintFinanceLine("Bank interest", report.BankInterest);
            //if (report.SponsorPayment > 0) PrintFinanceLine("Sponsorship", report.SponsorPayment);
            //if (report.TvBroadcastIncome > 0) PrintFinanceLine("TV broadcast", report.TvBroadcastIncome);
            //if (report.LeagueBonusPaid > 0) PrintFinanceLine("Win bonus paid", -report.LeagueBonusPaid);
            //if (report.LoanRepayment > 0) PrintFinanceLine("Loan repayment", -report.LoanRepayment);
            //if (report.MortgageRepayment > 0) PrintFinanceLine("Mortgage", -report.MortgageRepayment);
            //if (report.VatBill > 0) PrintFinanceLine("VAT bill", -report.VatBill);
            //if (report.DirectorsWithdrawal > 0) PrintFinanceLine("Directors withdrew", -report.DirectorsWithdrawal);
            //if (report.InsurancePayout > 0) PrintFinanceLine("Insurance payout", report.InsurancePayout);
            //if (report.TestimonialPayment > 0) PrintFinanceLine("Testimonial", report.TestimonialPayment);
            //if (report.ManagerOfMonthBonus > 0) PrintFinanceLine("Manager of Month", report.ManagerOfMonthBonus);

            //Separator();
            //string profSign = report.WeeklyProfit >= 0 ? "+" : "";
            //Console.WriteLine($"  NET THIS WEEK:  {profSign}{FormatMoney(report.WeeklyProfit)}");
            //Console.WriteLine($"  BANK BALANCE:   {FormatMoney(_gameState.Finances.BankBalance)}");
            //Console.WriteLine();

            //if (crisis.Outcome != CrisisOutcome.NoAction)
            //{
            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine($"  *** {crisis.Summary.ToUpper()} ***");
            //    Console.ResetColor();
            //    Console.WriteLine();
            //}

            //if (resign != null)
            //{
            //    Console.ForegroundColor = ConsoleColor.Yellow;
            //    Console.WriteLine($"  *** {resign.ToUpper()} HAS RESIGNED! ***");
            //    Console.ResetColor();
            //    Console.WriteLine();
            //}

            //foreach (var ev in events)
            //{
            //    //Console.WriteLine($"  {ev.Description}");

            //    switch (ev.Type)
            //    {
            //        case RandomEventType.InternationalCallUp:
            //            //Console.Write($"  Release {ev.PlayerName.TrimEnd()}? [Y/N] ");
            //            RandomEventService.ResolveInternationalCallUp(
            //                _gameState, ev.PlayerSlot, ReadYesNo(), _random);
            //            break;

            //        case RandomEventType.ForeignTransferOffer:
            //            //Console.Write(
            //            //    $"  Accept offer of {FormatMoney(ev.FinancialValue)} for " +
            //            //    $"{ev.PlayerName.TrimEnd()}? [Y/N] ");
            //            RandomEventService.ResolveForeignTransferOffer(
            //                _gameState, ev.PlayerSlot, ev.FinancialValue, ReadYesNo());
            //            break;

            //        case RandomEventType.PlayerTransferRequest:
            //            //Console.Write(
            //             //   $"  {ev.PlayerName.TrimEnd()} wants {FormatMoney(ev.FinancialValue)} — " +
            //             //   "sell? [Y/N] ");
            //            RandomEventService.ResolveTransferRequest(
            //                _gameState, ev.PlayerSlot, ev.FinancialValue, ReadYesNo(), _random);
            //            break;
            //    }
            //    Console.WriteLine();
            //}

            //if (crisis.ManagerSacked || _gameState.Club.ManagerContractWeeks == 0)
            //{
            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine("  YOU HAVE BEEN SACKED!  GAME OVER.");
            //    Console.ResetColor();
            //    Pause("Press any key to exit...");
            //    Environment.Exit(0);
            //}

            // Pause();
        }
        private static CupFixture ToCupFixture(CupFixturePair pair) => new()
        {
            HomeTeam = pair.HomeTeamName,
            AwayTeam = pair.AwayTeamName,
            HomeDivision = (Division)Math.Clamp(pair.HomeDivision, 1, 4),
            AwayDivision = (Division)Math.Clamp(pair.AwayDivision, 1, 4)
        };
    }
}