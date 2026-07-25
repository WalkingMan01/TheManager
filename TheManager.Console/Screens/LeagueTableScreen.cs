using Spectre.Console;
using TheManager.Models;

namespace TheManager.ConsoleApp.Screens;

internal static class LeagueTableScreen
{
    public static void Show(GameState state)
    {
        Ui.Header($"LEAGUE TABLE  ·  {Ui.DivisionName(state.Club.Division)}");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[dim]#[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Team[/]"))
            .AddColumn(new TableColumn("[dim]P[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]W[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]D[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]L[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]F[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]A[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Pts[/]").RightAligned());

        string ourName = state.Club.Name.Trim();
        int total     = state.CurrentLeague.Entries.Count;
        var division  = state.CurrentLeague.Division;

        // Promotion (automatic spots, then the 4-team play-off field) and
        // relegation boundaries, per docs/specs/promotion-playoffs.md. Premier
        // League has no promotion; League Two has no relegation (nothing below it).
        bool hasPromotion  = division != Division.One;
        bool hasRelegation = division != Division.Four;
        int  autoSpots     = hasPromotion  ? Constants.AutomaticPromotionSpots(division) : 0;
        int  relegationSpots = hasRelegation ? Constants.RelegationSpots(division)       : 0;

        var dividerBeforePosition = new HashSet<int>();
        if (hasPromotion)
        {
            dividerBeforePosition.Add(autoSpots + 1);     // end of automatic promotion
            dividerBeforePosition.Add(autoSpots + 4 + 1); // end of the play-off field
        }
        if (hasRelegation && relegationSpots > 0)
            dividerBeforePosition.Add(total - relegationSpots + 1);

        int pos = 1;
        foreach (var entry in state.CurrentLeague.Entries)
        {
            if (dividerBeforePosition.Contains(pos))
            {
                AddDivider(table);
            }

            bool isUs = entry.TeamName.Trim() == ourName;
            int  pts  = entry.Points(state.Club.PointsPerWin);
            string nameCell = isUs ? $"[bold green]{entry.TeamName.Trim()}[/]" : entry.TeamName.Trim();
            string ptsCell  = isUs ? $"[bold green]{pts}[/]" : pts.ToString();

            table.AddRow(
                pos++.ToString(),
                nameCell,
                entry.Played.ToString(),
                entry.Won.ToString(),
                entry.Drawn.ToString(),
                entry.Lost.ToString(),
                entry.GoalsFor.ToString(),
                entry.GoalsAgainst.ToString(),
                ptsCell);
        }

        AnsiConsole.Write(table);
        Ui.Pause();
    }

    private static void AddDivider(Table table)
    {
        var cells = Enumerable.Repeat("[dim]─[/]", table.Columns.Count).ToArray();
        table.AddRow(cells);
    }
}
