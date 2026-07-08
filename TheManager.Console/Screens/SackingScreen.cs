using Spectre.Console;
using TheManager.Models;

namespace TheManager.ConsoleApp.Screens;

internal static class SackingScreen
{
    public static void Show(string reason, string newClubName, Division newDivision)
    {
        Ui.Header("YOU HAVE BEEN SACKED");

        AnsiConsole.MarkupLine($"  [bold red]{Markup.Escape(reason.ToUpper())}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  The board have terminated your contract. You must seek employment elsewhere.");
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Spectre.Console.Rule("[dim]NEW APPOINTMENT[/]"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"  You have been appointed manager of [bold cyan]{Markup.Escape(newClubName)}[/]" +
            $" in the [bold]{Ui.DivisionName(newDivision)}[/].");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [dim]A new squad and staff are in place. The season continues.[/]");

        Ui.Pause();
    }
}
