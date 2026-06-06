using Spectre.Console;
using TheManager.Models;
using TheManager.Services;

namespace TheManager.ConsoleApp.Screens;

internal static class SaveGameScreen
{
    public static void Show(ISaveService saveService, GameState state)
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]SAVE GAME[/]");
        AnsiConsole.WriteLine();

        var slots = saveService.ListSlots();
        if (slots.Count > 0)
        {
            AnsiConsole.MarkupLine("  [dim]Existing saves:[/]");
            foreach (var s in slots)
                AnsiConsole.MarkupLine($"    {Markup.Escape(s.SlotName)}  [dim]({Markup.Escape(s.ClubName)}, Div {s.Division}, Season {s.Season}, Week {s.Week})[/]");
            AnsiConsole.WriteLine();
        }

        var slotName = AnsiConsole.Prompt(
            new TextPrompt<string>("  [bold]Slot name:[/]")
                .DefaultValue("Quick save")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(slotName))
            slotName = "Quick save";

        var existing = slots.FirstOrDefault(s =>
            string.Equals(s.SlotName, slotName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            var overwrite = AnsiConsole.Confirm(
                $"  Overwrite '[bold]{Markup.Escape(existing.SlotName)}[/]'?",
                defaultValue: false);
            if (!overwrite)
                return;
        }

        saveService.Save(slotName, state);
        AnsiConsole.MarkupLine("  [green]Saved.[/]");
        Ui.Pause();
    }
}
