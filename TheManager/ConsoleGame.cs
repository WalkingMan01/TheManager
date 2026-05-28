using System.Threading;
using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager;

internal sealed class ConsoleGame
{
    private GameState _state = new();
    private readonly Random _rng = new();
    private readonly MatchEngineService _engine;

    // Per-match state used by the weekly tick
    private bool _lostLastMatch;
    private bool _wonLastLeagueMatch;
    private bool _wonLastCupMatch;
    private bool _lastMatchWasHome;
    private int  _lastOpponentLeaguePos = 10;

    public ConsoleGame() => _engine = new MatchEngineService(_rng);

    // ── Entry point ───────────────────────────────────────────────────────────

    public void Run()
    {
        Banner();

        if (SaveLoadService.SaveExists(SaveLoadService.DefaultSavePath))
        {
            Console.Write("DO YOU WANT TO LOAD AN OLD GAME [Y/N]? ");
            if (ReadYesNo())
            {
                _state = SaveLoadService.Load(SaveLoadService.DefaultSavePath);
                Console.WriteLine("GAME LOADED.");
                Pause();
                MainLoop();
                return;
            }
        }

        NewGame();
        MainLoop();
    }

    // ── New game setup ────────────────────────────────────────────────────────

    private void NewGame()
    {
        TeamData.Seed(_state);

        Console.Clear();
        Header("SELECT YOUR TEAM");

        int div = ReadInt("Division (1–4): ", 1, 4);

        Console.Clear();
        Header($"DIVISION {div} TEAMS");
        int divStart = div * 20 - 19;
        for (int i = 0; i < 20; i++)
        {
            Console.Write($"  {i + 1,2}. {_state.AllTeamNames[divStart + i].TrimEnd(),-10}");
            if ((i + 1) % 4 == 0) Console.WriteLine();
        }
        Console.WriteLine();
        int teamNum   = ReadInt("Team number (1–20): ", 1, 20);
        string clubName = _state.AllTeamNames[divStart + teamNum - 1];

        string managerName;
        do
        {
            Console.Write("Manager's name (max 8 chars): ");
            managerName = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();
        }
        while (managerName.Length is < 1 or > 8);

        InitializationService.SetupNewGame(
            _state, clubName, (Division)div, managerName, _rng);

        InitLeagueTable();

        Console.WriteLine($"\nWelcome {managerName}! You manage {clubName.TrimEnd()}, Division {div}.");
        Pause();
    }

    private void InitLeagueTable()
    {
        int divStart = (int)_state.Club.Division * 20 - 19;
        _state.CurrentLeague = new LeagueTable { Division = _state.Club.Division };
        for (int i = 0; i < 20; i++)
            _state.CurrentLeague.Entries.Add(new LeagueEntry
            {
                TeamName = _state.AllTeamNames[divStart + i]
            });
    }

    // ── Main loop ─────────────────────────────────────────────────────────────

    private void MainLoop()
    {
        while (true)
        {
            Console.Clear();
            StatusBar();
            Console.WriteLine();
            Console.WriteLine("  1. Play Match          6. Wages & Contracts");
            Console.WriteLine("  2. League Table        7. Goalscorers");
            Console.WriteLine("  3. Fixtures            8. Finances");
            Console.WriteLine("  4. Squad               9. FA Cup Draw");
            Console.WriteLine("  5. Injuries & Stats    0. League Cup Draw");
            Console.WriteLine();
            Console.WriteLine("  E. Extra Training      H. Club History");
            Console.WriteLine("  W. Employees           S. Save Game");
            Console.WriteLine("  Q. Quit");
            Console.WriteLine();
            Console.Write("  Choice: ");

            char key = char.ToUpper(Console.ReadKey(true).KeyChar);
            Console.WriteLine(key);

            switch (key)
            {
                case '1': PlayMatch();               break;
                case '2': ShowLeagueTable();         break;
                case '3': ShowFixtures();            break;
                case '4': ShowSquad();               break;
                case '5': ShowInjuriesStats();       break;
                case '6': ShowWagesContracts();      break;
                case '7': ShowGoalscorers();         break;
                case '8': ShowFinances();            break;
                case '9': ShowCupDraw(CupType.FACup);      break;
                case '0': ShowCupDraw(CupType.LeagueCup);  break;
                case 'E': DoExtraTraining();         break;
                case 'H': ShowClubHistory();         break;
                case 'W': ShowEmployees();           break;
                case 'S': SaveGame();                break;
                case 'Q':
                    Console.Write("  Quit? Are you sure [Y/N]? ");
                    if (ReadYesNo()) return;
                    break;
            }
        }
    }

    // ── Match ─────────────────────────────────────────────────────────────────

