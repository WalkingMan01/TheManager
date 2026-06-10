using Spectre.Console;
using TheManager.ConsoleApp;
using TheManager.ConsoleApp.Screens;
using TheManager.Services;

ISaveService saveService = new JsonFileSaveService(JsonFileSaveService.DefaultSavesFolder);

GameService? gameService = null;

while (gameService == null)
{
    var titleChoice = TitleScreen.Show(saveService);

    switch (titleChoice)
    {
        case TitleChoice.Quit:
            return;

        case TitleChoice.Continue:
            var loaded = LoadGameScreen.Show(saveService);
            if (loaded != null)
                gameService = GameService.FromSave(loaded);
            break;

        case TitleChoice.NewGame:
            var (teamName, division, managerName) = TeamSelectionScreen.Show();
            gameService = new GameService
            {
                Team     = teamName,
                Division = division,
                Manager  = managerName
            };
            gameService.StartGame();
            break;
    }
}

bool running = true;
while (running)
{
    var match  = FixtureSchedulerService.GetCurrentMatch(gameService.State.CurrentWeek, gameService.State.Fixtures);
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
            {
                PlayMatchScreen.ShowResult(result, gameService.State);
                if (result.ManagerSacked)
                    SackingScreen.Show(
                        result.SackingReason ?? "You have been sacked.",
                        result.NewClubName   ?? string.Empty,
                        result.NewClubDivision ?? TheManager.Models.Division.Four);
            }
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

        case WeekAction.ManagerProfile:
            ManagerProfileScreen.Show(gameService.State);
            break;

        case WeekAction.Difficulty:
            DifficultyScreen.Show(gameService.State);
            break;

        case WeekAction.SaveGame:
            SaveGameScreen.Show(saveService, gameService.State);
            break;

        case WeekAction.Quit:
            running = false;
            break;
    }
}

AnsiConsole.Clear();
AnsiConsole.MarkupLine("[dim]  Thanks for playing THE MANAGER.[/]");
AnsiConsole.WriteLine();
