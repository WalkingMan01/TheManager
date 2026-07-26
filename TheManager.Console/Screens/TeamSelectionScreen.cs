using Spectre.Console;
using TheManager.Models;

namespace TheManager.ConsoleApp.Screens;

internal static class TeamSelectionScreen
{
    private static readonly (string Label, Division Value)[] Divisions =
    [
        ("Premier League", Division.One),
        ("Championship",   Division.Two),
        ("League One",     Division.Three),
        ("League Two",     Division.Four),
    ];

    public static (string TeamName, Division Division, string ManagerName) Show()
    {
        Ui.Header("NEW GAME");

        var divLabel = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select your division:[/]")
                .AddChoices(Divisions.Select(d => d.Label)));

        var division = Divisions.First(d => d.Label == divLabel).Value;
        var teams    = TeamData.GetDivisionTeams(division);

        AnsiConsole.WriteLine();
        var teamName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select your club:[/]")
                .PageSize(teams.Count)
                .AddChoices(teams));

        AnsiConsole.WriteLine();
        var managerName = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold]Manager name:[/] ")
                .DefaultValue("Manager")
                .Validate(s => s.Trim().Length > 0
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Name cannot be empty")));

        return (teamName, division, managerName.Trim());
    }
}
