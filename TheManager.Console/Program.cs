using Spectre.Console;
using TheManager.ConsoleApp.Screens;
using TheManager.Services;

TitleScreen.Show();

var (teamName, division, managerName) = TeamSelectionScreen.Show();

var gameService = new GameService
{
    Team     = teamName,
    Division = division,
    Manager  = managerName
};
gameService.StartGame();

bool running = true;
while (running)
{
    var match  = FixtureSchedulerService.GetCurrentMatch(gameService.State);
    var action = WeekHubScreen.Show(gameService.State, match);

    switch (action)
    {
        case WeekAction.CheckMatch:
            CheckMatchScreen.Show(gameService.State, gameService.Random);
            break;

        case WeekAction.PlayMatch:
            var result = gameService.PlayMatch();
            if (result.WasEndOfSeason)
                running = EndOfSeasonScreen.Show(gameService.State);
            else
                PlayMatchScreen.ShowResult(result, gameService.State);
            break;

        case WeekAction.LeagueTable:
            LeagueTableScreen.Show(gameService.State);
            break;

        case WeekAction.Squad:
            SquadScreen.Show(gameService.State);
            break;

        case WeekAction.Fixtures:
            FixturesScreen.Show(gameService.State);
            break;

        case WeekAction.Employees:
            EmployeesScreen.Show(gameService.State, gameService.Random);
            break;

        case WeekAction.ScoutReports:
            ScoutReportsScreen.Show(gameService.State, gameService.Random);
            break;

        case WeekAction.TransferMarket:
            TransferMarketScreen.Show(gameService.State, gameService.Random);
            break;

        case WeekAction.Difficulty:
            DifficultyScreen.Show(gameService.State);
            break;

        case WeekAction.Quit:
            running = false;
            break;
    }
}

AnsiConsole.Clear();
AnsiConsole.MarkupLine("[dim]  Thanks for playing THE MANAGER.[/]");
AnsiConsole.WriteLine();
