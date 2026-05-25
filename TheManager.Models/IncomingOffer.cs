namespace TheManager.Models;

/// <summary>
/// An offer from another club to buy one of our players.
/// Up to 3 simultaneous offers. Corresponds to J$(1–3) / O(2,3).
/// </summary>
public class IncomingOffer
{
    /// <summary>Offer slot 1–3. Corresponds to IB index into J$.</summary>
    public int Slot { get; set; }

    /// <summary>Name of the club making the offer. Corresponds to J$(IB).</summary>
    public string BuyingClub { get; set; } = string.Empty;

    /// <summary>Division of the buying club. Corresponds to O(1,IB).</summary>
    public Division BuyingClubDivision { get; set; }

    /// <summary>
    /// League position of the buying club within their division.
    /// Corresponds to O(2,IB).
    /// </summary>
    public int BuyingClubPosition { get; set; }
}
