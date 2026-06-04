using Spectre.Console;
using TheManager.Services;

namespace TheManager.ConsoleApp.Screens;

internal static class SackMyselfScreen
{
    public static void Show(GameService gameService)
    {
        Ui.Header("RESIGN");

        AnsiConsole.MarkupLine(
            $"  You are currently managing [bold]{Markup.Escape(gameService.State.Club.Name.Trim())}[/]" +
            $" in Division {(int)gameService.State.Club.Division}.");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  Resigning will move you to a lower-division club immediately.");
        AnsiConsole.WriteLine();

        bool confirmed = AnsiConsole.Confirm("  Are you sure you want to resign?", defaultValue: false);

        if (!confirmed)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [dim]Resignation cancelled.[/]");
            Ui.Pause();
            return;
        }

        var (newClubName, newDivision) = gameService.SackMyself();

        SackingScreen.Show("You have resigned.", newClubName, newDivision);
    }
}
