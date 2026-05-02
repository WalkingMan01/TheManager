using System.Text.Json;
using System.Text.Json.Serialization;
using FootballBoss.Models;

namespace FootballBoss.Services;

/// <summary>
/// Persists and restores the complete game state.
///
/// The original FOOT.BAS wrote a flat comma-separated file (FDII.SAV) using
/// sequential WRITE/INPUT statements. This implementation uses JSON, which is
/// self-describing and survives model changes more gracefully.
///
/// Corresponds to the SAVEGAME and loadold routines in FOOT.BAS
/// (lines 4965–4963 and 4909–4963).
/// </summary>
public static class SaveLoadService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters             = { new JsonStringEnumConverter() }
    };

    // ── Save ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Serialises the entire <see cref="GameState"/> to a JSON file.
    ///
    /// Corresponds to SAVEGAME / savename in FOOT.BAS (lines 4965–5046).
    /// The original decremented OY (save-slots remaining) before writing;
    /// callers should do the same if that mechanic is preserved.
    /// </summary>
    public static void Save(GameState gameState, string filePath)
    {
        string json = JsonSerializer.Serialize(gameState, SerializerOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>Async variant of <see cref="Save"/>.</summary>
    public static async Task SaveAsync(GameState gameState, string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, gameState, SerializerOptions,
            cancellationToken);
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Deserialises a previously saved JSON file back into a <see cref="GameState"/>.
    ///
    /// Corresponds to loadold in FOOT.BAS (lines 4909–4963).
    /// Throws <see cref="InvalidDataException"/> if the file cannot be parsed.
    /// </summary>
    public static GameState Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Save file not found: {filePath}");

        string json = File.ReadAllText(filePath);
        return Deserialize(json);
    }

    /// <summary>Async variant of <see cref="Load"/>.</summary>
    public static async Task<GameState> LoadAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Save file not found: {filePath}");

        await using var stream = File.OpenRead(filePath);
        var state = await JsonSerializer.DeserializeAsync<GameState>(
            stream, SerializerOptions, cancellationToken);

        return state ?? throw new InvalidDataException("Save file contained null game state.");
    }

    // ── Check for existing save ───────────────────────────────────────────────

    /// <summary>
    /// Returns true when a save file exists at the given path.
    /// Corresponds to the "DO YOU WANT TO LOAD AN OLD GAME" prompt
    /// at line 4893 in FOOT.BAS.
    /// </summary>
    public static bool SaveExists(string filePath)
        => File.Exists(filePath);

    // ── Default file path ─────────────────────────────────────────────────────

    /// <summary>
    /// Canonical save file name. The original BASIC used "FDII.SAV";
    /// this version uses JSON extension for clarity.
    /// </summary>
    public const string DefaultSaveFileName = "FDII.json";

    /// <summary>
    /// Returns the full path to the save file in the same directory as
    /// the executing assembly, matching the original behaviour of writing
    /// to the current directory.
    /// </summary>
    public static string DefaultSavePath =>
        Path.Combine(
            AppContext.BaseDirectory,
            DefaultSaveFileName);

    // ── Private helpers ───────────────────────────────────────────────────────

    private static GameState Deserialize(string json)
    {
        var state = JsonSerializer.Deserialize<GameState>(json, SerializerOptions);
        return state ?? throw new InvalidDataException("Save file contained null game state.");
    }
}
