using Spectre.Console;
using TheManager.Models;

namespace TheManager.ConsoleApp.Screens;

internal static class SquadScreen
{
    public static void Show(GameState state)
    {
        string? error = null;

        while (true)
        {
            Ui.Header($"SQUAD  ·  {state.Club.Name.Trim()}");
            DrawTable(state);

            AnsiConsole.WriteLine();
            if (error is not null)
            {
                AnsiConsole.MarkupLine($"  [red]{error}[/]");
                error = null;
            }

            AnsiConsole.MarkupLine("  [dim]Enter two slot numbers to swap (e.g. [bold white]3 9[/]), or press Enter to go back:[/]");
            var input = AnsiConsole.Prompt(new TextPrompt<string>("> ").AllowEmpty());

            if (string.IsNullOrWhiteSpace(input))
                break;

            if (!TryParseSwap(input, out int a, out int b))
            {
                error = "Enter two different numbers between 1 and 20, e.g. \"3 9\"";
                continue;
            }

            (state.Squad[a], state.Squad[b]) = (state.Squad[b], state.Squad[a]);
        }
    }

    private static bool TryParseSwap(string input, out int a, out int b)
    {
        a = b = 0;
        var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out a) || !int.TryParse(parts[1], out b)) return false;
        if (a < 1 || a > 20 || b < 1 || b > 20 || a == b) return false;
        return true;
    }

    private static void DrawTable(GameState state)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[dim]#[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Pos[/]"))
            .AddColumn(new TableColumn("[bold]Name[/]"))
            .AddColumn(new TableColumn("[dim]Skill[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Age[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Temper[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Games[/]").RightAligned());

        AddSection(table, "FIRST TEAM", 1,  11, state.Squad, firstTeam: true);
        AddSection(table, "SUBSTITUTE", 12, 12, state.Squad, firstTeam: false);
        AddSection(table, "RESERVES",   13, 20, state.Squad, firstTeam: false);

        AnsiConsole.Write(table);
    }

    private static void AddSection(Table table, string title, int from, int to, Player?[] squad, bool firstTeam)
    {
        table.AddRow(new Markup(""), new Markup($"[bold dim] {title}[/]"), new Markup(""), new Markup(""), new Markup(""), new Markup(""), new Markup(""));

        for (int slot = from; slot <= to; slot++)
        {
            var player  = squad[slot];
            string pos  = firstTeam ? Ui.PositionLabel(slot) : Ui.PlayerPositionLabel(player);
            string name = player is null ? "[dim]—[/]" : player.IsStar ? $"[yellow]{player.Name}[/]" : player.Name;

            table.AddRow(
                $"[dim]{slot}[/]",
                pos,
                name,
                player?.DisplaySkill.ToString() ?? "[dim]—[/]",
                player?.DisplayAge.ToString()   ?? "[dim]—[/]",
                player?.Temper.ToString()        ?? "[dim]—[/]",
                player?.GamesPlayed.ToString()   ?? "[dim]—[/]");
        }
    }
}
