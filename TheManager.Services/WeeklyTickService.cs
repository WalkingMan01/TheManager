using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Runs all post-match weekly processing: squad appearances, youth coaching,
/// gate money, finance report, financial crisis check, random events, and
/// staff resignations.
///
/// Extracted from GameService.WeeklyUpdate(). Accepts a <see cref="MatchContext"/>
/// in place of the private fields that previously coupled PlayMatch to WeeklyUpdate.
///
/// Corresponds to BASIC lines 751–755, 5408–5411, subroutine 3801, and the
/// weekly finance/event loop in lines 5416–5445.
/// </summary>
public static class WeeklyTickService
{
    /// <summary>
    /// Runs the full weekly tick and returns a summary of what happened.
    /// Call this once per week, after the match result has been recorded and
    /// the week counter has been advanced.
    /// </summary>
    public static WeeklyTickResult Process(GameState gameState, MatchContext ctx, Random rng)
    {
        PlayerService.UpdateSquadAppearances(gameState.Squad);
        ApplyYouthCoaching(gameState, rng);

        gameState.Club.ManagerContractWeeks = Math.Max(0, gameState.Club.ManagerContractWeeks - 1);

        // Decrement each player's contract (BASIC line 188: V(2,I)=V(2,I)+(V(2,I)>0)).
        // Retiring players are released immediately. Non-retiring players whose contract reaches
        // 0 are held for a last-chance renewal offer; the caller removes any non-signers.
        var expiring = new List<(int Slot, Player Player)>();
        for (int i = 1; i <= 20; i++)
        {
            var p = gameState.Squad[i];
            if (p is null) continue;
            if (p.ContractWeeks > 0) p.ContractWeeks--;
            if (p.WeeksInjured > 0)               p.WeeksInjured--;
            if (p.SuspensionMatchesRemaining > 0)  p.SuspensionMatchesRemaining--;
            if (p.ContractWeeks == 0)
            {
                if (p.IsRetiring)
                    gameState.Squad[i] = null;
                else
                    expiring.Add((i, p));
            }
        }

        // Recalculate wage bill; exclude expiring players whose contracts just ended.
        gameState.Finances.PlayerWageBill =
            gameState.Squad.Skip(1).Take(20)
                .Where(p => p is not null && p.ContractWeeks > 0)
                .Sum(p => p!.WeeklyWage);

        double attendance = CalculateGateAttendance(gameState, ctx, rng);
        double gateMoney  = attendance * gameState.Club.TicketPriceInPounds;
        gameState.Finances.LastMatchAttendance = attendance;
        gameState.Finances.LastMatchGateMoney  = gateMoney;

        bool hasTestimonial = Enumerable.Range(1, 20)
            .Any(i => (gameState.Squad[i]?.GamesPlayed ?? 0) > 400);

        int    divNum     = (int)gameState.Club.Division;
        double staffWages = StaffService.TotalStaffWageBill(gameState.Coach, gameState.Physio, gameState.Scouts, gameState.YouthTeam);

        var finInput = new WeeklyReportInput
        {
            GateMoney                = gateMoney,
            PlayerWageBill           = gameState.Finances.PlayerWageBill + staffWages,
            InsurancePayout          = gameState.Finances.InsurancePayout,
            LeagueBonus              = 200.0 / divNum,
            CupBonus                 = 300.0 / divNum,
            WonLeagueMatch           = ctx.WonLeagueMatch,
            WonCupMatch              = ctx.WonCupMatch,
            IsManagerOfMonthEligible = false,
            TestimonialPayment       = hasTestimonial,
            Division                 = divNum
        };

        int leaguePosition = Math.Max(1,
            gameState.CurrentLeague.Entries
                .FindIndex(e => e.TeamName.Trim() == gameState.Club.Name.Trim()) + 1);

        var report     = FinanceService.CalculateWeeklyReport(finInput, gameState.Finances, rng);
        var crisis     = FinancialCrisisService.Evaluate(
            gameState.Finances, gameState.Club, gameState.Squad,
            gameState.FixturesPlayed, gameState.CurrentLeague.WeeklyResults, leaguePosition, rng);
        var events     = RandomEventService.EvaluateWeeklyEvents(gameState.Squad, gameState.Club.Name, gameState.AllTeamNames, rng);
        var resignation = StaffService.CheckRandomResignation(gameState.Club, gameState.Coach, gameState.Physio, gameState.Scouts, rng);
        if (resignation.Type == ResignationType.Coach)  gameState.Coach  = null;
        if (resignation.Type == ResignationType.Physio) gameState.Physio = null;
        string? resign = resignation.ResignerName;
        var currentScoutSlots = new Player?[] { gameState.Squad[21], gameState.Squad[22], gameState.Squad[23] };
        var scoutResult       = ScoutReportService.RunWeeklyReports(gameState.Scouts, currentScoutSlots, gameState.AllTeamNames, rng);
        foreach (var (slot, player) in scoutResult.SquadSlotUpdates)
            gameState.Squad[slot] = player;
        gameState.TransferMarket.PlayersBeingSought =
            MarketService.GenerateIncomingInterest(gameState.Squad, gameState.Club.Division, gameState.Club.Name, gameState.AllTeamNames, rng);

        return new WeeklyTickResult(report, crisis, events, resign, attendance, gateMoney, scoutResult.Findings, expiring);
    }

    // ── Youth coaching (BASIC lines 5408–5411) ────────────────────────────────

    private static void ApplyYouthCoaching(GameState gameState, Random rng)
    {
        if (!gameState.Club.HasCoach || gameState.Coach is null || gameState.YouthTeam.Count == 0)
            return;

        int ra = gameState.Coach.SkillPercent / 10;
        int rb = ra > 0 ? rng.Next(ra) / 2 : 0;

        foreach (var youth in gameState.YouthTeam)
        {
            if (youth.SkillPercent + rb > youth.PotentialSkillPercent)
                youth.SkillPercent = Math.Max(0, youth.SkillPercent - rb);
            else
                youth.SkillPercent = Math.Min(99, youth.SkillPercent + rb);
        }
    }

    // ── Gate attendance (BASIC subroutine 3801, lines 3197–3229) ─────────────

    private static double CalculateGateAttendance(GameState gameState, MatchContext ctx, Random rng)
    {
        if (!ctx.WasHomeGame) return 0;

        int divNum = (int)gameState.Club.Division;
        int ourPos = Math.Max(1,
            gameState.CurrentLeague.Entries
                .FindIndex(e => e.TeamName.Trim() == gameState.Club.Name.Trim()) + 1);

        int dn = 50_000 + rng.Next(10_000);
        dn = (int)((double)dn / divNum) / divNum;
        dn -= (int)(1_250.0 / divNum / divNum * ourPos);
        dn -= (int)(1_250.0 / divNum / divNum * ctx.OpponentLeaguePosition);
        if (divNum == 1) dn += dn / 3;
        if (divNum < 3 && gameState.Club.GroundImprovementCost > 0 && dn > 18_721)
            dn = 18_721;

        return Math.Max(500, dn + 1 + rng.Next(50));
    }
}

// ── Result type ───────────────────────────────────────────────────────────────

/// <summary>Summary of everything that happened during the weekly tick.</summary>
public record WeeklyTickResult(
    WeeklyReport                    FinanceReport,
    CrisisResult                    Crisis,
    List<RandomEvent>               Events,
    string?                         Resignation,
    double                          Attendance,
    double                          GateMoney,
    List<ScoutFinding>              ScoutFindings,
    List<(int Slot, Player Player)> ExpiringPlayers);
