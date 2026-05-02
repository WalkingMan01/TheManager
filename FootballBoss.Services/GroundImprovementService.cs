using FootballBoss.Models;

namespace FootballBoss.Services;

/// <summary>
/// Manages the one-time stadium capacity upgrade.
///
/// Corresponds to subroutines 4201–4206 in FOOT.BAS (lines 3623–3642).
///
/// The builders charge a fixed fee of INT(1,500,000 / Division) to expand
/// the ground to maximum capacity. Once purchased NI = 0 and the option
/// disappears from the menu.
/// </summary>
public static class GroundImprovementService
{
    // ── Calculate cost ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the improvement cost for a club in the given division.
    ///
    /// BASIC line 4624:
    ///   cost = INT(1,500,000 / division)
    ///
    /// The cost is also stored in <see cref="Club.GroundImprovementCost"/> at
    /// initialisation time; this method recalculates it on demand.
    /// </summary>
    public static double CalculateCost(Division division)
        => (int)(1_500_000.0 / (int)division);

    // ── Purchase the improvement (lines 4206) ────────────────────────────────

    /// <summary>
    /// Attempts to purchase the stadium upgrade.
    /// Returns false when the club cannot afford it or is already at capacity.
    ///
    /// BASIC lines 4204–4206:
    ///   IF IN$="N" THEN NI=0 (user cancels — clears cost but doesn't improve)
    ///   IF AI &lt; NI THEN loop (re-show if not enough money)
    ///   AI = INT(AI - NI); NI = 0 (buy and clear the option)
    /// </summary>
    public static GroundImprovementResult PurchaseImprovement(
        Club     club,
        Finances finances)
    {
        if (club.GroundIsMaxCapacity)
            return new GroundImprovementResult(false,
                "Ground is already at maximum capacity.");

        double cost = club.GroundImprovementCost;

        if (finances.BankBalance < cost)
            return new GroundImprovementResult(false,
                $"Cannot afford £{(int)cost:N0} — current balance is £{(int)finances.BankBalance:N0}.");

        finances.BankBalance         -= (int)cost;
        club.GroundImprovementCost    = 0;   // NI = 0: already at max / option gone

        return new GroundImprovementResult(true,
            $"Ground upgraded for £{(int)cost:N0}. Capacity is now at maximum.");
    }

    // ── Initialise cost for a new club ────────────────────────────────────────

    /// <summary>
    /// Sets the ground improvement cost on a club according to its division.
    /// Called during <see cref="InitializationService.JoinNewClub"/> and new-game setup.
    ///
    /// Lower-division clubs (3+) start with no improvement available (NI=0)
    /// because their ground is effectively "good enough" at that level.
    /// BASIC line 5546: NI=NI-ABS(O(1,IB)>2)*NI.
    /// </summary>
    public static void InitialiseForDivision(Club club, Division division)
    {
        club.GroundImprovementCost = (int)division > 2
            ? 0
            : CalculateCost(division);
    }
}

// ── Data class ────────────────────────────────────────────────────────────────

public class GroundImprovementResult
{
    public bool   Success     { get; }
    public string Description { get; }

    public GroundImprovementResult(bool success, string description)
    {
        Success     = success;
        Description = description;
    }
}
