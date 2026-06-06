using Spectre.Console;
using TheManager.Services;

namespace TheManager.ConsoleApp.Screens;

internal enum TitleChoice { NewGame, Continue, Quit }

internal static class TitleScreen
{
    public static TitleChoice Show(ISaveService saveService)
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new FigletText("THE MANAGER").Centered().Color(Color.Green));
        AnsiConsole.MarkupLine("[dim]  A port of Football Director II — D&H Games, 1988[/]");
        AnsiConsole.WriteLine();

        var choices = new List<string>();
        if (saveService.AnySaveExists())
            choices.Add("Continue");
        choices.Add("New Game");
        choices.Add("Quit");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .AddChoices(choices));

        return choice switch
        {
            "Continue" => TitleChoice.Continue,
            "Quit"     => TitleChoice.Quit,
            _          => TitleChoice.NewGame
        };
    }
}
