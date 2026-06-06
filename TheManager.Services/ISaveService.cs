using TheManager.Models;

namespace TheManager.Services;

/// <summary>Persistence contract for named save slots.</summary>
public interface ISaveService
{
    /// <summary>
    /// Writes (or overwrites) a save slot. Creates the slot if it does not
    /// exist; replaces it silently if it does.
    /// </summary>
    void Save(string slotName, GameState state);

    /// <summary>
    /// Returns all save-slot summaries ordered by SavedAt descending.
    /// Reads only the header section of each file — does not deserialise
    /// the full game state.
    /// </summary>
    IReadOnlyList<SaveSlotSummary> ListSlots();

    /// <summary>
    /// Loads and deserialises a save slot by name.
    /// Throws <see cref="KeyNotFoundException"/> if the slot does not exist.
    /// </summary>
    GameState Load(string slotName);

    /// <summary>Deletes a save slot. No-op if the slot does not exist.</summary>
    void Delete(string slotName);

    /// <summary>True when at least one save slot exists.</summary>
    bool AnySaveExists();
}
