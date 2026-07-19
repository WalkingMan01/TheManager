using Spectre.Console;
using Spectre.Console.Rendering;
using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.ConsoleApp.Screens;

internal static class PlayMatchScreen
{
    public static void ShowResult(MatchResult result, GameState state, Random rng)
    {
        if (result.WasRestDay)
        {
            ShowRestDay(result, state, rng);
            return;
        }

        string header = result.MatchType == MatchType.FACup
            ? $"FA CUP — {result.CupRoundName?.ToUpperInvariant()}{(result.IsNeutralVenue ? "  ·  WEMBLEY" : "")}"
            : "MATCH RESULT";
        Ui.Header(header);

        string homeTeam = result.IsHomeGame ? result.OurClubName.Trim() : result.OpponentName.Trim();
        string awayTeam = result.IsHomeGame ? result.OpponentName.Trim() : result.OurClubName.Trim();

        var sortedGoals = result.Goals.OrderBy(g => g.Minute).ToList();
        int homeScore   = 0;
        int awayScore   = 0;
        var events      = new List<(string Minute, string Text, string Color)>();

        // Animate the match clock, revealing goals as they happen
        AnsiConsole.Live(MatchDisplay(homeTeam, awayTeam, homeScore, awayScore, "0'", events, result.IsHomeGame))
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .Start(ctx =>
            {
                for (int min = 1; min <= result.MatchLength; min++)
                {
                    Thread.Sleep(40);
                    string minuteStr = min <= 90 ? $"{min}'" : $"90+{min - 90}'";

                    if (min == 45)
                        events.Add(("HT", "HALF TIME", "dim"));

                    foreach (var goal in sortedGoals.Where(g => g.Minute == min))
                    {
                        if (goal.IsOurGoal)
                        {
                            if (result.IsHomeGame) homeScore++; else awayScore++;
                            string scorer = string.IsNullOrWhiteSpace(goal.Scorer)
                                ? result.OurClubName.Trim()
                                : goal.Scorer.Trim();
                            events.Add((minuteStr, Markup.Escape(scorer), "green"));
                        }
                        else
                        {
                            if (result.IsHomeGame) awayScore++; else homeScore++;
                            events.Add((minuteStr, Markup.Escape(result.OpponentName.Trim()), "red"));
                        }
                    }

                    foreach (var inc in result.Incidents.Where(i => i.Minute == min))
                    {
                        string text = inc.Type switch
                        {
                            IncidentType.Injury     => $"{inc.PlayerName.Trim()} INJURED ({inc.WeeksOut} wk)",
                            IncidentType.RedCard    => $"{inc.PlayerName.Trim()} SENT OFF",
                            IncidentType.YellowCard => $"{inc.PlayerName.Trim()} booked",
                            _                       => ""
                        };
                        string color = inc.Type == IncidentType.YellowCard ? "yellow" : "red";
                        events.Add((minuteStr, Markup.Escape(text), color));
                    }

                    ctx.UpdateTarget(MatchDisplay(homeTeam, awayTeam, homeScore, awayScore, minuteStr, events, result.IsHomeGame));
                    ctx.Refresh();
                }
            });

        AnsiConsole.WriteLine();

        // ── Penalty shootout — played out kick by kick as part of the match ───
        if (result.Shootout != null)
            PlayOutShootout(result, rng);

        (string label, string badgeColor) =
              result.Shootout != null
                ? result.Shootout.WeWon
                    ? ($"WIN (pens {result.Shootout.OurScore}–{result.Shootout.TheirScore})",  "green")
                    : ($"LOSS (pens {result.Shootout.OurScore}–{result.Shootout.TheirScore})", "red")
            : result.OurScore > result.TheirScore  ? ("WIN",  "green")
            : result.OurScore == result.TheirScore ? ("DRAW", "yellow")
            : ("LOSS", "red");
        AnsiConsole.MarkupLine($"  Result: [{badgeColor}][bold]{label}[/][/]");
        AnsiConsole.WriteLine();

        DrawCupResultsSection(result);

        if (result.OtherFixtures.Count > 0)
        {
            AnsiConsole.MarkupLine("  [bold dim]OTHER RESULTS[/]");
            AnsiConsole.WriteLine();

            var otherTable = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn(new TableColumn("").RightAligned())
                .AddColumn(new TableColumn("").Centered().Width(7))
                .AddColumn(new TableColumn(""));

            foreach (var f in result.OtherFixtures)
                otherTable.AddRow(
                    $"[dim]{Markup.Escape(f.HomeTeam)}[/]",
                    $"[bold]{f.HomeScore} – {f.AwayScore}[/]",
                    $"[dim]{Markup.Escape(f.AwayTeam)}[/]");

            AnsiConsole.Write(otherTable);
            AnsiConsole.WriteLine();
        }

        if (result.IsHomeGame && state.Finances.LastMatchAttendance > 0)
            AnsiConsole.MarkupLine(
                $"  Attendance: [cyan]{state.Finances.LastMatchAttendance:N0}[/]   Gate: [cyan]{Ui.FormatMoney(state.Finances.LastMatchGateMoney)}[/]");

        DrawWeeklyFinances(result);

        AnsiConsole.MarkupLine(
            $"  Bank balance: [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]   Morale: [yellow]{state.Club.TeamMorale}[/]");

        if (result.ScoutFindings.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [bold dim]SCOUT NEWS[/]");
            foreach (var f in result.ScoutFindings)
            {
                string pos = f.Player.Position switch
                {
                    PlayerPosition.Goalkeeper => "GK",
                    PlayerPosition.Defender   => "DEF",
                    PlayerPosition.Midfielder => "MID",
                    PlayerPosition.Attacker   => "ATK",
                    _                         => "—"
                };
                AnsiConsole.MarkupLine(
                    $"  [dim]{Markup.Escape(f.ScoutName)}[/] found " +
                    $"[bold]{Markup.Escape(f.Player.Name.Trim())}[/] " +
                    $"({pos}, skill {f.Player.Skill:F1}) at {Markup.Escape(f.SourceClubName)}");
            }
        }

        if (result.ExpiringPlayers.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [bold dim]EXPIRING CONTRACTS — LAST CHANCE[/]");
            foreach (var (slot, player) in result.ExpiringPlayers)
                ShowLastChanceNegotiation(player, slot, state, rng);
        }

        if (result.NewSuspensions.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [bold dim]SUSPENSIONS[/]");
            foreach (var s in result.NewSuspensions)
            {
                string reason = s.Reason == SuspensionReason.RedCard
                    ? "sent off"
                    : "five yellow cards";
                AnsiConsole.MarkupLine(
                    $"  [red]{Markup.Escape(s.PlayerName.Trim())}[/] suspended for " +
                    $"{s.MatchesOut} match{(s.MatchesOut == 1 ? "" : "es")} ({reason})");
            }
        }

        Ui.Pause();
    }