    private void PlayMatch()
    {
        var scheduled = FixtureSchedulerService.GetCurrentMatch(_state);

        if (scheduled.MatchType == MatchType.EndOfSeason)
        {
            RunEndOfSeason();
            return;
        }

        bool isCupWeek = scheduled.MatchType is MatchType.LeagueCup or MatchType.FACup;

        // Eliminated teams skip cup weeks; gate money = 0, no match
        if (isCupWeek && !FixtureSchedulerService.HasCupFixtureThisWeek(_state))
        {
            Console.Clear();
            Header($"WEEK {_state.CurrentWeek} — CUP BYE");
            Console.WriteLine("  Your club is not in this round.");
            Pause();
            _lastMatchWasHome = false;
            FixtureSchedulerService.AdvanceWeek(_state);
            RunWeeklyTick();
            return;
        }

        // ── Determine opponent ────────────────────────────────────────────────
        bool   isHome        = scheduled.IsHomeGame;
        string opponentName  = scheduled.OpponentName;

        if (isCupWeek)
        {
            var cup = scheduled.MatchType == MatchType.LeagueCup
                ? _state.LeagueCup
                : _state.FACup;

            var fixture = cup.CurrentRoundFixtures.FirstOrDefault(
                f => f.HomeTeam.Trim() == _state.Club.Name.Trim()
                  || f.AwayTeam.Trim() == _state.Club.Name.Trim());

            if (fixture != null)
            {
                isHome       = fixture.HomeTeam.Trim() == _state.Club.Name.Trim();
                opponentName = isHome ? fixture.AwayTeam : fixture.HomeTeam;
            }
        }

        // ── Build match simulation ────────────────────────────────────────────
        var ourRatings = PlayerService.CalculateTeamRatings(_state.Squad);

        var opponentRatings = OpponentRatingService.Estimate(
            opponentName,
            _state.CurrentLeague,
            _state.Club.Division,
            _state.DifficultyLevel,
            cupRound: isCupWeek ? (int)_state.LeagueCup.CurrentRound : 0,
            isCupMatch: isCupWeek,
            _rng);

        var matchInput = OpponentRatingService.BuildMatchInput(
            ourRatings, opponentRatings, _state.Club,
            isHome, _lostLastMatch, lineupChanges: 0);

        var sim = _engine.SetupMatch(matchInput);

        // ── Display match ─────────────────────────────────────────────────────
        Console.Clear();
        string matchLabel = isCupWeek
            ? scheduled.MatchType.ToString().Replace("Cup", " CUP").ToUpper()
            : "LEAGUE MATCH";
        Header($"WEEK {_state.CurrentWeek}  —  {matchLabel}");
        Console.WriteLine();
        Console.WriteLine($"  {_state.Club.Name.TrimEnd()} vs {opponentName.TrimEnd()}  " +
                          $"({(isHome ? "HOME" : "AWAY")})");
        Console.WriteLine();
        Console.Write("  Press any key to kick off...");
        Console.ReadKey(true);
        Console.WriteLine("\n");

        int  ourScore   = 0;
        int  theirScore = 0;
        int  eventIdx   = 0;
        bool halfShown  = false;
        var  goals      = sim.GoalEvents.OrderBy(g => g.Minute).ToList();

        for (int min = 1; min <= sim.MatchLength; min++)
        {
            if (min == 46 && !halfShown)
            {
                halfShown = true;
                Console.WriteLine($" 45' ── HALF TIME  {ourScore}–{theirScore} ──");
                Thread.Sleep(400);
            }

            while (eventIdx < goals.Count && goals[eventIdx].Minute <= min)
            {
                var ev = goals[eventIdx++];
                if (ev.IsOurGoal)
                {
                    ourScore++;
                    _engine.RecordOurGoal(_state.Squad);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" {min,2}' GOAL!  {_state.Club.Name.TrimEnd()}  {ourScore}–{theirScore}");
                    Console.ResetColor();
                }
                else
                {
                    theirScore++;
                    _engine.RecordOpponentGoal(_state.Squad);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($" {min,2}' GOAL!  {opponentName.TrimEnd()}  {ourScore}–{theirScore}");
                    Console.ResetColor();
                }
                Thread.Sleep(250);
            }

            if (sim.IncidentMinute == min)
            {
                var incident = _engine.ResolveIncident(
                    _state.Squad, min < 81,
                    physioSkillPercent: _state.Physio?.SkillPercent ?? 0);
                if (incident != null)
                {
                    string incDesc = incident.Type == IncidentType.RedCard
                        ? "RED CARD"
                        : $"INJURED ({incident.WeeksOut} wk)";
                    Console.WriteLine($" {min,2}' *** {incident.PlayerName.TrimEnd()} — {incDesc} ***");
                    Thread.Sleep(250);
                }
            }
        }

        Console.WriteLine();
        Separator();

        bool weWon      = ourScore  > theirScore;
        bool weDrew     = ourScore == theirScore;
        bool weLost     = ourScore  < theirScore;
        bool cleanSheet = theirScore == 0;
        string result   = weWon ? "WIN" : weDrew ? "DRAW" : "LOSS";

