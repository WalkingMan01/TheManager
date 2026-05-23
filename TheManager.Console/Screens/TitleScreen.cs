using Spectre.Console;

namespace TheManager.ConsoleApp.Screens;

internal static class TitleScreen
{
    public static void Show()
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new FigletText("THE MANAGER").Centered().Color(Color.Green));
        AnsiConsole.MarkupLine("[dim]  A port of Football Director II — D&H Games, 1988[/]");
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .AddChoices("New Game", "Quit"));

        if (choice == "Quit")
            Environment.Exit(0);
    }
}