    // ── Penalty shootout playback ─────────────────────────────────────────────

    private static void PlayOutShootout(MatchResult result, Random rng)
    {
        var shootout = result.Shootout!;

        AnsiConsole.MarkupLine(
            $"  [bold yellow]FULL TIME — {result.OurScore}–{result.TheirScore}. The tie goes to penalties…[/]");
        AnsiConsole.WriteLine();
        Thread.Sleep(1200);

        bool suddenDeathAnnounced = false;

        foreach (var kick in shootout.Kicks)
        {
            if (kick.IsSuddenDeath && !suddenDeathAnnounced)
            {
                suddenDeathAnnounced = true;
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("  [bold red]SUDDEN DEATH[/]");
                AnsiConsole.WriteLine();
                Thread.Sleep(1000);
            }

            AnsiConsole.Markup($"  [dim]{Markup.Escape(kick.TakerName)} steps up…[/]");
            Thread.Sleep(900);

            string line  = CommentaryLine(kick, rng);
            string color = kick.Scored ? (kick.IsOurKick ? "green" : "red") : "yellow";
            AnsiConsole.MarkupLine($"  [{color}]{Markup.Escape(line)}[/]" +
                $"   [dim]({kick.OurRunningScore}–{kick.TheirRunningScore})[/]");
            Thread.Sleep(500);
        }

        AnsiConsole.WriteLine();
    }

    private static string CommentaryLine(PenaltyKick kick, Random rng)
    {
        string name = kick.TakerName;
        return kick.Outcome switch
        {
            PenaltyOutcome.Scored => rng.Next(3) switch
            {
                0 => $"{name} scores!",
                1 => $"{name} sends the keeper the wrong way!",
                _ => $"{name} buries it in the bottom corner!"
            },
            PenaltyOutcome.Saved => rng.Next(2) == 0
                ? $"The goalkeeper saves {name}'s penalty!"
                : $"{name}'s penalty is kept out — superb save!",
            PenaltyOutcome.Wide => rng.Next(2) == 0
                ? $"{name} puts the penalty wide!"
                : $"{name}'s shot rolls past the post!",
            _ => $"{name} blazes it over the bar!"
        };
    }

    // ── Rest days & cup news ──────────────────────────────────────────────────