        Console.WriteLine($"  FULL TIME:  {_state.Club.Name.TrimEnd()} {ourScore}–{theirScore} " +
                          $"{opponentName.TrimEnd()}  [{result}]");

        // ── Post-match processing ─────────────────────────────────────────────
        PlayerService.ApplyPostMatchSkillChanges(_state.Squad, weWon, weLost, cleanSheet);

        _state.Club.TeamMorale += weWon ? 5 : weDrew ? 1 : -7;
        _state.Club.TeamMorale  = Math.Max(2, Math.Min(99, _state.Club.TeamMorale));

        if (!isCupWeek && _state.FixturesPlayed < 38)
        {
            _state.CurrentLeague.WeeklyResults[_state.FixturesPlayed] =
                $"{theirScore}{ourScore}";

            string home = isHome ? _state.Club.Name : opponentName;
            int    hScr = isHome ? ourScore          : theirScore;
            string away = isHome ? opponentName      : _state.Club.Name;
            int    aScr = isHome ? theirScore        : ourScore;

            LeagueService.RecordResult(_state.CurrentLeague, home, hScr, away, aScr);
            LeagueService.Sort(_state.CurrentLeague, _state.Club.PointsPerWin);
        }

        _lostLastMatch        = weLost;
        _wonLastLeagueMatch   = weWon && !isCupWeek;
        _wonLastCupMatch      = weWon && isCupWeek;
        _lastMatchWasHome     = isHome;
        _lastOpponentLeaguePos = opponentRatings.LeaguePosition;

