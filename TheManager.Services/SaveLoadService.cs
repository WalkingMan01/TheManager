using System.Text.Json;
using System.Text.Json.Serialization;
using TheManager.Models;

namespace TheManager.Services;

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
    internal static readonly JsonSerializerOptions SerializerOptions = new()
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
    public static GameState Load(string filePath, Random? rng = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Save file not found: {filePath}");

        string json = File.ReadAllText(filePath);
        return Deserialize(json, rng);
    }

    /// <summary>Async variant of <see cref="Load"/>.</summary>
    public static async Task<GameState> LoadAsync(string filePath,
        CancellationToken cancellationToken = default, Random? rng = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Save file not found: {filePath}");

        await using var stream = File.OpenRead(filePath);
        var state = await JsonSerializer.DeserializeAsync<GameState>(
            stream, SerializerOptions, cancellationToken);

        if (state == null)
            throw new InvalidDataException("Save file contained null game state.");

        var random = rng ?? new Random();
        MigrateLegacyPotentials(state, random);
        MigrateLegacyCupState(state);
        MigrateLegacyGround(state);
        return state;
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

    internal static GameState Deserialize(string json, Random? rng = null)
    {
        var state = JsonSerializer.Deserialize<GameState>(json, SerializerOptions);
        if (state == null)
            throw new InvalidDataException("Save file contained null game state.");

        MigrateLegacyPotentials(state, rng ?? new Random());
        MigrateLegacyCupState(state);
        MigrateLegacyGround(state);
        return state;
    }

    /// <summary>
    /// One-time migration for saves created before the hidden-potential
    /// mechanic: players with PeakAge 0 (never assigned) get a PeakAge,
    /// PotentialSkill, and DevelopmentRate rolled via
    /// <see cref="PlayerService.AssignPotential"/>. AssignPotential always sets
    /// the ceiling above current skill, so no loaded player loses ability.
    /// Players saved after the potential mechanic shipped but before
    /// DevelopmentRate existed have a valid PeakAge already, so that branch
    /// won't fire for them — backfill just the rate via
    /// <see cref="PlayerService.AssignDevelopmentRate"/> instead.
    /// </summary>
    private static void MigrateLegacyPotentials(GameState state, Random rng)
    {
        foreach (var player in state.Squad)
        {
            if (player is { PeakAge: 0 })
                PlayerService.AssignPotential(player, rng);
            else if (player is { DevelopmentRate: <= 0 })
                PlayerService.AssignDevelopmentRate(player, rng);
        }
    }

    /// <summary>
    /// One-time migration for saves created before the FA Cup port:
    ///   - The team-name pool grew from 120 to 128 slots (32 non-league teams,
    ///     indices 93–124) — expand the array and fill the new names.
    ///   - A save whose fixture list has no cup matchdays sits the FA Cup out
    ///     for the season in progress and joins from the next season
    ///     (docs/specs/fa-cup.md, Step 6).
    /// </summary>
    private static void MigrateLegacyCupState(GameState state)
    {
        if (state.AllTeamNames.Length < 128)
        {
            var expanded = new string[128];
            Array.Copy(state.AllTeamNames, expanded, state.AllTeamNames.Length);
            state.AllTeamNames = expanded;
        }

        for (int i = 0; i < state.AllTeamNames.Length; i++)
            state.AllTeamNames[i] ??= string.Empty;

        TeamData.FillMissing(state.AllTeamNames);

        bool hasCupCalendar = state.Fixtures.Any(f => f.MatchType == Models.MatchType.FACup);
        if (!hasCupCalendar && state.Fixtures.Count > 0)
            state.FACup.CurrentRound = CupRound.NotEntered;
    }

    /// <summary>
    /// One-time migration for saves created before the ground-capacity model
    /// (docs/specs/gate-receipts-ground-capacity.md, Step 4): seed the ground
    /// name and capacity from the real-ground table, or the division fallback
    /// with no jitter — deterministic, no <see cref="Random"/> in the load path.
    /// </summary>
    private static void MigrateLegacyGround(GameState state)
    {
        var club = state.Club;
        if (club.GroundCapacity > 0 && !string.IsNullOrWhiteSpace(club.GroundName))
            return;

        if (TeamData.TryGetGround(club.Name, out string groundName, out int capacity))
        {
            club.GroundName     = groundName;
            club.GroundCapacity = capacity;
        }
        else
        {
            club.GroundName     = $"{club.Name.Trim()} Stadium";
            club.GroundCapacity = Constants.FallbackGroundCapacity(club.Division);
        }
    }
}