    private static void ShowRestDay(MatchResult result, GameState state, Random rng)
    {
        if (result.MatchType == MatchType.FACup && result.CupRoundName != null)
        {
            Ui.Header($"FA CUP — {result.CupRoundName.ToUpperInvariant()}");
            AnsiConsole.MarkupLine("  [dim]No tie for us this round — rest day.[/]");
        }
        else
        {
            Ui.Header("REST DAY");
            AnsiConsole.MarkupLine("  [dim]No fixture today. Training continues and wages are paid.[/]");
        }
        AnsiConsole.WriteLine();

        DrawCupResultsSection(result);

        DrawWeeklyFinances(result);

        AnsiConsole.MarkupLine(
            $"  Bank balance: [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]   Morale: [yellow]{state.Club.TeamMorale}[/]");

        if (result.ScoutFindings.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [bold dim]SCOUT NEWS[/]");
            foreach (var f in result.ScoutFindings)
                AnsiConsole.MarkupLine(
                    $"  [dim]{Markup.Escape(f.ScoutName)}[/] found " +
                    $"[bold]{Markup.Escape(f.Player.Name.Trim())}[/] at {Markup.Escape(f.SourceClubName)}");
        }

        if (result.ExpiringPlayers.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [bold dim]EXPIRING CONTRACTS — LAST CHANCE[/]");
            foreach (var (slot, player) in result.ExpiringPlayers)
                ShowLastChanceNegotiation(player, slot, state, rng);
        }

        Ui.Pause();
    }

    // ── Weekly finances (legacy "WEEKLY NEWS", FOOT.BAS subroutines 2501–2563) ─

    private static void DrawWeeklyFinances(MatchResult result)
    {
        var report = result.FinanceReport;
        if (report == null) return;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [bold dim]WEEKLY FINANCES[/]");

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("").Width(24))
            .AddColumn(new TableColumn("").RightAligned());

        void Line(string label, double amount)
        {
            if (amount == 0) return;
            string color = amount >= 0 ? "green" : "red";
            string sign  = amount >= 0 ? "+" : "-";
            table.AddRow(
                $"  [dim]{label}[/]",
                $"[{color}]{sign}{Ui.FormatMoney(Math.Abs(amount))}[/]");
        }

        Line("Gate money",         report.GateMoney);
        Line("Wages",             -report.PlayerWageBill);
        Line("Running costs",     -report.RunningCosts);
        Line("Bank interest",      report.BankInterest);
        Line("Police bill",       -report.PoliceBill);
        Line("Lottery income",     report.LotteryIncome);
        Line("Sponsorship",        report.SponsorPayment);
        Line("TV broadcast",       report.TvBroadcastIncome);
        Line("Win bonus paid",    -report.LeagueBonusPaid);
        Line("Cup bonus paid",    -report.CupBonusPaid);
        Line("Loan repayment",    -report.LoanRepayment);
        Line("Mortgage",          -report.MortgageRepayment);
        Line("VAT bill",          -report.VatBill);
        Line("Directors withdrew",-report.DirectorsWithdrawal);
        Line("Insurance payout",   report.InsurancePayout);
        Line("Testimonial",        report.TestimonialPayment);
        Line("Manager of Month",   report.ManagerOfMonthBonus);

        AnsiConsole.Write(table);

