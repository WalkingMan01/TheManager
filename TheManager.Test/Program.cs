using TheManager.Models;
using TheManager.Services;

var gameService = new GameService()
{
    Manager = "Steve",
    Team = "BURNLEY"
};

gameService.StartGame();