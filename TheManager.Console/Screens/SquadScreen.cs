using Spectre.Console;
using TheManager.Models;
using TheManager.Services;

namespace TheManager.ConsoleApp.Screens;

internal static class SquadScreen
{
    private const int TargetWidth  = 100;
    private const int TargetHeight = 50;
    private const int TeamSlotMax  = 12;   // slots 1-11 starting XI, 12 substitute

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

            if (TryParseRenegotiate(input, out int rSlot))
            {
                if (rSlot < 1 || rSlot > 20)
                {
                    error = "Enter a player number between 1 and 20, e.g. \"R7\"";
                    continue;
                }

                var rPlayer = state.Squad[rSlot];
                if (rPlayer is null)
                {
                    error = $"There is no player in slot {rSlot}";
                    continue;
                }

                if (rPlayer.IsRetiring)
                {
                    error = $"{rPlayer.Name.Trim()} is retiring and cannot be offered a contract";
                    continue;
                }

                if (rPlayer.ContractWeeks > 10)
                {
                    error = $"{rPlayer.Name.Trim()}'s contract does not expire for {rPlayer.ContractWeeks} weeks";
                    continue;
                }

                error = RunNegotiation(state, rSlot);
                continue;
            }

            if (!TryParseSwap(input, out int a, out int b))
            {
                error = "Enter two different numbers between 1 and 20, e.g. \"3 9\"";
                continue;
            }

            // Slots 1-12 are the team (starting XI + substitute) — an unavailable
            // player (injured or suspended) cannot be swapped into one of them.
            var swapBlocked = false;
            foreach (var (destinationSlot, incoming) in new[] { (a, state.Squad[b]), (b, state.Squad[a]) })
            {
                if (destinationSlot > TeamSlotMax || incoming is not { IsAvailable: false }) continue;
                error = UnavailableReason(incoming);
                swapBlocked = true;
                break;
            }
            if (swapBlocked) continue;

