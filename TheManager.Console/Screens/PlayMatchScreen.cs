using Spectre.Console;
using TheManager.Models;

namespace TheManager.ConsoleApp.Screens;

internal static class PlayMatchScreen
{
    public static void ShowResult(MatchResult result, GameState state)
    {
        Ui.Header("MATCH RESULT");

        string homeTeam = result.IsHomeGame ? result.OurClubName.Trim() : result.OpponentName.Trim();
        string awayTeam = result.IsHomeGame ? result.OpponentName.Trim() : result.OurClubName.Trim();
        int homeScore   = result.IsHomeGame ? result.OurScore   : result.TheirScore;
        int awayScore   = result.IsHomeGame ? result.TheirScore : result.OurScore;

        (string label, string color) = result.OurScore > result.TheirScore ? ("WIN",  "green")
                                     : result.OurScore == result.TheirScore ? ("DRAW", "yellow")
                                     : ("LOSS", "red");

        AnsiConsole.MarkupLine($"  [bold green]{homeTeam}[/]   [bold]{homeScore} – {awayScore}[/]   [bold]{awayTeam}[/]");
        AnsiConsole.MarkupLine($"  Result: [{color}][bold]{label}[/][/]");
        AnsiConsole.WriteLine();

        // Goal timeline
        var goals = result.Goals.OrderBy(g => g.Minute).ToList();
        if (goals.Count > 0)
        {
            var table = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn(new TableColumn("").Width(6))
                .AddColumn(new TableColumn(""));

            foreach (var goal in goals)
            {
                string scorer = goal.IsOurGoal
                    ? (string.IsNullOrWhiteSpace(goal.Scorer) ? result.OurClubName.Trim() : goal.Scorer.Trim())
                    : result.OpponentName.Trim();
                string goalColor = goal.IsOurGoal ? "green" : "red";
                table.AddRow($"[dim]{goal.Minute}'[/]", $"[{goalColor}]{scorer}[/]");
            }

            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("  [dim]No goals scored[/]");
        }

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
                    $"[dim]{f.HomeTeam}[/]",
                    $"[bold]{f.HomeScore} – {f.AwayScore}[/]",
                    $"[dim]{f.AwayTeam}[/]");

            AnsiConsole.Write(otherTable);
            AnsiConsole.WriteLine();
        }

        if (result.IsHomeGame && state.Finances.LastMatchAttendance > 0)
            AnsiConsole.MarkupLine($"  Attendance: [cyan]{state.Finances.LastMatchAttendance:N0}[/]   Gate: [cyan]{Ui.FormatMoney(state.Finances.LastMatchGateMoney)}[/]");

        AnsiConsole.MarkupLine($"  Bank balance: [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]   Morale: [yellow]{state.Club.TeamMorale}[/]");

        Ui.Pause();
    }
}
