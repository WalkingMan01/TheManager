using TheManager.Models;
using TheManager.Services;

var gameService = new GameService()
{
    Manager = "Steve",
    Team = "Sunderland"
};

gameService.StartGame();