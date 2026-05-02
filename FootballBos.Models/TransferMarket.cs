namespace FootballBoss.Models;

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

/// <summary>
/// The current state of the transfer market screen.
/// </summary>
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

/// <summary>
/// Active negotiation between the manager and a player / selling club.
/// Populated while navigating the negotiation screens (subroutines 4022–4062,
/// 2601–2654 in FOOT.BAS).
/// </summary>
public class TransferNegotiation
{
    /// <summary>
    /// Squad slot of the player being negotiated for. Corresponds to IB.
    /// </summary>
    public int TargetSquadSlot { get; set; }

    /// <summary>Snapshot of the target player's data.</summary>
    public Player TargetPlayer { get; set; } = new();

    /// <summary>
    /// Negotiation type. Corresponds to HF:
    ///   1 = we are buying
    ///   2 = we are selling
    ///   3 = contract renewal with existing player
    /// </summary>
    public int NegotiationType { get; set; }

    /// <summary>Transfer fee we are offering. Corresponds to HI.</summary>
    public double MoneyOffer { get; set; }

    /// <summary>
    /// Number of weeks we want to loan one of our players as part of the deal.
    /// 0 = no loan offered. Corresponds to hj.
    /// </summary>
    public int LoanWeeksOffered { get; set; }

    /// <summary>
    /// Our player (squad slot) being offered on loan. 0 = none. Corresponds to hk.
    /// </summary>
    public int OurPlayerOnLoanSlot { get; set; }

    /// <summary>
    /// Our player (squad slot) offered as a free transfer sweetener. 0 = none.
    /// Corresponds to hl.
    /// </summary>
    public int FreeTransferPlayerSlot { get; set; }

    // ── Contract terms being offered to the player ────────────────────────────

    /// <summary>Contract length in weeks. Corresponds to HR.</summary>
    public int ContractWeeks { get; set; }

    /// <summary>Offered weekly wage. Corresponds to HS.</summary>
    public double WeeklyWage { get; set; }

    /// <summary>One-off signing-on fee. Corresponds to HT.</summary>
    public double SigningOnFee { get; set; }
}
