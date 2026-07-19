using Spectre.Console;
using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.ConsoleApp.Screens;

internal enum WeekAction { CheckMatch, PlayMatch, LeagueTable, Squad, Fixtures, Cup, Employees, ScoutReports, TransferMarket, ManagerProfile, Difficulty, SaveGame, Quit }

internal static class WeekHubScreen
{
    public static WeekAction Show(GameState state, ScheduledMatch match)
    {
        Ui.Header($"MATCHDAY {state.CurrentWeek}  ·  {state.Club.Name.Trim()}  ·  {Ui.DivisionName(state.Club.Division)}");

        bool isEndOfSeason = match.MatchType == MatchType.EndOfSeason;
        bool isRestDay     = match.MatchType == MatchType.NoFixture;
        bool isCupDay      = match.MatchType == MatchType.FACup;
        bool cupDrawKnown  = !string.IsNullOrWhiteSpace(match.OpponentName);

        if (isEndOfSeason)
        {
            AnsiConsole.MarkupLine("  Season complete — process end of season to continue.");
        }
        else if (isRestDay)
        {
            AnsiConsole.MarkupLine("  [dim]No fixture today — rest day.[/]");
        }
        else if (isCupDay)
        {
            var round = CupRoundForMatchday(match.Week);
            string roundName = CupService.RoundDisplayName(round).ToUpperInvariant();
            string wembley   = CupService.IsNeutralVenue(round) ? " — [bold]WEMBLEY[/]" : "";

            if (cupDrawKnown)
            {
                string venue = CupService.IsNeutralVenue(round) ? "[bold]WEMBLEY[/]"
                             : match.IsHomeGame ? "[green]HOME[/]" : "AWAY";
                AnsiConsole.MarkupLine($"  FA CUP {roundName}:  [bold]{match.OpponentName.Trim()}[/]  ({venue})");
            }
            else
                AnsiConsole.MarkupLine($"  FA CUP {roundName}{wembley}  [dim](no tie this round — rest day)[/]");
        }
        else
        {
            string venue = match.IsHomeGame ? "[green]HOME[/]" : "AWAY";
            AnsiConsole.MarkupLine($"  Next: [bold]{match.OpponentName.Trim()}[/]  ({venue})   Type: [dim]{MatchTypeLabel(match.MatchType)}[/]");
        }

        AnsiConsole.MarkupLine($"  Balance: [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]   Morale: [yellow]{state.Club.TeamMorale}[/]");
        AnsiConsole.WriteLine();

        DrawNewsSection(state);

        var choices = new List<string>();
        if (!isEndOfSeason && !isRestDay) choices.Add("Check Match");
        choices.Add(isEndOfSeason ? "Process End of Season"
                  : isRestDay     ? "Continue (rest day)"
                  : "Play Match");
        choices.Add("League Table");
        choices.Add("Squad");
        choices.Add("Fixtures");
        choices.Add("FA Cup");
        choices.Add("Employees");
        choices.Add("Scout Reports");
        choices.Add("Transfer Market");
        choices.Add("Manager Profile");
        choices.Add("Difficulty");
        choices.Add("Save Game");
        choices.Add("Quit");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]What would you like to do?[/]")
                .PageSize(choices.Count)
                .AddChoices(choices));

        return choice switch
        {
            "Check Match"            => WeekAction.CheckMatch,
            "Play Match"             => WeekAction.PlayMatch,
            "Continue (rest day)"    => WeekAction.PlayMatch,
            "Process End of Season"  => WeekAction.PlayMatch,
            "League Table"           => WeekAction.LeagueTable,
            "Squad"                  => WeekAction.Squad,
            "Fixtures"               => WeekAction.Fixtures,
            "FA Cup"                 => WeekAction.Cup,
            "Employees"              => WeekAction.Employees,
            "Scout Reports"          => WeekAction.ScoutReports,
            "Transfer Market"        => WeekAction.TransferMarket,
            "Manager Profile"        => WeekAction.ManagerProfile,
            "Difficulty"             => WeekAction.Difficulty,
            "Save Game"              => WeekAction.SaveGame,
            _                        => WeekAction.Quit
        };
    }

    /// <summary>Round played on a given cup matchday (falls back to Round1).</summary>
    internal static CupRound CupRoundForMatchday(int matchday)
    {
        int idx = Array.IndexOf(Constants.FACupMatchdays, matchday);
        return CupService.RoundForIndex(Math.Max(0, idx));
    }

    private static void DrawNewsSection(GameState state)
    {
        var items = new List<string>();

        for (int i = 1; i <= 20; i++)
        {
            var p = state.Squad[i];
            if (p is null || p.IsRetiring) continue;
            if (p.ContractWeeks > 10) continue;

            string weeks = p.ContractWeeks == 0
                ? "[red bold]expired[/]"
                : $"[red]{p.ContractWeeks}[/] week{(p.ContractWeeks == 1 ? "" : "s")} remaining";
            items.Add($"[bold]{Markup.Escape(p.Name.Trim())}[/] — contract {weeks}");
        }

        if (items.Count == 0)
            return;

        AnsiConsole.MarkupLine("  [bold yellow]NEWS[/]");
        foreach (var item in items)
            AnsiConsole.MarkupLine($"  · {item}");
        AnsiConsole.WriteLine();
    }

    private static string MatchTypeLabel(MatchType type) => type switch
    {
        MatchType.League            => "League",
        MatchType.LeagueCup         => "League Cup",
        MatchType.FACup             => "FA Cup",
        MatchType.EuropeanFirstLeg  => "Euro (1st leg)",
        MatchType.EuropeanSecondLeg => "Euro (2nd leg)",
        MatchType.EuropeanFriendly  => "Euro Friendly",
        MatchType.Replay            => "Replay",
        _                           => type.ToString()
    };
}
