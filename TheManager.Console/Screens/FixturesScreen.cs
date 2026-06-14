using Spectre.Console;
using TheManager.Models;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.ConsoleApp.Screens;

internal static class FixturesScreen
{
    private const int TargetWidth  = 120;
    private const int TargetHeight = 30;

    public static void Show(GameState state)
    {
        Ui.ResizeConsole(TargetWidth, TargetHeight);
        Ui.Header($"FIXTURES  ·  {state.Club.Name.Trim()}");

        int half = (state.Fixtures.Count + 1) / 2;
        var leftTable  = BuildFixtureTable(state, state.Fixtures.Take(half));
        var rightTable = BuildFixtureTable(state, state.Fixtures.Skip(half));

        var wrapper = new Table()
            .NoBorder()
            .HideHeaders()
            .AddColumn(new TableColumn("left").Padding(0, 0, 1, 0))
            .AddColumn(new TableColumn("right").Padding(0, 0, 0, 0));

        wrapper.AddRow(leftTable, rightTable);
        AnsiConsole.Write(wrapper);
        Ui.Pause();
    }

    private static Table BuildFixtureTable(GameState state, IEnumerable<ScheduledMatch> fixtures)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[dim]Wk[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Opponent[/]"))
            .AddColumn(new TableColumn("[dim]H/A[/]").Centered())
            .AddColumn(new TableColumn("[dim]Type[/]"))
            .AddColumn(new TableColumn("[dim]Result[/]").Centered());

        foreach (var fixture in fixtures)
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

        return table;
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
