using Spectre.Console;
using TheManager.Models;
using TheManager.Services;

namespace TheManager.ConsoleApp.Screens;

internal static class TransferMarketScreen
{
    public static void Show(GameState state, Random rng)
    {
        while (true)
        {
            Ui.Header($"TRANSFER MARKET  ·  {state.Club.Name.Trim()}");

            var listed  = MarketService.GetListedPlayers(state.Squad).ToList();
            var offered = state.TransferMarket.PlayersBeingSought;

            DrawTransferList(listed, offered);

            if (offered.Count > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("  [bold]Incoming offers[/]");
                DrawOffersTable(offered);
            }

            AnsiConsole.WriteLine();

            var choices = new List<string>();
            if (listed.Count < 20) choices.Add("List a player for transfer");
            if (listed.Count > 0)  choices.Add("Unlist a player");
            if (offered.Count > 0) choices.Add("Respond to an offer");
            choices.Add("Back");

            var pick = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]What would you like to do?[/]")
                    .AddChoices(choices));

            switch (pick)
            {
                case "List a player for transfer": ListPlayer(state);              break;
                case "Unlist a player":            UnlistPlayer(state);            break;
                case "Respond to an offer":        RespondToOffer(state);          break;
                default: return;
            }
        }
    }

    // ── Tables ────────────────────────────────────────────────────────────────

    private static void DrawTransferList(
        List<(int slot, Player player)>  listed,
        List<TransferListing>            offered)
    {
        AnsiConsole.MarkupLine("  [bold]Your transfer list[/]");
        AnsiConsole.WriteLine();

        if (listed.Count == 0)
        {
            AnsiConsole.MarkupLine("  [dim]No players currently listed for transfer.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[dim]Slot[/]").RightAligned())
            .AddColumn("[bold]Name[/]")
            .AddColumn("[dim]Pos[/]")
            .AddColumn(new TableColumn("[dim]Skill[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Age[/]").RightAligned())
            .AddColumn("[dim]Interest from[/]");

        foreach (var (slot, player) in listed)
        {
            var interest = offered.FirstOrDefault(o => o.SourceSquadSlot == slot);
            string interestCell = interest is not null
                ? $"[yellow]{Markup.Escape(interest.OwningClubName.Trim())}[/]"
                : "[dim]none[/]";

            table.AddRow(
                $"[dim]{slot}[/]",
                Markup.Escape(player.Name.Trim()),
                PositionAbbr(player.Position),
                $"{player.Skill:F1}",
                player.DisplayAge.ToString(),
                interestCell);
        }

        AnsiConsole.Write(table);
    }

    private static void DrawOffersTable(List<TransferListing> offered)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[dim]Club[/]")
            .AddColumn("[dim]Div[/]")
            .AddColumn("[bold]Player[/]")
            .AddColumn("[dim]Pos[/]")
            .AddColumn(new TableColumn("[dim]Skill[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Their bid[/]").RightAligned());

        foreach (var offer in offered)
        {
            table.AddRow(
                Markup.Escape(offer.OwningClubName.Trim()),
                $"D{RivalDivision(offer.OwningClubIndex)}",
                Markup.Escape(offer.Player.Name.Trim()),
                PositionAbbr(offer.Player.Position),
                $"{offer.Player.Skill:F1}",
                $"[cyan]{Ui.FormatMoney(offer.OfferedBid)}[/]");
        }

        AnsiConsole.Write(table);
    }

    // ── List / unlist ─────────────────────────────────────────────────────────

    private static void ListPlayer(GameState state)
    {
        var candidates = Enumerable.Range(1, 20)
            .Where(i => state.Squad[i] is not null && !MarketService.IsListed(state.Squad[i]!))
            .Select(i => $"{i,2}. {state.Squad[i]!.Name.Trim(),-9}  " +
                         $"{PositionAbbr(state.Squad[i]!.Position),-3}  " +
                         $"skill:{state.Squad[i]!.Skill:F1}  age:{state.Squad[i]!.DisplayAge}")
            .Append("Cancel")
            .ToList();

        if (candidates.Count == 1)
        {
            AnsiConsole.MarkupLine("  [dim]All squad players are already listed.[/]");
            Ui.Pause();
            return;
        }

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]List which player?[/]")
                .AddChoices(candidates));

        if (pick == "Cancel") return;

        int slot = int.Parse(pick.TrimStart().Split('.')[0]);
        MarketService.ListForTransfer(state.Squad[slot]!);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [green]{Markup.Escape(state.Squad[slot]!.Name.Trim())}[/] is now listed for transfer.");
        Ui.Pause();
    }

    private static void UnlistPlayer(GameState state)
    {
        var listed = MarketService.GetListedPlayers(state.Squad).ToList();
        var options = listed
            .Select(p => $"{p.slot,2}. {p.player.Name.Trim(),-9}  " +
                         $"{PositionAbbr(p.player.Position),-3}  skill:{p.player.Skill:F1}")
            .Append("Cancel")
            .ToList();

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Unlist which player?[/]")
                .AddChoices(options));

        if (pick == "Cancel") return;

        int slot = int.Parse(pick.TrimStart().Split('.')[0]);
        MarketService.UnlistFromTransfer(state.Squad[slot]!);

        // Remove any outstanding interest in this player
        state.TransferMarket.PlayersBeingSought.RemoveAll(l => l.SourceSquadSlot == slot);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [yellow]{Markup.Escape(state.Squad[slot]!.Name.Trim())}[/] removed from transfer list.");
        Ui.Pause();
    }

    // ── Respond to offer ──────────────────────────────────────────────────────

    private static void RespondToOffer(GameState state)
    {
        var offered = state.TransferMarket.PlayersBeingSought;

        var options = offered
            .Select((o, i) =>
                $"{i + 1}. {o.OwningClubName.Trim(),-12}  " +
                $"want {o.Player.Name.Trim()}  " +
                $"bid:{Ui.FormatMoney(o.OfferedBid)}")
            .Append("Cancel")
            .ToList();

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Which offer to respond to?[/]")
                .AddChoices(options));

        if (pick == "Cancel") return;

        int idx   = int.Parse(pick.Split('.')[0]) - 1;
        var offer = offered[idx];
        double bid = offer.OfferedBid;

        // Show details
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"  [bold]{Markup.Escape(offer.OwningClubName.Trim())}[/] (D{RivalDivision(offer.OwningClubIndex)}) " +
            $"want to buy [bold]{Markup.Escape(offer.Player.Name.Trim())}[/]  " +
            $"{PositionAbbr(offer.Player.Position)}  skill {offer.Player.Skill:F1}  age {offer.Player.DisplayAge}");
        AnsiConsole.MarkupLine(
            $"  Their offer: [cyan]{Ui.FormatMoney(bid)}[/]");
        AnsiConsole.MarkupLine(
            $"  Your balance: [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]");
        AnsiConsole.WriteLine();

        var response = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]What do you want to do?[/]")
                .AddChoices("Accept", "Decline"));

        if (response == "Accept")
        {
            int squadSlot = offer.SourceSquadSlot;
            var newRecord = MarketService.SellPlayer(state.Squad, state.Finances, state.TransferMarket.PlayersBeingSought, squadSlot, bid);
            if (newRecord is not null) state.RecordSaleName = newRecord;

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                $"  [green]Deal done![/]  {Markup.Escape(offer.Player.Name.Trim())} sold to " +
                $"{Markup.Escape(offer.OwningClubName.Trim())} for [cyan]{Ui.FormatMoney(bid)}[/].");
            AnsiConsole.MarkupLine(
                $"  New balance: [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]");
        }
        else
        {
            // Club withdraws — remove this offer
            state.TransferMarket.PlayersBeingSought.Remove(offer);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                $"  [yellow]Offer declined.[/] {Markup.Escape(offer.OwningClubName.Trim())} will look elsewhere.");
        }

        Ui.Pause();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int RivalDivision(int teamIndex)
        => Math.Clamp((teamIndex - 1) / 20 + 1, 1, 4);

    private static string PositionAbbr(PlayerPosition pos) => pos switch
    {
        PlayerPosition.Goalkeeper => "GK",
        PlayerPosition.Defender   => "DEF",
        PlayerPosition.Midfielder => "MID",
        PlayerPosition.Attacker   => "ATK",
        _                         => pos.ToString()
    };
}
