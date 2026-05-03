using FootballBoss.Models;
using FootballBoss.Services;

namespace FootballBoss;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("FOOTBALL BOSS");
        Console.WriteLine();

        var rng       = new Random();
        var gameState = SaveLoadService.SaveExists(SaveLoadService.DefaultSavePath)
            ? OfferLoad()
            : null;

        if (gameState is null)
            gameState = SetupNewGame(rng);

        Console.WriteLine();
        Console.WriteLine($"Managing {gameState.Club.Name.Trim()}, Division {(int)gameState.Club.Division}");
        Console.WriteLine($"Squad size: {gameState.Squad.Count(p => p is not null)} players");
    }

    // ── Load existing save ────────────────────────────────────────────────────

    private static GameState? OfferLoad()
    {
        Console.Write("DO YOU WANT TO LOAD AN OLD GAME [Y/N]? ");
        if (!ReadYesNo())
            return null;

        Console.WriteLine("LOADING...");
        return SaveLoadService.Load(SaveLoadService.DefaultSavePath);
    }

    // ── New game setup (mirrors BASIC lines 1011–812) ─────────────────────────

    private static GameState SetupNewGame(Random rng)
    {
        // Manager name — max 8 characters (BASIC line 801–805)
        string managerName;
        do
        {
            Console.Write("MANAGERS NAME (MAX 8): ");
            managerName = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();
        }
        while (managerName.Length < 1 || managerName.Length > 8);

        // Points for a win — 2 or 3 (BASIC line 807–811)
        int pointsPerWin = 0;
        while (pointsPerWin is not 2 and not 3)
        {
            Console.Write("POINTS FOR A WIN 2/3: ");
            int.TryParse(Console.ReadLine(), out pointsPerWin);
        }

        // Division — 1 (top) to 4
        int divChoice = 0;
        while (divChoice is < 1 or > 4)
        {
            Console.Write("CHOOSE DIVISION (1-4): ");
            int.TryParse(Console.ReadLine(), out divChoice);
        }
        var division = (Division)divChoice;

        // TODO: Team name selection requires AllTeamNames to be seeded from data.fd.
        // Until a TeamDataService exists, use a placeholder name.
        string clubName = "BOSS FC  ";

        var gameState = new GameState();

        InitializationService.SetupNewGame(
            gameState,
            clubName,
            division,
            managerName,
            pointsPerWin,
            rng);

        return gameState;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool ReadYesNo()
    {
        while (true)
        {
            string input = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();
            if (input == "Y") return true;
            if (input == "N") return false;
            Console.Write("Please enter Y or N: ");
        }
    }
}