            (state.Squad[a], state.Squad[b]) = (state.Squad[b], state.Squad[a]);
        }
    }

    private static string UnavailableReason(Player player)
    {
        string name = player.Name.Trim();
        return player.WeeksInjured > 0
            ? $"{name} is injured ({player.WeeksInjured}w) and cannot be added to the team"
            : $"{name} is suspended ({player.SuspensionMatchesRemaining}) and cannot be added to the team";
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

    private static bool TryParseRenegotiate(string input, out int slot)
    {
        slot = 0;
        var trimmed = input.Trim();
        if (trimmed.Length < 2 || (trimmed[0] != 'R' && trimmed[0] != 'r'))
            return false;
        return int.TryParse(trimmed[1..].Trim(), out slot);
    }

    /// <summary>
    /// Runs an inline contract negotiation for the player in the given squad slot.
    /// Returns a non-null error string if the negotiation ended without signing,
    /// or null on success (player signed).
    /// </summary>
    private static string? RunNegotiation(GameState state, int slot)
    {
        var player = state.Squad[slot]!;
        var rng    = new Random();
        var demand = ContractService.GetPlayerDemands(player, state.Club.Division, rng);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"  [bold]{Markup.Escape(player.Name.Trim())}[/]  " +
            $"{Ui.PlayerPositionLabel(player)}  Skill {player.DisplaySkill}  Age {player.DisplayAge}");
        string remaining = player.ContractWeeks == 0
            ? "[red]Contract expired[/]"
            : $"Contract expiring in [red]{player.ContractWeeks}[/] week{(player.ContractWeeks == 1 ? "" : "s")}";
        AnsiConsole.MarkupLine($"  {remaining}");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  He is asking for:");
        AnsiConsole.MarkupLine($"    Weekly wage     [cyan]£{demand.StatedWeeklyWage}[/]");
        AnsiConsole.MarkupLine($"    Signing-on fee  [cyan]£{demand.StatedSigningFee}[/]");
        AnsiConsole.MarkupLine($"    Contract        [cyan]{demand.StatedContractWeeks} weeks[/]");
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new TextPrompt<string>("  [dim]Accept his terms ([bold white]A[/]), make a counter-offer ([bold white]C[/]), or press Enter to cancel:[/] > ")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(choice))
            return null;

        int offeredWage, offeredFee, offeredWeeks;

        if (choice.Trim().Equals("A", StringComparison.OrdinalIgnoreCase))
        {
            offeredWage  = demand.StatedWeeklyWage;
            offeredFee   = demand.StatedSigningFee;
            offeredWeeks = demand.StatedContractWeeks;
        }
        else if (choice.Trim().Equals("C", StringComparison.OrdinalIgnoreCase))
        {
            offeredWage = AnsiConsole.Prompt(
                new TextPrompt<int>($"  Weekly wage (£, max {demand.StatedContractWeeks}w contract): > "));
            offeredFee = AnsiConsole.Prompt(
                new TextPrompt<int>("  Signing-on fee (£): > "));
            offeredWeeks = AnsiConsole.Prompt(
                new TextPrompt<int>($"  Contract length in weeks (max {demand.StatedContractWeeks}): > "));

            if (!ContractService.EvaluateOffer(demand, offeredWage, offeredFee, offeredWeeks))
                return $"{player.Name.Trim()} rejects your offer";
        }
        else
        {
            return null;
        }

        // The club can dip into its overdraft to fund a renewal, same as it
        // can for a new signing — blocked only once the resulting balance
        // would cross the same threshold FinancialCrisisService treats as
        // trouble (BankBalance < -OverdraftMaximum). Checked for both the
        // accepted-demand and custom-offer paths.
        double availableFunds = state.Finances.BankBalance + state.Finances.OverdraftMaximum;
        if (offeredFee > availableFunds)
            return $"You cannot afford a signing-on fee of £{offeredFee}, even using the overdraft";

        player.WeeklyWage    = offeredWage;
        player.ContractWeeks = offeredWeeks;
        ContractService.ApplyRenewal(state.Finances, offeredFee);

        AnsiConsole.MarkupLine($"  [green]{Markup.Escape(player.Name.Trim())} agrees to sign[/]");
        Ui.Pause();
        return null;
    }

    private static void DrawLayout(GameState state)
    {
        var squadTable    = BuildSquadTable(state.Squad);
        var ratingsTable  = BuildRatingsTable(state.Squad);
        var commandsTable = BuildCommandsTable();

        // Stack ratings + commands vertically in the right column.
        var rightColumn = new Table()
            .NoBorder()
            .HideHeaders()
            .AddColumn(new TableColumn("content").Padding(0, 0, 0, 0));
        rightColumn.AddRow(ratingsTable);
        rightColumn.AddRow(commandsTable);

        var wrapper = new Table()
            .NoBorder()
            .HideHeaders()
            .AddColumn(new TableColumn("squad").Padding(0, 0, 1, 0))
            .AddColumn(new TableColumn("right").Padding(0, 0, 0, 0));

        wrapper.AddRow(squadTable, rightColumn);
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
            .AddColumn(new TableColumn("[dim]YC[/]").RightAligned())
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

    private static Table BuildCommandsTable()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .HideHeaders()
            .AddColumn(new TableColumn("cmd").Padding(0, 0, 1, 0))
            .AddColumn(new TableColumn("desc"));

        table.AddRow("[bold white]3 9[/]",   "[dim]Swap players[/]");
        table.AddRow("[bold white]T9[/]",    "[dim]Transfer-list[/]");
        table.AddRow("[bold white]R7[/]",    "[dim]Renegotiate[/]");
        table.AddRow("[bold white]Enter[/]", "[dim]Go back[/]");
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
                new Markup(""), new Markup(""), new Markup(""));
        }

        for (int slot = from; slot <= to; slot++)
        {
            var    player = squad[slot];
            string pos    = Ui.PlayerPositionLabel(player);
            string name   = player is null                              ? "[dim]—[/]"
                          : player.WeeksInjured > 0                      ? $"[red]{player.Name} (inj {player.WeeksInjured}w)[/]"
                          : player.SuspensionMatchesRemaining > 0        ? $"[red]{player.Name} (susp {player.SuspensionMatchesRemaining})[/]"
                          : player.IsTransferListed                      ? $"[red]{player.Name}[/]"
                          : player.IsStar                                ? $"[yellow]{player.Name}[/]"
                          : player.Name;

            string age      = player is null    ? "[dim]—[/]"
                            : player.IsRetiring  ? "[red]RET[/]"
                            : player.DisplayAge.ToString();

            string wage     = player is null ? "[dim]—[/]" : $"£{(int)player.WeeklyWage}";
            string contract = player is null              ? "[dim]—[/]"
                            : player.ContractWeeks == 0  ? "[red bold]exp[/]"
                            : player.ContractWeeks <= 10 ? $"[red]{player.ContractWeeks}w[/]"
                            : $"{player.ContractWeeks}w";

            table.AddRow(
                $"[dim]{slot}[/]",
                pos,
                name,
                SkillCell(player),
                age,
                player?.Temper.ToString()        ?? "[dim]—[/]",
                player?.GamesPlayed.ToString()   ?? "[dim]—[/]",
                YellowCardCell(player),
                wage,
                contract);
        }
    }

    private static string YellowCardCell(Player? player)
    {
        if (player is null) return "[dim]—[/]";
        return player.YellowCardsThisSeason >= 4
            ? $"[red]{player.YellowCardsThisSeason}[/]"   // one booking away from suspension
            : player.YellowCardsThisSeason.ToString();
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
