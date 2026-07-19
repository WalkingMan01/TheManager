using Spectre.Console;
using TheManager.Models;

namespace TheManager.ConsoleApp.Screens;

internal static class EndOfSeasonScreen
{
    public static void Show(GameState state)
    {
        Ui.Header("END OF SEASON");

        var history = state.SeasonHistory.LastOrDefault();
        if (history != null)
        {
            AnsiConsole.MarkupLine($"  Season:           [bold]{history.SeasonNumber}[/]");
            AnsiConsole.MarkupLine($"  Final position:   [bold]{history.FinalLeaguePosition}[/]");
            AnsiConsole.MarkupLine($"  Division:         [bold]{Ui.DivisionName(history.Division)}[/]");
            AnsiConsole.MarkupLine($"  Manager rating:   [bold]{state.Club.ManagerRating}%[/]");
            AnsiConsole.MarkupLine($"  Bank balance:     [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]");
            AnsiConsole.WriteLine();

            // Promotion / relegation banner. The history entry records the
            // division as played (written before promotion/relegation is
            // applied), so comparing it with the club's current division
            // shows exactly what happened this season.
            if (state.Club.Division < history.Division)
                AnsiConsole.MarkupLine("  [bold green]*** PROMOTED! ***[/]");
            else if (state.Club.Division > history.Division)
                AnsiConsole.MarkupLine("  [bold red]*** RELEGATED! ***[/]");

            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine($"  New season begins — [bold]{Ui.DivisionName(state.Club.Division)}[/]");
        AnsiConsole.WriteLine();

        Ui.Pause();
    }
}
