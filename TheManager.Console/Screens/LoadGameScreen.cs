using Spectre.Console;
using TheManager.Models;
using TheManager.Services;

namespace TheManager.ConsoleApp.Screens;

internal static class LoadGameScreen
{
    private const string BackOption = "← Back";

    public static GameState? Show(ISaveService saveService)
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();

        var slots = saveService.ListSlots();

        if (slots.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]  No saves found.[/]");
            Ui.Pause();
            return null;
        }

        var labels = slots
            .Select(s => $"{s.SlotName,-24} {s.ClubName,-20} {Ui.DivisionName((Division)s.Division),-16}  Season {s.Season}  Week {s.Week}  (saved {s.SavedAt:yyyy-MM-dd HH:mm})")
            .ToList();
        labels.Add(BackOption);

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select a save slot:[/]")
                .AddChoices(labels));

        if (choice == BackOption)
            return null;

        var index = labels.IndexOf(choice);
        return saveService.Load(slots[index].SlotName);
    }
}
