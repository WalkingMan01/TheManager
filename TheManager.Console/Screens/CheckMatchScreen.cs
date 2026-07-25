using Spectre.Console;
using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.ConsoleApp.Screens;

internal static class CheckMatchScreen
{
    public static void Show(GameState state, Random rng)
    {
        var match = FixtureSchedulerService.GetCurrentMatch(state.CurrentWeek, state.Fixtures);

        Ui.Header($"CHECK MATCH  ·  Matchday {match.Week}  ·  {MatchTypeLabel(match.MatchType)}");

        if (match.MatchType == MatchType.NoFixture || string.IsNullOrWhiteSpace(match.OpponentName))
        {
            AnsiConsole.MarkupLine("  [dim]No fixture to check this matchday.[/]");
            if (match.MatchType == MatchType.FACup)
            {
                AnsiConsole.WriteLine();
                ShowOtherRoundFixtures(state);
            }
            Ui.Pause();
            return;
        }

        string ourName  = state.Club.Name.Trim();
        string oppName  = match.OpponentName.Trim();
        string homeTeam = match.IsHomeGame ? ourName : oppName;
        string awayTeam = match.IsHomeGame ? oppName : ourName;

        AnsiConsole.MarkupLine($"  [bold green]{homeTeam}[/]   vs   [bold]{awayTeam}[/]");
        AnsiConsole.WriteLine();

        var ourRatings = PlayerService.CalculateTeamRatings(state.Squad);
        bool isCup     = match.MatchType is MatchType.LeagueCup or MatchType.FACup or MatchType.Playoff;

        if (match.OpponentRatings == null)
        {
            match.OpponentRatings = OpponentRatingService.Estimate(
                match.OpponentName,
                state.CurrentLeague,
                state.Club.Division,
                state.DifficultyLevel,
                opponentDivision: isCup
                    ? CupService.GetDivisionForTeamIndex(match.OpponentTeamIndex)
                    : 0,
                isCupMatch: isCup,
                rng);
        }

        bool weAreHome = match.IsHomeGame;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[dim]Category[/]"))
            .AddColumn(new TableColumn($"[bold green]{homeTeam}[/]").Centered())
            .AddColumn(new TableColumn($"[bold]{awayTeam}[/]").Centered());

        AddRatingRow(table, "Goalkeeper",
            weAreHome ? ourRatings.GoalkeeperRating : match.OpponentRatings.GoalkeeperRating,
            weAreHome ? match.OpponentRatings.GoalkeeperRating : ourRatings.GoalkeeperRating);
        AddRatingRow(table, "Defence",
            weAreHome ? ourRatings.DefenceRating    : match.OpponentRatings.DefenceRating,
            weAreHome ? match.OpponentRatings.DefenceRating    : ourRatings.DefenceRating);
        AddRatingRow(table, "Midfield",
            weAreHome ? ourRatings.MidRating        : match.OpponentRatings.MidRating,
            weAreHome ? match.OpponentRatings.MidRating        : ourRatings.MidRating);
        AddRatingRow(table, "Attack",
            weAreHome ? ourRatings.AttackRating     : match.OpponentRatings.AttackRating,
            weAreHome ? match.OpponentRatings.AttackRating     : ourRatings.AttackRating);

        AnsiConsole.Write(table);

        if (match.MatchType == MatchType.FACup)
        {
            AnsiConsole.WriteLine();
            ShowOtherRoundFixtures(state);
        }

        Ui.Pause();
    }

    /// <summary>Lists the other ties drawn in this FA Cup round, alongside our own.</summary>
    private static void ShowOtherRoundFixtures(GameState state)
    {
        string ourName = state.Club.Name.Trim();
        var otherTies = state.FACup.CurrentRoundFixtures
            .Where(t => !t.HomeTeam.Trim().Equals(ourName, StringComparison.OrdinalIgnoreCase)
                     && !t.AwayTeam.Trim().Equals(ourName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (otherTies.Count == 0) return;

        AnsiConsole.MarkupLine("  [bold dim]OTHER FIXTURES THIS ROUND[/]");
        AnsiConsole.WriteLine();

        var fixturesTable = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("").RightAligned())
            .AddColumn(new TableColumn("").Centered().Width(3))
            .AddColumn(new TableColumn(""));

        foreach (var tie in otherTies)
            fixturesTable.AddRow(
                $"[dim]{Markup.Escape(tie.HomeTeam.Trim())}[/]",
                "[dim]v[/]",
                $"[dim]{Markup.Escape(tie.AwayTeam.Trim())}[/]");

        AnsiConsole.Write(fixturesTable);
    }

    private static void AddRatingRow(Table table, string label, int left, int right)
    {
        string lStr = left  > right ? $"[bold green]{left}[/]"  : $"[red]{left}[/]";
        string rStr = right > left  ? $"[bold green]{right}[/]" : $"[red]{right}[/]";
        if (left == right) { lStr = left.ToString(); rStr = right.ToString(); }
        table.AddRow(label, lStr, rStr);
    }

    private static string MatchTypeLabel(MatchType type) => type switch
    {
        MatchType.League            => "League",
        MatchType.LeagueCup         => "League Cup",
        MatchType.FACup             => "FA Cup",
        MatchType.Playoff           => "Play-off",
        MatchType.EuropeanFirstLeg  => "Euro (1st leg)",
        MatchType.EuropeanSecondLeg => "Euro (2nd leg)",
        MatchType.EuropeanFriendly  => "Euro Friendly",
        MatchType.Replay            => "Replay",
        _                           => type.ToString()
    };
}
