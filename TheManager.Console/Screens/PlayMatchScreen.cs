using Spectre.Console;
using Spectre.Console.Rendering;
using TheManager.Models;

namespace TheManager.ConsoleApp.Screens;

internal static class PlayMatchScreen
{
    public static void ShowResult(MatchResult result, GameState state)
    {
        Ui.Header("MATCH RESULT");

        string homeTeam = result.IsHomeGame ? result.OurClubName.Trim() : result.OpponentName.Trim();
        string awayTeam = result.IsHomeGame ? result.OpponentName.Trim() : result.OurClubName.Trim();

        var sortedGoals = result.Goals.OrderBy(g => g.Minute).ToList();
        int homeScore   = 0;
        int awayScore   = 0;
        var events      = new List<(string Minute, string Text, string Color)>();

        // Animate the match clock, revealing goals as they happen
        AnsiConsole.Live(MatchDisplay(homeTeam, awayTeam, homeScore, awayScore, "0'", events))
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

                    ctx.UpdateTarget(MatchDisplay(homeTeam, awayTeam, homeScore, awayScore, minuteStr, events));
                    ctx.Refresh();
                }
            });

        AnsiConsole.WriteLine();

        (string label, string badgeColor) = result.OurScore > result.TheirScore  ? ("WIN",  "green")
                                          : result.OurScore == result.TheirScore  ? ("DRAW", "yellow")
                                          : ("LOSS", "red");
        AnsiConsole.MarkupLine($"  Result: [{badgeColor}][bold]{label}[/][/]");
        AnsiConsole.WriteLine();

        if (result.OtherFixtures.Count > 0)
        {
            AnsiConsole.MarkupLine("  [bold dim]OTHER RESULTS[/]");
            AnsiConsole.WriteLine();

            var otherTable = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn(new TableColumn("").RightAligned().Width(12))
                .AddColumn(new TableColumn("").Centered().Width(7))
                .AddColumn(new TableColumn("").Width(12));

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

        AnsiConsole.MarkupLine(
            $"  Bank balance: [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]   Morale: [yellow]{state.Club.TeamMorale}[/]");

        Ui.Pause();
    }

    private static IRenderable MatchDisplay(
        string homeTeam, string awayTeam,
        int homeScore, int awayScore,
        string minute,
        IReadOnlyList<(string Minute, string Text, string Color)> events)
    {
        string homeEsc = Markup.Escape(homeTeam);
        string awayEsc = Markup.Escape(awayTeam);

        var rows = new List<IRenderable>
        {
            new Markup($"  [bold green]{homeEsc}[/]   [bold white]{homeScore} – {awayScore}[/]   [bold]{awayEsc}[/]"),
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
