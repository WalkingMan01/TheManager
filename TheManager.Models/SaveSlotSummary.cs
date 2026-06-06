namespace TheManager.Models;

/// <summary>Lightweight save-slot metadata stored in the header of each save file.</summary>
public record SaveSlotSummary(
    string   SlotName,
    string   ClubName,
    int      Division,
    string   Manager,
    int      Season,
    int      Week,
    DateTime SavedAt);