        FixtureSchedulerService.AdvanceWeek(_state);
        Pause();
        RunWeeklyTick();
    }

    // ── Weekly tick ───────────────────────────────────────────────────────────

    private void RunWeeklyTick()
    {
        PlayerService.TickWeeklyCountdowns(_state.Squad);
        PlayerService.ApplyWeeklySkillDrift(_state.Squad, _rng);

        // Coach improves youth players weekly (BASIC lines 5408–5411)
        if (_state.Club.HasCoach && _state.Coach != null && _state.YouthTeam.Count > 0)
        {
            int ra = _state.Coach.SkillPercent / 10;
            int rb = ra > 0 ? _rng.Next(ra) / 2 : 0;
            foreach (var youth in _state.YouthTeam)
            {
                if (youth.SkillPercent + rb > youth.PotentialSkillPercent)
                    youth.SkillPercent = Math.Max(0, youth.SkillPercent - rb);
                else
                    youth.SkillPercent = Math.Min(99, youth.SkillPercent + rb);
            }
        }

        _state.Club.ManagerContractWeeks = Math.Max(0, _state.Club.ManagerContractWeeks - 1);

        // Gate money (home only; BASIC bl = dn * nj)
        double attendance = 0;
        if (_lastMatchWasHome)
        {
            int divNum = (int)_state.Club.Division;
            int ourPos = Math.Max(1,
                _state.CurrentLeague.Entries
                    .FindIndex(e => e.TeamName.Trim() == _state.Club.Name.Trim()) + 1);

            int dn = 50000 + _rng.Next(10000);
            dn  = (int)((double)dn / divNum) / divNum;
            dn -= (int)(1250.0 / divNum / divNum * ourPos);
            dn -= (int)(1250.0 / divNum / divNum * _lastOpponentLeaguePos);
            if (divNum == 1) dn += dn / 3;
            if (divNum < 3 && _state.Club.GroundImprovementCost > 0 && dn > 18721)
                dn = 18721;
            attendance = Math.Max(500, dn + 1 + _rng.Next(50));
        }

        double gateMoney = attendance * _state.Club.TicketPriceInPounds;
        _state.Finances.LastMatchAttendance = attendance;
        _state.Finances.LastMatchGateMoney  = gateMoney;

        bool hasTestimonial = Enumerable.Range(1, 20)
            .Any(i => (_state.Squad[i]?.GamesPlayed ?? 0) > 400);

        int divNum2 = (int)_state.Club.Division;
        double staffWages = StaffService.TotalStaffWageBill(_state);
        var finInput = new WeeklyReportInput
        {
            GateMoney                = gateMoney,
            PlayerWageBill           = _state.Finances.PlayerWageBill + staffWages,
            InsurancePayout          = _state.Finances.InsurancePayout,
            LeagueBonus              = 200.0 / divNum2,
            CupBonus                 = 300.0 / divNum2,
            WonLeagueMatch           = _wonLastLeagueMatch,
            WonCupMatch              = _wonLastCupMatch,
            IsManagerOfMonthEligible = false,
            TestimonialPayment       = hasTestimonial,
            Division                 = divNum2
        };

        var report      = FinanceService.CalculateWeeklyReport(finInput, _state.Finances, _rng);
        var crisis      = FinancialCrisisService.Evaluate(_state, _rng);
        var events      = RandomEventService.EvaluateWeeklyEvents(_state, _rng);
        string? resign  = StaffService.CheckRandomResignation(_state, _rng);

        // ── Show weekly news ──────────────────────────────────────────────────
        Console.Clear();
        Header($"WEEKLY NEWS  —  WEEK {_state.CurrentWeek - 1}");
        Console.WriteLine();

        if (_lastMatchWasHome && attendance > 0)
            Console.WriteLine($"  Attendance: {attendance:N0}   Gate: {FormatMoney(gateMoney)}");

        PrintFinanceLine("Gate money",         report.GateMoney);
        PrintFinanceLine("Wages",             -report.PlayerWageBill);
        PrintFinanceLine("Running costs",     -report.RunningCosts);
        if (report.BankInterest      > 0) PrintFinanceLine("Bank interest",      report.BankInterest);
        if (report.SponsorPayment    > 0) PrintFinanceLine("Sponsorship",        report.SponsorPayment);
        if (report.TvBroadcastIncome > 0) PrintFinanceLine("TV broadcast",       report.TvBroadcastIncome);
        if (report.LeagueBonusPaid   > 0) PrintFinanceLine("Win bonus paid",    -report.LeagueBonusPaid);
        if (report.LoanRepayment     > 0) PrintFinanceLine("Loan repayment",    -report.LoanRepayment);
        if (report.MortgageRepayment > 0) PrintFinanceLine("Mortgage",          -report.MortgageRepayment);
        if (report.VatBill           > 0) PrintFinanceLine("VAT bill",          -report.VatBill);
        if (report.DirectorsWithdrawal > 0) PrintFinanceLine("Directors withdrew", -report.DirectorsWithdrawal);
        if (report.InsurancePayout   > 0) PrintFinanceLine("Insurance payout",   report.InsurancePayout);
        if (report.TestimonialPayment > 0) PrintFinanceLine("Testimonial",        report.TestimonialPayment);
        if (report.ManagerOfMonthBonus > 0) PrintFinanceLine("Manager of Month",  report.ManagerOfMonthBonus);

        Separator();
        string profSign = report.WeeklyProfit >= 0 ? "+" : "";
        Console.WriteLine($"  NET THIS WEEK:  {profSign}{FormatMoney(report.WeeklyProfit)}");
        Console.WriteLine($"  BANK BALANCE:   {FormatMoney(_state.Finances.BankBalance)}");
        Console.WriteLine();

        if (crisis.Outcome != CrisisOutcome.NoAction)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  *** {crisis.Summary.ToUpper()} ***");
            Console.ResetColor();
            Console.WriteLine();
        }

        if (resign != null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  *** {resign.ToUpper()} HAS RESIGNED! ***");
            Console.ResetColor();
            Console.WriteLine();
        }

        foreach (var ev in events)
        {
            Console.WriteLine($"  {ev.Description}");

            switch (ev.Type)
            {
                case RandomEventType.InternationalCallUp:
                    Console.Write($"  Release {ev.PlayerName.TrimEnd()}? [Y/N] ");
                    RandomEventService.ResolveInternationalCallUp(
                        _state, ev.PlayerSlot, ReadYesNo(), _rng);
                    break;

                case RandomEventType.ForeignTransferOffer:
                    Console.Write(
                        $"  Accept offer of {FormatMoney(ev.FinancialValue)} for " +
                        $"{ev.PlayerName.TrimEnd()}? [Y/N] ");
                    RandomEventService.ResolveForeignTransferOffer(
                        _state, ev.PlayerSlot, ev.FinancialValue, ReadYesNo());
                    break;

                case RandomEventType.PlayerTransferRequest:
                    Console.Write(
                        $"  {ev.PlayerName.TrimEnd()} wants {FormatMoney(ev.FinancialValue)} — " +
                        "sell? [Y/N] ");
                    RandomEventService.ResolveTransferRequest(
                        _state, ev.PlayerSlot, ev.FinancialValue, ReadYesNo(), _rng);
                    break;
            }
            Console.WriteLine();
        }

        if (crisis.ManagerSacked || _state.Club.ManagerContractWeeks == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  YOU HAVE BEEN SACKED!  GAME OVER.");
            Console.ResetColor();
            Pause("Press any key to exit...");
            Environment.Exit(0);
        }

        Pause();
    }

    // ── End of season ─────────────────────────────────────────────────────────

    private void RunEndOfSeason()
    {
        LeagueService.Sort(_state.CurrentLeague, _state.Club.PointsPerWin);
        int finalPos = Math.Max(1,
            _state.CurrentLeague.Entries
                .FindIndex(e => e.TeamName.Trim() == _state.Club.Name.Trim()) + 1);

        var newDivision = SeasonService.DetermineNewDivision(finalPos, _state.Club.Division);

        int squadCount = Enumerable.Range(1, 20).Count(i => _state.Squad[i] != null);
        int rating = SeasonService.CalculateManagerRating(
            finalPos,
            (int)_state.Club.LeagueCupRound,
            (int)_state.Club.FACupRound,
            europeanRound: 0,
            squadPlayersRemaining: squadCount,
            bankBalance: _state.Finances.BankBalance,
            division: _state.Club.Division);

        _state.Club.ManagerRating = rating;

        Console.Clear();
        Header("END OF SEASON");
        Console.WriteLine();
        Console.WriteLine($"  Final league position: {finalPos}");

        if      (newDivision < _state.Club.Division)
        { Console.ForegroundColor = ConsoleColor.Green;
          Console.WriteLine("  *** PROMOTED! ***"); Console.ResetColor(); }
        else if (newDivision > _state.Club.Division)
        { Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine("  *** RELEGATED! ***"); Console.ResetColor(); }

        SeasonService.AwardLeaguePrizeMoney(_state.Finances, finalPos, _state.Club.Division);

        Console.WriteLine($"  Manager rating: {rating}%");
        Console.WriteLine($"  Bank balance:   {FormatMoney(_state.Finances.BankBalance)}");

        // Save history
        _state.SeasonHistory.Add(new SeasonRecord
        {
            SeasonNumber         = _state.SeasonsPlayed + 1,
            FinalLeaguePosition  = finalPos,
            Division             = _state.Club.Division,
            LeagueCupRoundReached = (int)_state.Club.LeagueCupRound,
            FACupRoundReached     = (int)_state.Club.FACupRound
        });
        if (_state.SeasonHistory.Count > 10)
            _state.SeasonHistory.RemoveAt(0);

        Pause();

        _state.Club.Division = newDivision;
        SeasonService.SwapPromotedRelegatedTeams(_state.AllTeamNames, upperDivisionNumber: 1);
        SeasonService.SwapPromotedRelegatedTeams(_state.AllTeamNames, upperDivisionNumber: 3);

        _state.CurrentWeek                 = 1;
        _state.FixturesPlayed              = 0;
        _state.MatchesRemainingThisSeason  = 38;
        _state.SeasonsPlayed++;
        _state.Club.LeagueCupRound         = CupRound.NotEntered;
        _state.Club.FACupRound             = CupRound.NotEntered;
        FixtureSchedulerService.ResetOpponentPointer(_state);

        // New cup draws
        var lcBracket = CupService.SetupInitialBracket(_state.AllTeamNames, _rng);
        _state.LeagueCup.CurrentRoundFixtures =
            [..CupService.DrawRound(lcBracket, _state.AllTeamNames, _rng)
                         .Select(ToCupFixture)];

        var faBracket = CupService.SetupInitialBracket(_state.AllTeamNames, _rng);
        _state.FACup.CurrentRoundFixtures =
            [..CupService.DrawRound(faBracket, _state.AllTeamNames, _rng)
                         .Select(ToCupFixture)];

        InitLeagueTable();

        Console.Clear();
        Header("NEW SEASON");
        Console.WriteLine($"  Division {(int)_state.Club.Division}  —  Good luck!");
        Pause();
    }

    // ── Screens ───────────────────────────────────────────────────────────────

    private void ShowLeagueTable()
    {
        Console.Clear();
        Header($"DIVISION {(int)_state.Club.Division} LEAGUE TABLE");
        Console.WriteLine();
        Console.WriteLine("   #  TEAM           P   W   D   L  GF  GA  GD  PTS");
        Separator();

        LeagueService.Sort(_state.CurrentLeague, _state.Club.PointsPerWin);

        for (int i = 0; i < _state.CurrentLeague.Entries.Count; i++)
        {
            var  e   = _state.CurrentLeague.Entries[i];
            bool us  = e.TeamName.Trim() == _state.Club.Name.Trim();
            int  pts = e.Points(_state.Club.PointsPerWin);

            if (us) Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(
                $"  {i + 1,2}  {e.TeamName.TrimEnd(),-13}  {e.Played,2}  {e.Won,2}  {e.Drawn,2}" +
                $"  {e.Lost,2}  {e.GoalsFor,2}  {e.GoalsAgainst,2}  {e.GoalDifference,3}  {pts,3}");
            if (us) Console.ResetColor();
        }

        Pause();
    }

    private void ShowFixtures()
    {
        Console.Clear();
        Header("FIXTURES & RESULTS");
        Console.WriteLine();
        Console.WriteLine($"  Season week: {_state.CurrentWeek} / 59   " +
                          $"League matches remaining: {_state.MatchesRemainingThisSeason}");
        Console.WriteLine();

        int count = 0;
        for (int i = 0; i < 38 && i < _state.FixturesPlayed; i++)
        {
            string r = _state.CurrentLeague.WeeklyResults[i] ?? string.Empty;
            if (r.Length < 2) continue;
            if (count == 0) Console.WriteLine("  RESULTS:");
            int them = r[0] - '0', us = r[1] - '0';
            string outcome = us > them ? "W" : us == them ? "D" : "L";
            Console.WriteLine($"    Week {i + 1,2}: {us}–{them}  [{outcome}]");
            count++;
        }
        if (count == 0) Console.WriteLine("  No results yet.");

        Console.WriteLine();
        var next = FixtureSchedulerService.GetCurrentMatch(_state);
        if (next.MatchType == MatchType.EndOfSeason)
        {
            Console.WriteLine("  Season is over — end-of-season processing pending.");
        }
        else
        {
            Console.WriteLine($"  NEXT (Week {next.Week}):");
            string type  = next.MatchType == MatchType.League ? "LEAGUE" : next.MatchType.ToString().ToUpper();
            string venue = next.IsHomeGame ? "HOME" : "AWAY";
            if (next.MatchType == MatchType.League)
                Console.WriteLine($"    {type}  vs  {next.OpponentName.TrimEnd()}  ({venue})");
            else
                Console.WriteLine($"    {type}  (see cup draw for fixture)");
        }

        Pause();
    }

    private void ShowSquad()
    {
        while (true)
        {
            Console.Clear();
            Header("SQUAD");
            Console.WriteLine();
            Console.WriteLine("   #  NAME       POS  SKL  AGE  STATUS        WK.WAGE  APPS  GLS");
            Separator();

            for (int i = 1; i <= 20; i++)
            {
                var p = _state.Squad[i];
                string section = i == 1 ? "──GK──" : i <= 5 ? null! : i <= 8 ? null! : i <= 11 ? null! : i == 12 ? "──SUB─" : i == 13 ? "─RSRV─" : null!;
                if (i == 1)  Console.WriteLine("  ─── FIRST TEAM ─────────────────────────────────────");
                if (i == 12) Console.WriteLine("  ─── SUBSTITUTE ─────────────────────────────────────");
                if (i == 13) Console.WriteLine("  ─── RESERVES ───────────────────────────────────────");

                if (p == null)
                { Console.WriteLine($"  {i,2}  (empty slot)"); continue; }

                string pos = p.Position switch
                {
                    PlayerPosition.Goalkeeper => "GK",
                    PlayerPosition.Defender   => "DEF",
                    PlayerPosition.Midfielder => "MID",
                    PlayerPosition.Attacker   => "ATK",
                    _                         => "—"
                };
                Console.WriteLine(
                    $"  {i,2}  {p.Name.TrimEnd(),-9}  {pos,-3}   {p.DisplaySkill,2}   {p.DisplayAge,2}" +
                    $"  Tmp:{p.Temper}  Games:{p.GamesPlayed}");
            }

            Console.WriteLine();
            Console.WriteLine("  Swap: enter two slot numbers (e.g. '3 15')   or 0 to exit");
            Console.Write("  > ");
            string? line = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line) || line == "0") break;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2
                && int.TryParse(parts[0], out int a) && int.TryParse(parts[1], out int b)
                && a is >= 1 and <= 20 && b is >= 1 and <= 20 && a != b)
            {
                (_state.Squad[a], _state.Squad[b]) = (_state.Squad[b], _state.Squad[a]);
            }
        }
    }

    private void ShowInjuriesStats()
    {
        Console.Clear();
        Header("SQUAD STATS");
        Console.WriteLine();
        Console.WriteLine("   #  NAME       POS  SKL  AGE  TMP  GAMES");
        Separator();

        for (int i = 1; i <= 20; i++)
        {
            var p = _state.Squad[i];
            if (p == null) continue;
            string pos = p.Position.ToString()[..3];
            Console.WriteLine(
                $"  {i,2}  {p.Name.TrimEnd(),-9}  {pos,-3}   {p.DisplaySkill,2}   {p.DisplayAge,2}   {p.Temper,2}   {p.GamesPlayed,3}");
        }

        Pause();
    }

    private void ShowWagesContracts()
    {
        Console.Clear();
        Header("WAGES & CONTRACTS");
        Console.WriteLine("  (Wage and contract data removed from player model.)");
        Pause();
    }

    private void ShowGoalscorers()
    {
        Console.Clear();
        Header("GOALSCORERS THIS SEASON");
        Console.WriteLine("  (Goals and appearances removed from player model.)");
        Pause();
    }

    private void ShowFinances()
    {
        Console.Clear();
        Header("FINANCES");
        Console.WriteLine();
        Console.WriteLine($"  Bank balance:       {FormatMoney(_state.Finances.BankBalance)}");
        Console.WriteLine($"  Overdraft available:{FormatMoney(_state.Finances.OverdraftAvailable)}");
        if (_state.Finances.LoanOutstanding > 0)
            Console.WriteLine(
                $"  Loan outstanding:   {FormatMoney(_state.Finances.LoanOutstanding)}" +
                $"  ({FormatMoney(_state.Finances.WeeklyLoanRepayment)}/wk)");
        if (_state.Finances.MortgageOutstanding > 0)
            Console.WriteLine(
                $"  Mortgage:           {FormatMoney(_state.Finances.MortgageOutstanding)}" +
                $"  ({FormatMoney(_state.Finances.WeeklyMortgageRepayment)}/wk)");
        Console.WriteLine($"  Weekly wage bill:   {FormatMoney(_state.Finances.PlayerWageBill)}");
        Console.WriteLine($"  Shares owned:       {_state.Finances.SharesOwned:N0}" +
                          $"  @  {_state.Finances.SharePriceInPence:N0}p");
        Console.WriteLine($"  Ticket price:       £{_state.Club.TicketPriceInPounds:F2}");
        if (_state.Finances.LastMatchAttendance > 0)
            Console.WriteLine($"  Last attendance:    {_state.Finances.LastMatchAttendance:N0}");
        Pause();
    }

    private void ShowCupDraw(CupType cupType)
    {
        var    cup  = cupType == CupType.FACup ? _state.FACup : _state.LeagueCup;
        string name = cupType == CupType.FACup ? "FA CUP" : "LEAGUE CUP";

        Console.Clear();
        Header($"{name} DRAW");
        Console.WriteLine();

        if (cup.CurrentRoundFixtures.Count == 0)
        {
            Console.WriteLine("  No fixtures drawn yet.");
        }
        else
        {
            Console.WriteLine($"  Round: {cup.RoundName}");
            Console.WriteLine();
            foreach (var f in cup.CurrentRoundFixtures)
                Console.WriteLine($"  {f.HomeTeam.TrimEnd(),-12} vs  {f.AwayTeam.TrimEnd()}");
        }

        Pause();
    }

    private void ShowClubHistory()
    {
        Console.Clear();
        Header("CLUB HISTORY");
        Console.WriteLine();
        Console.WriteLine($"  Club:           {_state.Club.Name.TrimEnd()}");
        Console.WriteLine($"  Division:       {(int)_state.Club.Division}");
        Console.WriteLine($"  Manager:        {_state.Club.ManagerName}");
        Console.WriteLine($"  Seasons served: {_state.SeasonsPlayed}");
        Console.WriteLine($"  Manager rating: {_state.Club.ManagerRating}%");
        Console.WriteLine($"  Contract:       {_state.Club.ManagerContractWeeks} wk remaining");

        if (!string.IsNullOrWhiteSpace(_state.RecordSigningName))
            Console.WriteLine($"  Record signing: {_state.RecordSigningName}" +
                              $"  ({FormatMoney(_state.Finances.RecordSigningFee)})");
        if (!string.IsNullOrWhiteSpace(_state.RecordSaleName))
            Console.WriteLine($"  Record sale:    {_state.RecordSaleName}" +
                              $"  ({FormatMoney(_state.Finances.RecordSaleFee)})");

        if (_state.SeasonHistory.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  SEASON HISTORY:");
            Console.WriteLine("     Season  Div  Pos  LC   FA");
            foreach (var s in _state.SeasonHistory)
                Console.WriteLine(
                    $"     {s.SeasonNumber,4}    {(int)s.Division}   {s.FinalLeaguePosition,2}  " +
                    $" {s.LeagueCupRoundReached,2}   {s.FACupRoundReached,2}");
        }

        Pause();
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private void DoExtraTraining()
    {
        Console.Clear();
        Header("EXTRA TRAINING");
        Console.WriteLine();
        Console.WriteLine("  1. Goalkeepers");
        Console.WriteLine("  2. Defenders");
        Console.WriteLine("  3. Midfielders");
        Console.WriteLine("  4. Attackers");
        Console.WriteLine("  0. Cancel");
        Console.WriteLine();
        int choice = ReadInt("  Position: ", 0, 4);
        if (choice == 0) return;

        PlayerPosition pos = choice switch
        {
            1 => PlayerPosition.Goalkeeper,
            2 => PlayerPosition.Defender,
            3 => PlayerPosition.Midfielder,
            _ => PlayerPosition.Attacker
        };

        int hours = ReadInt("  Hours (1–9): ", 1, 9);
        var outcomes = ExtraTrainingService.RunSession(pos, hours, _state.Squad, _state.Club, _rng);

        Console.WriteLine();
        foreach (var o in outcomes)
        {
            Console.WriteLine($"  {o.PlayerName.TrimEnd()}: {o.Result.ToString().ToLower()}" +
                              (o.NetImprovement != 0 ? $"  ({o.NetImprovement:+0.0;-0.0})" : string.Empty));
        }

        Pause();
    }

    private void ShowEmployees()
    {
        while (true)
        {
            Console.Clear();
            Header("EMPLOYEES");
            Console.WriteLine();

            if (_state.Club.HasCoach && _state.Coach != null)
                Console.WriteLine($"  COACH:   {_state.Coach.Name.TrimEnd(),-8}  " +
                                  $"Skill: {_state.Coach.SkillPercent}%  " +
                                  $"Wage: {FormatMoney(_state.Coach.WeeklySalary)}/wk");
            else
                Console.WriteLine("  COACH:   (none)");

            if (_state.Club.HasPhysio && _state.Physio != null)
                Console.WriteLine($"  PHYSIO:  {_state.Physio.Name.TrimEnd(),-8}  " +
                                  $"Skill: {_state.Physio.SkillPercent}%  " +
                                  $"Wage: {FormatMoney(_state.Physio.WeeklySalary)}/wk");
            else
                Console.WriteLine("  PHYSIO:  (none)");

            Console.WriteLine();
            Console.WriteLine(_state.Club.HasCoach ? "  1. Sack coach" : "  1. Hire coach");
            Console.WriteLine(_state.Club.HasPhysio ? "  2. Sack physio" : "  2. Hire physio");
            Console.WriteLine("  0. Back");
            Console.WriteLine();

            int choice = ReadInt("  Choice: ", 0, 2);
            if (choice == 0) break;

            if (choice == 1)
            {
                if (_state.Club.HasCoach)
                {
                    Console.Write($"  Sack {_state.Coach!.Name.TrimEnd()}? [Y/N] ");
                    if (ReadYesNo()) StaffService.SackCoach(_state);
                }
                else
                {
                    var coach = StaffService.HireCoach(_state, _rng);
                    if (coach != null)
                    {
                        Console.WriteLine($"  Hired: {coach.Name.TrimEnd()}  " +
                                          $"Skill: {coach.SkillPercent}%  " +
                                          $"Wage: {FormatMoney(coach.WeeklySalary)}/wk");
                        Pause();
                    }
                }
            }
            else
            {
                if (_state.Club.HasPhysio)
                {
                    Console.Write($"  Sack {_state.Physio!.Name.TrimEnd()}? [Y/N] ");
                    if (ReadYesNo()) StaffService.SackPhysio(_state);
                }
                else
                {
                    var physio = StaffService.HirePhysio(_state, _rng);
                    if (physio != null)
                    {
                        Console.WriteLine($"  Hired: {physio.Name.TrimEnd()}  " +
                                          $"Skill: {physio.SkillPercent}%  " +
                                          $"Wage: {FormatMoney(physio.WeeklySalary)}/wk");
                        Pause();
                    }
                }
            }
        }
    }

    private void SaveGame()
    {
        SaveLoadService.Save(_state, SaveLoadService.DefaultSavePath);
        Console.WriteLine("  Game saved.");
        Pause();
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private void StatusBar()
    {
        LeagueService.Sort(_state.CurrentLeague, _state.Club.PointsPerWin);
        int pos = _state.CurrentLeague.Entries
            .FindIndex(e => e.TeamName.Trim() == _state.Club.Name.Trim()) + 1;
        string posStr = pos > 0 ? pos.ToString() : "?";

        Separator();
        Console.WriteLine(
            $"  The Manager  |  {_state.Club.Name.TrimEnd(),-9} " +
            $" Div {(int)_state.Club.Division}  Pos:{posStr,2}" +
            $"  |  Week {_state.CurrentWeek,2}  |  {FormatMoney(_state.Finances.BankBalance)}");
        Separator();
    }

    private static void Banner()
    {
        Console.Clear();
        Console.WriteLine("================================================");
        Console.WriteLine("           F O O T B A L L  B O S S");
        Console.WriteLine("     Based on Football Director II  (1988)");
        Console.WriteLine("================================================");
        Console.WriteLine();
    }

    private static void Header(string title)
    {
        Separator();
        Console.WriteLine($"  {title}");
        Separator();
    }

    private static void Separator() =>
        Console.WriteLine("------------------------------------------------");

    private static string FormatMoney(double amount)
    {
        string sign = amount < 0 ? "-" : string.Empty;
        double abs  = Math.Abs(amount);
        return abs >= 1_000_000 ? $"{sign}£{abs / 1_000_000:F2}M"
             : abs >= 1_000     ? $"{sign}£{abs / 1000:N0}k"
             : $"{sign}£{abs:N0}";
    }

    private static void PrintFinanceLine(string label, double value)
    {
        if (value == 0) return;
        string sign = value >= 0 ? "+" : string.Empty;
        Console.WriteLine($"  {label,-22}  {sign}{FormatMoney(value)}");
    }

    private static void Pause(string msg = "  Press any key...")
    {
        Console.WriteLine();
        Console.Write(msg);
        Console.ReadKey(true);
        Console.WriteLine();
    }

    private static bool ReadYesNo()
    {
        while (true)
        {
            string input = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();
            if (input == "Y") return true;
            if (input == "N") return false;
            Console.Write("  Y or N: ");
        }
    }

    private static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int v) && v >= min && v <= max)
                return v;
            Console.WriteLine($"  Please enter a number between {min} and {max}.");
        }
    }

    private static CupFixture ToCupFixture(CupFixturePair pair) => new()
    {
        HomeTeam     = pair.HomeTeamName,
        AwayTeam     = pair.AwayTeamName,
        HomeDivision = (Division)Math.Clamp(pair.HomeDivision, 1, 4),
        AwayDivision = (Division)Math.Clamp(pair.AwayDivision, 1, 4)
    };
}
