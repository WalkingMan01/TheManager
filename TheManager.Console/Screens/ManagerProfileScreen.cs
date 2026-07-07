using Spectre.Console;
using TheManager.Models;

namespace TheManager.ConsoleApp.Screens;

internal static class ManagerProfileScreen
{
    public static void Show(GameState state)
    {
        Club club = state.Club;
        Ui.Header($"MANAGER PROFILE  ·  {club.ManagerName.Trim()}");

        // ── Contract ──────────────────────────────────────────────────────────
        AnsiConsole.MarkupLine("  [bold]Contract[/]");
        AnsiConsole.MarkupLine($"    Wage      : [cyan]{Ui.FormatMoney(club.ManagerWeeklyWage)}/wk[/]");
        AnsiConsole.MarkupLine($"    Remaining : {ContractLabel(club.ManagerContractWeeks)}");
        string ratingStr = club.ManagerRating > 0 ? $"[cyan]{club.ManagerRating}%[/]" : "[dim]— (updated end of season)[/]";
        AnsiConsole.MarkupLine($"    Rating    : {ratingStr}");
        AnsiConsole.WriteLine();

        // ── This season ───────────────────────────────────────────────────────
        AnsiConsole.MarkupLine("  [bold]This Season[/]");
        AnsiConsole.MarkupLine($"    Club      : [bold]{Markup.Escape(club.Name.Trim())}[/]  ({Ui.DivisionName(club.Division)})");

        int livePosition = Math.Max(1,
            state.CurrentLeague.Entries
                .FindIndex(e => e.TeamName.Trim() == club.Name.Trim()) + 1);
        AnsiConsole.MarkupLine($"    Position  : {Ordinal(livePosition)}");
        AnsiConsole.MarkupLine($"    Morale    : {MoraleLabel(club.TeamMorale)}");

        var entry = state.CurrentLeague.Entries
            .FirstOrDefault(e => e.TeamName.Trim() == club.Name.Trim());
        if (entry is not null)
        {
            int    gd    = entry.GoalDifference;
            string gdStr = gd > 0 ? $"+{gd}" : gd.ToString();
            AnsiConsole.MarkupLine(
                $"    Record    : [green]{entry.Won}W[/]  [yellow]{entry.Drawn}D[/]  [red]{entry.Lost}L[/]" +
                $"   {entry.GoalsFor}–{entry.GoalsAgainst}  GD {gdStr}");
            AnsiConsole.MarkupLine($"    Points    : [bold]{entry.Points(club.PointsPerWin)}[/]");
        }

        var cups = new List<string>();
        if (club.LeagueCupRound > CupRound.NotEntered) cups.Add($"League Cup: {CupRoundLabel(club.LeagueCupRound)}");
        if (club.FACupRound     > CupRound.NotEntered) cups.Add($"FA Cup: {CupRoundLabel(club.FACupRound)}");
        if (state.European is not null)                cups.Add("In European competition");
        if (cups.Count > 0)
            AnsiConsole.MarkupLine($"    Cups      : {string.Join("   |   ", cups)}");

        AnsiConsole.WriteLine();

        // ── Career summary ────────────────────────────────────────────────────
        AnsiConsole.MarkupLine("  [bold]Career Summary[/]");
        AnsiConsole.MarkupLine($"    Seasons managed : {state.SeasonsPlayed}");

        int lcWins  = state.SeasonHistory.Count(s => s.WonLeagueCup);
        int faWins  = state.SeasonHistory.Count(s => s.WonFACup);
        int eurWins = state.SeasonHistory.Count(s => s.WonEuropean);

        if (lcWins + faWins + eurWins > 0)
        {
            var parts = new List<string>();
            if (lcWins  > 0) parts.Add($"League Cup ×{lcWins}");
            if (faWins  > 0) parts.Add($"FA Cup ×{faWins}");
            if (eurWins > 0) parts.Add($"European ×{eurWins}");
            AnsiConsole.MarkupLine($"    Trophies        : [bold yellow]{string.Join("   |   ", parts)}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("    Trophies        : [dim]None yet[/]");
        }

        var prevClubs = state.PreviousClubs
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => Markup.Escape(c.Trim()))
            .ToList();
        if (prevClubs.Count > 0)
            AnsiConsole.MarkupLine($"    Previous clubs  : {string.Join(" → ", prevClubs)}");

        // ── Season history ────────────────────────────────────────────────────
        if (state.SeasonHistory.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [bold]Season History[/]");

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[dim]Season[/]").RightAligned())
                .AddColumn(new TableColumn("[dim]League[/]").RightAligned())
                .AddColumn(new TableColumn("[dim]Pos[/]").RightAligned())
                .AddColumn(new TableColumn("[dim]Lg Cup[/]"))
                .AddColumn(new TableColumn("[dim]FA Cup[/]"))
                .AddColumn(new TableColumn("[dim]Trophies[/]").Centered());

            foreach (var record in state.SeasonHistory)
            {
                var won = new List<string>();
                if (record.WonLeagueCup) won.Add("LC");
                if (record.WonFACup)     won.Add("FA");
                if (record.WonEuropean)  won.Add("EUR");
                string trophyCell = won.Count > 0
                    ? $"[bold yellow]{string.Join(" ", won)}[/]"
                    : "[dim]—[/]";

                table.AddRow(
                    record.SeasonNumber.ToString(),
                    Ui.DivisionShort(record.Division),
                    Ordinal(record.FinalLeaguePosition),
                    record.LeagueCupRoundReached > 0 ? $"Rd {record.LeagueCupRoundReached}" : "[dim]—[/]",
                    record.FACupRoundReached > 0     ? $"Rd {record.FACupRoundReached}" : "[dim]—[/]",
                    trophyCell);
            }

            AnsiConsole.Write(table);
        }

        Ui.Pause();
    }

    private static string ContractLabel(int weeks) => weeks switch
    {
        <= 0  => "[bold red]Expired[/]",
        <= 8  => $"[red]{weeks} weeks[/]",
        <= 20 => $"[yellow]{weeks} weeks[/]",
        _     => $"[green]{weeks} weeks[/]"
    };

    private static string MoraleLabel(int morale) => morale switch
    {
        >= 75 => $"[green]{morale}[/] (high)",
        >= 45 => $"[yellow]{morale}[/] (average)",
        _     => $"[red]{morale}[/] (low)"
    };

    private static string CupRoundLabel(CupRound round) => round switch
    {
        CupRound.Round1       => "Round 1",
        CupRound.Round2       => "Round 2",
        CupRound.Round3       => "Round 3",
        CupRound.Round4       => "Round 4",
        CupRound.Round5       => "Round 5",
        CupRound.QuarterFinal => "Quarter-final",
        CupRound.SemiFinal    => "Semi-final",
        CupRound.Final        => "Final",
        CupRound.Winner       => "[bold yellow]Winner![/]",
        _                     => "—"
    };

    private static string Ordinal(int n) => (n % 100) switch
    {
        11 or 12 or 13 => $"{n}th",
        _ => (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        }
    };
}
