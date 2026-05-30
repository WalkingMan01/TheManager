using Spectre.Console;
using TheManager.Models;

namespace TheManager.ConsoleApp.Screens;

internal static class DifficultyScreen
{
    private const string Easy   = "Easy   — opponents are weaker";
    private const string Normal = "Normal — standard opponent strength";
    private const string Hard   = "Hard   — opponents are stronger";

    public static void Show(GameState state)
    {
        Ui.Header("DIFFICULTY");

        string current = state.DifficultyLevel switch
        {
            -1 => Easy,
            1  => Hard,
            _  => Normal
        };

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Current: [bold]{DifficultyLabel(state.DifficultyLevel)}[/]  — choose a new level:")
                .AddChoices(Easy, Normal, Hard)
                .HighlightStyle(new Style(foreground: Color.Yellow))
                .UseConverter(s => s == current ? $"[green]{s}[/]" : s));

        state.DifficultyLevel = choice switch
        {
            Easy  => -1,
            Hard  =>  1,
            _     =>  0
        };

        AnsiConsole.MarkupLine($"\n  Difficulty set to [bold yellow]{DifficultyLabel(state.DifficultyLevel)}[/].");
        Ui.Pause();
    }

    private static string DifficultyLabel(int level) => level switch
    {
        -1 => "Easy",
        1  => "Hard",
        _  => "Normal"
    };
}
