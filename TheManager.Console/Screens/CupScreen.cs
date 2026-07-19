using Spectre.Console;
using TheManager.Models;
using TheManager.Services;

namespace TheManager.ConsoleApp.Screens;

/// <summary>
/// FA Cup overview: the current round's draw plus the full round-by-round
/// results history. Equivalent to the BASIC "Cup fixtures" view (lines
/// 1527–1548) and the "FA cup draw" menu entry (line 34).
/// </summary>
internal static class CupScreen
{
    private const int TargetWidth  = 120;
    private const int TargetHeight = 40;

    public static void Show(GameState state)
    {
        Ui.ResizeConsole(TargetWidth, TargetHeight);
        Ui.Header($"FA CUP  ·  {state.Club.Name.Trim()}");

        var cup = state.FACup;
        string ourName = state.Club.Name.Trim();

        // ── Our status ────────────────────────────────────────────────────────
        string status = cup.CurrentRound switch
        {
            CupRound.Winner     => "[bold green]FA CUP WINNERS![/]",
            CupRound.NotEntered => cup.RoundHistory.Count == 0
                ? "[dim]The competition has not started yet.[/]"
                : "[dim]Not yet involved this season.[/]",
            _ => WeStillIn(cup, ourName)
                ? $"Still in the cup — reached [bold]{cup.RoundName}[/]"
                : $"[red]Out of the cup[/] — eliminated in the [bold]{cup.RoundName}[/]"
        };
        AnsiConsole.MarkupLine($"  {status}");
        AnsiConsole.WriteLine();

        // ── Current round draw ────────────────────────────────────────────────
        if (cup.CurrentRoundFixtures.Count > 0)
        {
            var round = CupService.RoundForIndex(cup.RoundHistory.Count);
            string wembley = CupService.IsNeutralVenue(round) ? "  ·  WEMBLEY" : "";
            AnsiConsole.MarkupLine($"  [bold yellow]THE DRAW — {CupService.RoundDisplayName(round).ToUpperInvariant()}{wembley}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(BuildDrawTable(cup.CurrentRoundFixtures, ourName));
            AnsiConsole.WriteLine();
        }

        // ── Completed rounds, in the order they were played ───────────────────
        foreach (var record in cup.RoundHistory)
        {
            AnsiConsole.MarkupLine($"  [bold dim]{CupService.RoundDisplayName(record.Round).ToUpperInvariant()} RESULTS[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(BuildResultsTable(record.Results, ourName));
            AnsiConsole.WriteLine();
        }

        if (cup.CurrentRoundFixtures.Count == 0 && cup.RoundHistory.Count == 0)
            AnsiConsole.MarkupLine("  [dim]The first-round draw is made at the start of the season.[/]");

        Ui.Pause();
    }

    private static bool WeStillIn(CupCompetition cup, string ourName)
    {
        if (CupService.FindPlayerFixture(cup.CurrentRoundFixtures, ourName) != null)
            return true;

        var lastRound = cup.RoundHistory.LastOrDefault();
        return lastRound?.Results.Any(t => t.Winner.Trim().Equals(ourName, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static Table BuildDrawTable(IReadOnlyList<CupFixture> fixtures, string ourName)
    {
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("").RightAligned())
            .AddColumn(new TableColumn("").Centered().Width(3))
            .AddColumn(new TableColumn(""));

        foreach (var tie in fixtures)
            table.AddRow(TeamCell(tie.HomeTeam, ourName), "[dim]v[/]", TeamCell(tie.AwayTeam, ourName));

        return table;
    }

    private static Table BuildResultsTable(IReadOnlyList<CupFixture> results, string ourName)
    {
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("").RightAligned())
            .AddColumn(new TableColumn("").Centered().Width(7))
            .AddColumn(new TableColumn(""));

        foreach (var tie in results)
        {
            string pens = tie.WonOnPenalties
                ? $" [dim]({tie.HomePenalties}–{tie.AwayPenalties} pens)[/]"
                : "";
            table.AddRow(
                TeamCell(tie.HomeTeam, ourName),
                $"[bold]{tie.HomeScore} – {tie.AwayScore}[/]",
                $"{TeamCell(tie.AwayTeam, ourName)}{pens}");
        }

        return table;
    }

    private static string TeamCell(string team, string ourName)
    {
        string trimmed = team.Trim();
        return trimmed.Equals(ourName, StringComparison.OrdinalIgnoreCase)
            ? $"[bold green]{Markup.Escape(trimmed)}[/]"
            : $"[dim]{Markup.Escape(trimmed)}[/]";
    }
}
