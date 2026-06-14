using Spectre.Console;
using TheManager.Models;
using TheManager.Services;

namespace TheManager.ConsoleApp.Screens;

internal static class SquadScreen
{
    private const int TargetWidth  = 100;
    private const int TargetHeight = 50;

    public static void Show(GameState state)
    {
        Ui.ResizeConsole(TargetWidth, TargetHeight);
        string? error = null;

        while (true)
        {
            Ui.Header($"SQUAD  ·  {state.Club.Name.Trim()}");
            DrawLayout(state);

            AnsiConsole.WriteLine();
            if (error is not null)
            {
                AnsiConsole.MarkupLine($"  [red]{error}[/]");
                error = null;
            }

            AnsiConsole.MarkupLine("  [dim]Enter two slot numbers to swap (e.g. [bold white]3 9[/]), [bold white]T<number>[/] to transfer-list a player (e.g. [bold white]T9[/]), or press Enter to go back:[/]");
            var input = AnsiConsole.Prompt(new TextPrompt<string>("> ").AllowEmpty());

            if (string.IsNullOrWhiteSpace(input))
                break;

            if (TryParseTransferListToggle(input, out int slot))
            {
                if (slot < 1 || slot > 20)
                {
                    error = "Enter a player number between 1 and 20, e.g. \"T9\"";
                    continue;
                }

                var player = state.Squad[slot];
                if (player is null)
                {
                    error = $"There is no player in slot {slot}";
                    continue;
                }

                if (!PlayerService.ToggleTransferListed(player))
                    error = $"{player.Name} cannot be transfer-listed";

                continue;
            }

            if (!TryParseSwap(input, out int a, out int b))
            {
                error = "Enter two different numbers between 1 and 20, e.g. \"3 9\"";
                continue;
            }

            (state.Squad[a], state.Squad[b]) = (state.Squad[b], state.Squad[a]);
        }
    }

    private static bool TryParseTransferListToggle(string input, out int slot)
    {
        slot = 0;
        var trimmed = input.Trim();
        if (trimmed.Length < 2 || (trimmed[0] != 'T' && trimmed[0] != 't'))
            return false;

        return int.TryParse(trimmed[1..].Trim(), out slot);
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

    private static void DrawLayout(GameState state)
    {
        var squadTable   = BuildSquadTable(state.Squad);
        var ratingsTable = BuildRatingsTable(state.Squad);

        // Borderless wrapper places ratings to the right of the squad list.
        var wrapper = new Table()
            .NoBorder()
            .HideHeaders()
            .AddColumn(new TableColumn("squad").Padding(0, 0, 1, 0))
            .AddColumn(new TableColumn("ratings").Padding(0, 0, 0, 0));

        wrapper.AddRow(squadTable, ratingsTable);
        AnsiConsole.Write(wrapper);
    }

    private static Table BuildSquadTable(Player?[] squad)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[dim]#[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Pos[/]"))
            .AddColumn(new TableColumn("[bold]Name[/]"))
            .AddColumn(new TableColumn("[dim]Skill[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Age[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Temper[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Games[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Wage[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Ctr[/]").RightAligned());

        AddSection(table, null,        1,  11, squad);
        AddSection(table, "SUBSTITUTE", 12, 12, squad);
        AddSection(table, "RESERVES",   13, 20, squad);
        return table;
    }

    private static Table BuildRatingsTable(Player?[] squad)
    {
        var r = PlayerService.CalculateTeamRatings(squad);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .HideHeaders()
            .AddColumn(new TableColumn("label"))
            .AddColumn(new TableColumn("value").RightAligned());

        table.AddRow("[dim]GK[/]",  RatingCell(r.GoalkeeperRating));
        table.AddRow("[dim]DEF[/]", RatingCell(r.DefenceRating));
        table.AddRow("[dim]MID[/]", RatingCell(r.MidRating));
        table.AddRow("[dim]ATK[/]", RatingCell(r.AttackRating));

        return table;
    }

    private static string RatingCell(int rating) => rating switch
    {
        >= 8 => $"[bold green]{rating}[/]",
        >= 6 => $"[bold yellow]{rating}[/]",
        >= 4 => $"[bold]{rating}[/]",
        _    => $"[red]{rating}[/]"
    };

    private static void AddSection(Table table, string? title, int from, int to, Player?[] squad)
    {
        if (title is not null)
        {
            // Section header goes in the Name column to keep the Pos column narrow.
            table.AddRow(
                new Markup(""),
                new Markup(""),
                new Markup($"[bold dim] {title}[/]"),
                new Markup(""), new Markup(""), new Markup(""), new Markup(""),
                new Markup(""), new Markup(""));
        }

        for (int slot = from; slot <= to; slot++)
        {
            var    player = squad[slot];
            string pos    = Ui.PlayerPositionLabel(player);
            string name   = player is null              ? "[dim]—[/]"
                          : player.IsTransferListed      ? $"[red]{player.Name}[/]"
                          : player.IsStar                ? $"[yellow]{player.Name}[/]"
                          : player.Name;

            string age      = player is null    ? "[dim]—[/]"
                            : player.IsRetiring  ? "[red]RET[/]"
                            : player.DisplayAge.ToString();

            string wage     = player is null ? "[dim]—[/]" : $"£{(int)player.WeeklyWage}";
            string contract = player is null ? "[dim]—[/]"
                            : player.ContractWeeks == 0 ? "[red]exp[/]"
                            : $"{player.ContractWeeks}w";

            table.AddRow(
                $"[dim]{slot}[/]",
                pos,
                name,
                SkillCell(player),
                age,
                player?.Temper.ToString()        ?? "[dim]—[/]",
                player?.GamesPlayed.ToString()   ?? "[dim]—[/]",
                wage,
                contract);
        }
    }

    /// <summary>
    /// Formats a player's displayed skill, appending an indicator when the
    /// player is close to a skill-level change: "+" if the fractional part
    /// of <see cref="Player.Skill"/> is 0.8 or higher (close to leveling up), or
    /// "-" if it is 0.2 or lower (close to dropping a level).
    /// </summary>
    private static string SkillCell(Player? player)
    {
        if (player is null) return "[dim]—[/]";

        double fraction = player.Skill - player.DisplaySkill;
        string indicator = fraction >= 0.8 ? "+"
                          : fraction <= 0.2 ? "-"
                          : "";

        return $"{player.DisplaySkill}{indicator}";
    }

}
