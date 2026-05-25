namespace TheManager.Models;

/// <summary>The current state of the transfer market screen.</summary>
public class TransferMarket
{
    /// <summary>
    /// Players available to buy (up to 3 slots).
    /// Corresponds to x(21), x(22), x(23) being non-zero.
    /// </summary>
    public List<TransferListing> PlayersForSale { get; set; } = new(3);

    /// <summary>
    /// Players that other clubs are trying to buy from us (up to 3 slots).
    /// Corresponds to x(24), x(25), x(26) being non-zero.
    /// </summary>
    public List<TransferListing> PlayersBeingSought { get; set; } = new(3);

    /// <summary>
    /// Clubs making direct approaches to sign our players.
    /// Corresponds to J$(1–3) / O(2,3).
    /// </summary>
    public List<IncomingOffer> IncomingOffers { get; set; } = new(3);
}
