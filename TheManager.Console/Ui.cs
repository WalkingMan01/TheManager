using Spectre.Console;
using TheManager.Models;

namespace TheManager.ConsoleApp;

internal static class Ui
{
    public static void Header(string title)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold white] {title} [/]").LeftJustified());
        AnsiConsole.WriteLine();
    }

    public static void Pause(string prompt = "Press any key to continue...")
    {
        AnsiConsole.MarkupLine($"\n[dim]{prompt}[/]");
        System.Console.ReadKey(true);
    }

    public static string FormatMoney(double amount)
        => amount >= 0 ? $"£{amount:N0}" : $"-£{Math.Abs(amount):N0}";

    public static string PositionLabel(int slot) => slot switch
    {
        1                  => "GK",
        2 or 3 or 4 or 5   => "DEF",
        6 or 7 or 8        => "MID",
        9 or 10 or 11      => "ATK",
        12                 => "SUB",
        _                  => "RES"
    };

    public static string PlayerPositionLabel(Player? player) => player?.Position switch
    {
        PlayerPosition.Goalkeeper => "GK",
        PlayerPosition.Defender   => "DEF",
        PlayerPosition.Midfielder => "MID",
        PlayerPosition.Attacker   => "ATK",
        _                         => "—"
    };
}
