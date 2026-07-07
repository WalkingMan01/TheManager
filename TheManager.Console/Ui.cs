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

    /// <summary>
    /// Resizes the console window/buffer (Windows only) to fit content that needs
    /// more than the default terminal size.
    /// </summary>
    public static void ResizeConsole(int targetWidth, int targetHeight)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            // Buffer must be at least as large as the window on Windows.
            if (Console.BufferWidth  < targetWidth)  Console.BufferWidth  = targetWidth;
            if (Console.BufferHeight < targetHeight) Console.BufferHeight = targetHeight;
            Console.SetWindowSize(
                Math.Min(targetWidth,  Console.LargestWindowWidth),
                Math.Min(targetHeight, Console.LargestWindowHeight));
        }
        catch { /* Ignore terminals that don't support resize */ }
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

    public static string DivisionName(Division division) => division switch
    {
        Division.One   => "Premier League",
        Division.Two   => "Championship",
        Division.Three => "League One",
        Division.Four  => "League Two",
        _              => "Unknown"
    };

    public static string DivisionShort(Division division) => division switch
    {
        Division.One   => "PL",
        Division.Two   => "Champ",
        Division.Three => "L1",
        Division.Four  => "L2",
        _              => "?"
    };
}
