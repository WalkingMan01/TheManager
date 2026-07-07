using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Handles the flow when a rival club tries to poach the player's manager
/// to run their team.
///
/// Corresponds to subroutines 5501–5532 in FOOT.BAS (lines 4365–4431).
///
/// The offering club generates a wage + contract package. The manager can
/// re-negotiate (pushing the wage up slightly until the board loses patience)
/// or accept (triggering <see cref="InitializationService.JoinNewClub"/>).
/// Accepting or declining moves the offering entry out of J$()/O() slots.
/// </summary>
public static class IncomingOfferService
{
    // ── Generate an offer ─────────────────────────────────────────────────────

    /// <summary>
    /// Generates the initial contract offer from a rival club using the same
    /// formula as the manager's own board (subroutine 1401).
    ///
    /// BASIC lines 5501–5503:
    ///   offeredWage   = INT(200 − 7.5 × offeringClubLeaguePos) × (5 − offeringDivision)
    ///   contractWeeks = INT((20 / currentLeaguePos) × 10) + (1 + RND(0–19))
    /// </summary>
    public static ClubManagerOffer GenerateOffer(
        IncomingOffer offeringClub,
        int           currentLeaguePosition,
        Random        rng)
    {
        int offeringDivision = (int)offeringClub.BuyingClubDivision;
        int offeringPosition = offeringClub.BuyingClubPosition;

        double offeredWage      = (int)((200 - 7.5 * offeringPosition) * (5 - offeringDivision));
        double signingBonusPart = (offeredWage / 100.0) * 20;
        offeredWage            -= signingBonusPart;

        int contractWeeks       = (int)((20.0 / Math.Max(1, currentLeaguePosition)) * 10)
                                + 1 + rng.Next(20);

        int sackableAfterWeeks  = Math.Max(0,
            (int)(14 - offeringPosition) - rng.Next(7));

        return new ClubManagerOffer
        {
            OfferingClub       = offeringClub.BuyingClub,
            OfferingDivision   = offeringClub.BuyingClubDivision,
            WeeklyWage         = Math.Max(0, (int)offeredWage),
            ContractWeeks      = contractWeeks,
            SackableAfterWeeks = sackableAfterWeeks,
            OriginalWage       = Math.Max(0, (int)offeredWage)
        };
    }

    // ── Re-negotiate ──────────────────────────────────────────────────────────

    /// <summary>
    /// Called when the manager asks for better terms.
    ///
    /// BASIC lines 5516–5518:
    ///   If offeredWage &lt; originalWage → increase by 10.
    ///   RA = 1 + RND(0–4); if RA=1 → board gives up, player goes elsewhere.
    ///   Otherwise regenerate with fresh random terms.
    /// </summary>
    public static RenegotiateOutcome Renegotiate(
        ref ClubManagerOffer offer,
        int                  currentLeaguePosition,
        Random               rng)
    {
        if (offer.WeeklyWage < offer.OriginalWage) offer.WeeklyWage += 10;

        int patienceRoll = 1 + rng.Next(5);
        if (patienceRoll == 1)
            return RenegotiateOutcome.BoardLosesPatience;

        // Regenerate terms  (line 5502 loop)
        int offeringPosition = offer.OfferingPosition;
        int offeringDivision = (int)offer.OfferingDivision;

        double newWage    = (int)((200 - 7.5 * offeringPosition) * (5 - offeringDivision));
        double bonusPart  = (newWage / 100.0) * 20;
        newWage          -= bonusPart;

        if (newWage < offer.OriginalWage) newWage = offer.OriginalWage + 10;

        offer.WeeklyWage    = Math.Max(0, (int)newWage);
        offer.ContractWeeks = (int)((20.0 / Math.Max(1, currentLeaguePosition)) * 10)
                            + 1 + rng.Next(20);

        return RenegotiateOutcome.NewOffer;
    }

    // ── Accept offer ──────────────────────────────────────────────────────────

    /// <summary>
    /// The manager accepts the offer. Updates game state to reflect the move.
    ///
    /// BASIC subroutine 5532 (lines 4432–4443) → 5555 → 5557:
    ///   Reset European state, cup ties, shares, loans.
    ///   Swap club names (Z$ ↔ J$(IB)).
    ///   Set new division and league position.
    ///   Regenerate squad and staff.
    /// </summary>
    public static void Accept(
        GameState        gameState,
        ClubManagerOffer offer,
        Random           rng)
    {
        // Manager's new contract terms
        gameState.Club.ManagerWeeklyWage    = offer.WeeklyWage;
        gameState.Club.ManagerContractWeeks = offer.ContractWeeks;

        // Clear all transfer-market and cup state
        gameState.LeagueCup.CurrentRound = CupRound.NotEntered;
        gameState.FACup.CurrentRound     = CupRound.NotEntered;
        gameState.European               = null;

        // Signature check happens in UI layer; join the new club
        InitializationService.JoinNewClub(
            gameState,
            newClubName:            offer.OfferingClub,
            newDivision:            offer.OfferingDivision,
            startingLeaguePosition: offer.OfferingPosition,
            rng:                    rng);
    }

    // ── Decline / board loses patience ────────────────────────────────────────

    /// <summary>
    /// The manager declines or the offering board gives up.
    /// Removes the offering entry and sends the job to another candidate.
    ///
    /// BASIC lines 5506, 5521–5523: clears J$(IB)/O(1,IB)/O(2,IB), then
    /// the job is re-advertised to a different team in a nearby division.
    /// </summary>
    public static void Decline(
        GameState        gameState,
        ClubManagerOffer offer,
        int              offerSlot,
        Random           rng)
    {
        // Clear the offer slot from transfer market
        if (offerSlot >= 0 && offerSlot < gameState.TransferMarket.IncomingOffers.Count)
            gameState.TransferMarket.IncomingOffers.RemoveAt(offerSlot);

        // The job goes to a different candidate — re-advertise in a nearby division
        // (BASIC lines 5519–5523: find a new club in a nearby division)
        int newDivisionNumber = Math.Max(1, Math.Min(4, (int)offer.OfferingDivision + rng.Next(3) - 1));
        var newOfferDiv       = (Division)newDivisionNumber;
        int newPosition       = 1 + rng.Next(Constants.TeamCount(newOfferDiv));

        var (offerDivStart, _) = Constants.DivisionRange(newOfferDiv);
        int midTeamIndex       = offerDivStart + Constants.TeamCount(newOfferDiv) / 2 - 1;

        var newOffer = new IncomingOffer
        {
            Slot               = offerSlot,
            BuyingClub        = gameState.AllTeamNames[midTeamIndex],
            BuyingClubDivision = (Division)newDivisionNumber,
            BuyingClubPosition = newPosition
        };

        if (gameState.TransferMarket.IncomingOffers.Count < 3)
            gameState.TransferMarket.IncomingOffers.Add(newOffer);
    }
}

// ── Data classes ─────────────────────────────────────────────────────────────

/// <summary>A contract package offered to the manager by a rival club.</summary>
public class ClubManagerOffer
{
    public string   OfferingClub       { get; set; } = string.Empty;
    public Division OfferingDivision   { get; set; }
    public int      OfferingPosition   { get; set; }
    public double   WeeklyWage         { get; set; }
    public int      ContractWeeks      { get; set; }
    public int      SackableAfterWeeks { get; set; }

    /// <summary>The initial wage when the offer was first generated (used in re-negotiation).</summary>
    public double OriginalWage         { get; set; }
}
