namespace TheManager.Models;

/// <summary>
/// A player available on the transfer market.
/// Slots 21–23 in the BASIC squad array are clubs selling players we can buy.
/// Slots 24–26 are players other clubs have enquired about buying from us.
/// </summary>
public class TransferListing
{
    /// <summary>BASIC squad array index (21–26). Corresponds to I in x(I).</summary>
    public int SquadSlot { get; set; }

    /// <summary>The player's data.</summary>
    public Player Player { get; set; } = new();

    /// <summary>
    /// Index into the all-teams array (Y$) of the club that owns the player.
    /// Corresponds to x(I).
    /// </summary>
    public int OwningClubIndex { get; set; }

    /// <summary>Resolved club name for display.</summary>
    public string OwningClubName { get; set; } = string.Empty;
}
