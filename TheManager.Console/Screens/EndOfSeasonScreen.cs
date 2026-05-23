using Spectre.Console;
using TheManager.Models;

namespace TheManager.ConsoleApp.Screens;

internal static class EndOfSeasonScreen
{
    public static bool Show(GameState state)
    {
        Ui.Header("END OF SEASON");

        var history = state.SeasonHistory.LastOrDefault();
        if (history != null)
        {
            AnsiConsole.MarkupLine($"  Season:           [bold]{history.SeasonNumber}[/]");
            AnsiConsole.MarkupLine($"  Final position:   [bold]{history.FinalLeaguePosition}[/]");
            AnsiConsole.MarkupLine($"  Division:         [bold]{(int)history.Division}[/]");
            AnsiConsole.MarkupLine($"  Manager rating:   [bold]{state.Club.ManagerRating}%[/]");
            AnsiConsole.MarkupLine($"  Bank balance:     [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]");
            AnsiConsole.WriteLine();

            // Promotion / relegation banner
            if (state.SeasonHistory.Count >= 2)
            {
                var prev = state.SeasonHistory[^2];
                if (state.Club.Division < prev.Division)
                    AnsiConsole.MarkupLine("  [bold green]*** PROMOTED! ***[/]");
                else if (state.Club.Division > prev.Division)
                    AnsiConsole.MarkupLine("  [bold red]*** RELEGATED! ***[/]");
            }
            else
            {
                // First season — infer from position
                if (history.FinalLeaguePosition <= 2 && history.Division != Division.One)
                    AnsiConsole.MarkupLine("  [bold green]*** PROMOTED! ***[/]");
                else if (history.FinalLeaguePosition >= 19 && history.Division != Division.Four)
                    AnsiConsole.MarkupLine("  [bold red]*** RELEGATED! ***[/]");
            }

            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine($"  New season begins — Division [bold]{(int)state.Club.Division}[/]");
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .AddChoices("Continue to new season", "Quit"));

        return choice == "Continue to new season";
    }
}
