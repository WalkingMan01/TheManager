using System.Text.Json;
using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Saves and loads game state as GUID-named JSON files inside a saves folder.
/// Each file contains a header (slot metadata) and the full serialised GameState.
/// </summary>
public class JsonFileSaveService : ISaveService
{
    /// <summary>Path to the saves folder used by this instance.</summary>
    public string SavesFolder { get; }

    /// <summary>
    /// Creates the service. The saves folder is created on first save if it does
    /// not already exist. Pass <see cref="DefaultSavesFolder"/> for production use.
    /// </summary>
    public JsonFileSaveService(string savesFolder)
    {
        SavesFolder = savesFolder;
    }

    /// <summary>Default saves folder — next to the executing assembly.</summary>
    public static string DefaultSavesFolder =>
        Path.Combine(AppContext.BaseDirectory, "saves");

    // ── ISaveService ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Save(string slotName, GameState state)
    {
        Directory.CreateDirectory(SavesFolder);
        var filePath = FindExistingSlotPath(slotName)
                       ?? Path.Combine(SavesFolder, $"{Guid.NewGuid()}.json");

        var header = new SaveSlotSummary(
            SlotName: slotName,
            ClubName: state.Club.Name.Trim(),
            Division: (int)state.Club.Division,
            Manager:  state.Club.ManagerName,
            Season:   state.SeasonsPlayed + 1,
            Week:     state.CurrentWeek,
            SavedAt:  DateTime.UtcNow);

        var stateElement = JsonSerializer.SerializeToElement(state, SaveLoadService.SerializerOptions);
        var saveFile     = new SaveFile(header, stateElement);
        File.WriteAllText(filePath, JsonSerializer.Serialize(saveFile, SaveLoadService.SerializerOptions));
    }

    /// <inheritdoc/>
    public IReadOnlyList<SaveSlotSummary> ListSlots()
    {
        if (!Directory.Exists(SavesFolder))
            return [];

        var results = new List<SaveSlotSummary>();
        foreach (var path in Directory.GetFiles(SavesFolder, "*.json"))
        {
            try
            {
                using var doc    = JsonDocument.Parse(File.ReadAllText(path));
                var       header = JsonSerializer.Deserialize<SaveSlotSummary>(
                    doc.RootElement.GetProperty("Header"),
                    SaveLoadService.SerializerOptions);
                if (header != null)
                    results.Add(header);
            }
            catch { /* skip corrupt or unrecognised files */ }
        }

        return results.OrderByDescending(s => s.SavedAt).ToList();
    }

    /// <inheritdoc/>
    public GameState Load(string slotName)
    {
        var path = FindExistingSlotPath(slotName)
                   ?? throw new KeyNotFoundException($"Save slot '{slotName}' not found.");

        using var doc     = JsonDocument.Parse(File.ReadAllText(path));
        var       wrapper = JsonSerializer.Deserialize<SaveFile>(doc.RootElement, SaveLoadService.SerializerOptions)
                            ?? throw new InvalidDataException("Save file contained null.");
        return SaveLoadService.Deserialize(wrapper.GameState.GetRawText());
    }

    /// <inheritdoc/>
    public void Delete(string slotName)
    {
        var path = FindExistingSlotPath(slotName);
        if (path != null)
            File.Delete(path);
    }

    /// <inheritdoc/>
    public bool AnySaveExists() =>
        Directory.Exists(SavesFolder) &&
        Directory.EnumerateFiles(SavesFolder, "*.json").Any();

    // ── Private helpers ───────────────────────────────────────────────────────

    private string? FindExistingSlotPath(string slotName)
    {
        if (!Directory.Exists(SavesFolder))
            return null;

        foreach (var path in Directory.GetFiles(SavesFolder, "*.json"))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var name = doc.RootElement
                    .GetProperty("Header")
                    .GetProperty("SlotName")
                    .GetString();
                if (string.Equals(name, slotName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            catch { /* skip corrupt files */ }
        }

        return null;
    }

    private record SaveFile(SaveSlotSummary Header, JsonElement GameState);
}
