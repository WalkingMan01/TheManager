namespace TheManager.Models;

/// <summary>
/// Active negotiation between the manager and a player / selling club.
/// Populated while navigating the negotiation screens (subroutines 4022–4062,
/// 2601–2654 in FOOT.BAS).
/// </summary>
public class TransferNegotiation
{
    /// <summary>Squad slot of the player being negotiated for. Corresponds to IB.</summary>
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