        string profitColor = report.WeeklyProfit >= 0 ? "green" : "red";
        string profitSign  = report.WeeklyProfit >= 0 ? "+" : "-";
        AnsiConsole.MarkupLine(
            $"  Net this week: [{profitColor}]{profitSign}{Ui.FormatMoney(Math.Abs(report.WeeklyProfit))}[/]");
        AnsiConsole.WriteLine();
    }

    private static void DrawCupResultsSection(MatchResult result)
    {
        if (result.CupResults.Count == 0) return;

        AnsiConsole.MarkupLine($"  [bold dim]FA CUP {result.CupRoundName?.ToUpperInvariant()} RESULTS[/]");
        AnsiConsole.WriteLine();

        // All ties, split into two side-by-side columns so a 40-tie first round
        // stays readable.
        int half       = (result.CupResults.Count + 1) / 2;
        var leftTable  = BuildCupResultsTable(result.CupResults.Take(half));
        var rightTable = BuildCupResultsTable(result.CupResults.Skip(half));

        var wrapper = new Table()
            .NoBorder()
            .HideHeaders()
            .AddColumn(new TableColumn("left").Padding(0, 0, 4, 0))
            .AddColumn(new TableColumn("right").Padding(0, 0, 0, 0));

        wrapper.AddRow(leftTable, rightTable);
        AnsiConsole.Write(wrapper);
        AnsiConsole.WriteLine();
    }

    private static Table BuildCupResultsTable(IEnumerable<CupFixture> ties)
    {
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("").RightAligned())
            .AddColumn(new TableColumn("").Centered().Width(7))
            .AddColumn(new TableColumn(""));

        foreach (var tie in ties)
        {
            string pens = tie.WonOnPenalties
                ? $" [dim]({tie.HomePenalties}–{tie.AwayPenalties}p)[/]"
                : "";
            table.AddRow(
                $"[dim]{Markup.Escape(tie.HomeTeam.Trim())}[/]",
                $"[bold]{tie.HomeScore} – {tie.AwayScore}[/]",
                $"[dim]{Markup.Escape(tie.AwayTeam.Trim())}[/]{pens}");
        }

        return table;
    }

    private static void ShowLastChanceNegotiation(Player player, int slot, GameState state, Random rng)
    {
        AnsiConsole.WriteLine();

        string name   = player.Name.Trim();
        var    demand = ContractService.GetPlayerDemands(player, state.Club.Division, rng);

        AnsiConsole.MarkupLine(
            $"  [bold]{Markup.Escape(name)}[/]  " +
            $"{Ui.PlayerPositionLabel(player)}  Skill {player.DisplaySkill}  Age {player.DisplayAge}");
        AnsiConsole.MarkupLine("  Contract expired — last chance to keep him.");
        AnsiConsole.MarkupLine("  He is asking for:");
        AnsiConsole.MarkupLine($"    Weekly wage     [cyan]£{demand.StatedWeeklyWage}[/]");
        AnsiConsole.MarkupLine($"    Signing-on fee  [cyan]£{demand.StatedSigningFee}[/]");
        AnsiConsole.MarkupLine($"    Contract        [cyan]{demand.StatedContractWeeks} weeks[/]");
        AnsiConsole.WriteLine();

        string trimmedChoice = AnsiConsole.Prompt(
            new TextPrompt<string>("  [dim]Accept ([bold white]A[/]), counter-offer ([bold white]C[/]), or let him go (Enter):[/] > ")
                .AllowEmpty()).Trim();

        if (trimmedChoice.Equals("A", StringComparison.OrdinalIgnoreCase))
        {
            Apply(player, slot, state, demand.StatedWeeklyWage, demand.StatedSigningFee, demand.StatedContractWeeks, name);
            return;
        }

        if (trimmedChoice.Equals("C", StringComparison.OrdinalIgnoreCase))
        {
            int w  = AnsiConsole.Prompt(new TextPrompt<int>("  Weekly wage (£): > "));
            int f  = AnsiConsole.Prompt(new TextPrompt<int>("  Signing-on fee (£): > "));
            int wk = AnsiConsole.Prompt(new TextPrompt<int>($"  Contract weeks (max {demand.StatedContractWeeks}): > "));

            if (f > state.Finances.BankBalance)
                AnsiConsole.MarkupLine("  [red]You cannot afford that signing-on fee[/]");
            else if (ContractService.EvaluateOffer(demand, w, f, wk))
            {
                Apply(player, slot, state, w, f, wk, name);
                return;
            }
            else
                AnsiConsole.MarkupLine($"  [red]{Markup.Escape(name)} rejects your offer[/]");
        }

        ContractService.ReleasePlayer(state.Squad, slot);
        AnsiConsole.MarkupLine($"  [red]{Markup.Escape(name)} has left the club[/]");
    }

    private static void Apply(Player player, int slot, GameState state, int wage, int fee, int weeks, string name)
    {
        player.WeeklyWage    = wage;
        player.ContractWeeks = weeks;
        ContractService.ApplyRenewal(state.Finances, fee);
        AnsiConsole.MarkupLine($"  [green]{Markup.Escape(name)} agrees to sign[/]");
    }

    private static IRenderable MatchDisplay(
        string homeTeam, string awayTeam,
        int homeScore, int awayScore,
        string minute,
        IReadOnlyList<(string Minute, string Text, string Color)> events,
        bool isHomeGame)
    {
        string homeEsc = Markup.Escape(homeTeam);
        string awayEsc = Markup.Escape(awayTeam);

        string homeStyle = isHomeGame ? "bold green" : "bold";
        string awayStyle = isHomeGame ? "bold"       : "bold green";

        var rows = new List<IRenderable>
        {
            new Markup($"  [{homeStyle}]{homeEsc}[/]   [bold white]{homeScore} – {awayScore}[/]   [{awayStyle}]{awayEsc}[/]"),
            new Markup($"  [dim]{minute}[/]"),
        };

        if (events.Count > 0)
        {
            rows.Add(new Text(""));

            var goalsTable = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn(new TableColumn("").Width(8))
                .AddColumn(new TableColumn(""));

            foreach (var (min, text, col) in events)
                goalsTable.AddRow($"[dim]{min}[/]", $"[{col}]{text}[/]");

            rows.Add(goalsTable);
        }

        return new Rows(rows);
    }
}
