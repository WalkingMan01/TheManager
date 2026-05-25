using Spectre.Console;
using TheManager.Models;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.ConsoleApp.Screens;

internal static class FixturesScreen
{
    public static void Show(GameState state)
    {
        Ui.Header($"FIXTURES  ·  {state.Club.Name.Trim()}");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[dim]Wk[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Opponent[/]"))
            .AddColumn(new TableColumn("[dim]H/A[/]").Centered())
            .AddColumn(new TableColumn("[dim]Type[/]"))
            .AddColumn(new TableColumn("[dim]Result[/]").Centered());

        foreach (var fixture in state.Fixtures)
        {
            bool isCurrent = fixture.Week == state.CurrentWeek;
            bool isPast    = fixture.Week <  state.CurrentWeek;

            string week = isCurrent ? $"[bold yellow]{fixture.Week}[/]" : fixture.Week.ToString();

            string opp = fixture.OpponentName.Trim();
            if (isCurrent)   opp = $"[bold yellow]{opp}[/]";
            else if (isPast) opp = $"[dim]{opp}[/]";

            string venue = fixture.IsHomeGame ? "H" : "A";
            if (isPast) venue = $"[dim]{venue}[/]";

            string result = BuildResultCell(fixture);

            table.AddRow(week, opp, venue, MatchTypeLabel(fixture.MatchType), result);
        }

        AnsiConsole.Write(table);
        Ui.Pause();
    }

    private static string BuildResultCell(ScheduledMatch fixture)
    {
        if (!fixture.WasPlayed)
            return "[dim]—[/]";

        int ours   = fixture.OurScore!.Value;
        int theirs = fixture.TheirScore!.Value;

        string color = ours > theirs ? "green"
                     : ours < theirs ? "red"
                     : "yellow";

        string badge = ours > theirs ? "W" : ours < theirs ? "L" : "D";

        return $"[{color}]{badge} {ours}–{theirs}[/]";
    }

    private static string MatchTypeLabel(MatchType type) => type switch
    {
        MatchType.League    => "League",
        MatchType.LeagueCup => "LC",
        MatchType.FACup     => "FA",
        _                   => type.ToString()
    };
}
