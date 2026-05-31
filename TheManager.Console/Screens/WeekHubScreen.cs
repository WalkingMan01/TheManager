using Spectre.Console;
using TheManager.Models;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.ConsoleApp.Screens;

internal enum WeekAction { CheckMatch, PlayMatch, LeagueTable, Squad, Fixtures, Employees, ScoutReports, TransferMarket, ManagerProfile, Difficulty, Quit }

internal static class WeekHubScreen
{
    public static WeekAction Show(GameState state, ScheduledMatch match)
    {
        Ui.Header($"WEEK {state.CurrentWeek}  ·  {state.Club.Name.Trim()}  ·  DIV {(int)state.Club.Division}");

        if (match.MatchType == MatchType.EndOfSeason)
        {
            AnsiConsole.MarkupLine("  Season complete — process end of season to continue.");
        }
        else
        {
            string venue = match.IsHomeGame ? "[green]HOME[/]" : "AWAY";
            AnsiConsole.MarkupLine($"  Next: [bold]{match.OpponentName.Trim()}[/]  ({venue})   Type: [dim]{MatchTypeLabel(match.MatchType)}[/]");
        }

        AnsiConsole.MarkupLine($"  Balance: [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]   Morale: [yellow]{state.Club.TeamMorale}[/]");
        AnsiConsole.WriteLine();

        bool isEndOfSeason = match.MatchType == MatchType.EndOfSeason;

        var choices = new List<string>();
        if (!isEndOfSeason) choices.Add("Check Match");
        choices.Add(isEndOfSeason ? "Process End of Season" : "Play Match");
        choices.Add("League Table");
        choices.Add("Squad");
        choices.Add("Fixtures");
        choices.Add("Employees");
        choices.Add("Scout Reports");
        choices.Add("Transfer Market");
        choices.Add("Manager Profile");
        choices.Add("Difficulty");
        choices.Add("Quit");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]What would you like to do?[/]")
                .AddChoices(choices));

        return choice switch
        {
            "Check Match"            => WeekAction.CheckMatch,
            "Play Match"             => WeekAction.PlayMatch,
            "Process End of Season"  => WeekAction.PlayMatch,
            "League Table"           => WeekAction.LeagueTable,
            "Squad"                  => WeekAction.Squad,
            "Fixtures"               => WeekAction.Fixtures,
            "Employees"              => WeekAction.Employees,
            "Scout Reports"          => WeekAction.ScoutReports,
            "Transfer Market"        => WeekAction.TransferMarket,
            "Manager Profile"        => WeekAction.ManagerProfile,
            "Difficulty"             => WeekAction.Difficulty,
            _                        => WeekAction.Quit
        };
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
