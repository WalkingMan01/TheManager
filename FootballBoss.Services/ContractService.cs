using FootballBoss.Models;

namespace FootballBoss.Services;

/// <summary>
/// Manages contract re-negotiations — both the board's offer to the manager
/// and the manager's re-negotiation of expiring player contracts.
///
/// Corresponds to subroutines 1399–1415 (manager contract) and
/// 359–366 (player contract renewal) in FOOT.BAS.
/// </summary>
public static class ContractService
{
    // ── Board offer to manager (lines 1400–1409) ──────────────────────────────

    /// <summary>
    /// Generates the board's contract offer to the manager.
    ///
    /// BASIC lines 1401–1402:
    ///   offeredWage = INT(200 - 7.5 * leaguePosition) * (5 - division)
    ///   contractWeeks = INT((20 / leaguePosition) * 10) + (1 + RND*20)
    /// </summary>
    public static ManagerContractOffer GenerateBoardOffer(
        int      leaguePosition,
        Division division,
        Random   rng)
    {
        double offeredWage         = (int)((200 - 7.5 * leaguePosition) * (5 - (int)division));
        double signingBonusPortion = (offeredWage / 100.0) * 20;
        offeredWage               -= signingBonusPortion;

        int contractWeeks          = (int)((20.0 / leaguePosition) * 10) + 1 + rng.Next(20);
        int sackableAfterWeeks     = Math.Max(0, (int)(14 - leaguePosition) - rng.Next(7));

        return new ManagerContractOffer
        {
            WeeklyWage         = (int)offeredWage,
            ContractWeeks      = contractWeeks,
            SackableAfterWeeks = sackableAfterWeeks
        };
    }

    /// <summary>
    /// Called when the manager keeps re-negotiating (presses "Y" to renegotiate).
    /// The board may increase the wage offer slightly each cycle, but after a
    /// random check they may lose patience and install a new manager elsewhere.
    ///
    /// BASIC lines 1408–1409:
    ///   IF offeredWage &lt; originalWage THEN offeredWage += 10
    ///   RA = RND*4; if RA=1 → board loses patience
    /// </summary>
    public static RenegotiateOutcome RenegotiateManagerContract(
        ref ManagerContractOffer offer,
        double                   originalWage,
        int                      leaguePosition,
        Random                   rng)
    {
        if (offer.WeeklyWage < originalWage) offer.WeeklyWage += 10;

        int boardPatienceRoll = 1 + rng.Next(4);
        if (boardPatienceRoll == 1)
            return RenegotiateOutcome.BoardLosesPatience;

        // Regenerate with fresh random terms
        offer = GenerateBoardOffer(leaguePosition, Division.One, rng);
        return RenegotiateOutcome.NewOffer;
    }

    // ── Player contract renewal (lines 359–366, 363) ─────────────────────────

    /// <summary>
    /// Releases a player (manager chose not to renew).
    /// Clears the squad slot.
    /// BASIC lines 412–414: I=F; GOSUB 200; GOSUB 332.
    /// </summary>
    public static void ReleasePlayer(Player?[] squad, int squadSlot)
    {
        PlayerService.ClearSlot(squad, squadSlot);
    }

    // ── Player's minimum acceptable renewal terms ─────────────────────────────

    /// <summary>
    /// Returns the minimum weekly wage and maximum contract length the player
    /// will accept for a renewal. Uses the same formula as a full transfer offer.
    ///
    /// BASIC lines 2610–2615 applied in the renewal path (HF=3).
    /// </summary>
    public static PlayerRenewalRequirements GetRenewalRequirements(
        Player   player,
        Division clubDivision,
        Random   rng)
    {
        double minimumAcceptableWage =
            (1 + rng.Next(20) + 50) * (int)player.Skill
            + (player.Skill > 9.6 ? 1_000 : 0);
        int ageDivisor               = Math.Max(1, player.DisplayAge - 27);
        minimumAcceptableWage        = minimumAcceptableWage / ageDivisor;
        minimumAcceptableWage        = Math.Max(50, minimumAcceptableWage);

        int maxAcceptableContractWeeks = Math.Max(1, player.DisplayAge - 23);
        maxAcceptableContractWeeks    += rng.Next(2) == 1 ? 1 : 0;
        maxAcceptableContractWeeks    *= 53;

        return new PlayerRenewalRequirements
        {
            MinimumWeeklyWage      = (int)minimumAcceptableWage,
            MaximumContractWeeks   = maxAcceptableContractWeeks
        };
    }

    /// <summary>
    /// Applies an accepted renewal — deducts the signing fee from finances.
    /// BASIC line 2616: AI-=HT (signing fee).
    /// </summary>
    public static void ApplyRenewal(Finances finances, double signingFee)
    {
        finances.BankBalance -= (int)signingFee;
    }

    // ── End-of-week countdowns ────────────────────────────────────────────────

    /// <summary>
    /// Decrements the manager's contract. Returns true when the contract has
    /// expired and the board must generate a new offer.
    /// </summary>
    public static bool TickManagerContract(Club club)
    {
        if (club.ManagerContractWeeks <= 0) return false;
        club.ManagerContractWeeks--;
        return club.ManagerContractWeeks == 0;
    }
}

// ── Data classes ─────────────────────────────────────────────────────────────

public class ManagerContractOffer
{
    /// <summary>Weekly wage offered (NA).</summary>
    public double WeeklyWage         { get; set; }

    /// <summary>Contract length in weeks (NC).</summary>
    public int ContractWeeks         { get; set; }

    /// <summary>Weeks before the board can sack the manager (ne).</summary>
    public int SackableAfterWeeks    { get; set; }
}

public class PlayerRenewalRequirements
{
    public double MinimumWeeklyWage    { get; set; }
    public int    MaximumContractWeeks { get; set; }
}

public enum RenegotiateOutcome
{
    NewOffer,
    BoardLosesPatience
}
