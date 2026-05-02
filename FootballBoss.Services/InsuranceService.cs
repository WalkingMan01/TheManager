using FootballBoss.Models;

namespace FootballBoss.Services;

/// <summary>
/// Manages squad insurance: purchasing a policy, weekly premium deduction,
/// and payout calculation for injured players on loan.
///
/// Corresponds to subroutines 4401–4402 and the insurance payout logic in
/// subroutines 2509, 2534–2536 in FOOT.BAS.
/// </summary>
public static class InsuranceService
{
    // ── Purchase a policy (subroutine 4402) ──────────────────────────────────

    /// <summary>
    /// Calculates the cost of a squad insurance policy and (if accepted) deducts
    /// the one-off premium from the club's bank balance.
    ///
    /// BASIC line 4715:
    ///   nr = INT((NP / 85) × NQ)
    ///   where NP = weekly player wage bill and NQ = weeks to insure (1–38).
    /// </summary>
    public static InsuranceQuote GetQuote(double weeklyWageBill, int weeksRequested)
    {
        weeksRequested = Math.Max(1, Math.Min(38, weeksRequested));
        double premium = (int)((weeklyWageBill / 85.0) * weeksRequested);

        return new InsuranceQuote
        {
            WeeksCovered    = weeksRequested,
            PremiumCost     = premium
        };
    }

    /// <summary>
    /// Purchases the policy: deducts the premium and sets the insurance counter.
    /// Returns false if the club cannot afford it.
    /// </summary>
    public static bool PurchasePolicy(Finances finances, InsuranceQuote quote)
    {
        if (finances.BankBalance < quote.PremiumCost) return false;

        finances.BankBalance            -= (int)quote.PremiumCost;
        finances.InsurancePremium        = quote.PremiumCost;
        finances.InsuranceWeeksRemaining = quote.WeeksCovered;

        return true;
    }

    // ── Weekly insurance tick (subroutines 2534–2536) ─────────────────────────

    /// <summary>
    /// Decrements the insurance counter by one week and checks for expiry.
    ///
    /// BASIC lines 2534–2536:
    ///   MC = 0 (reset pools flag); NQ-- (decrement insurance weeks)
    ///   IF NQ=0 THEN print "INSURANCE POLICY HAS EXPIRED"
    /// </summary>
    public static InsuranceTickResult TickInsurance(Finances finances)
    {
        if (finances.InsuranceWeeksRemaining == 0)
            return InsuranceTickResult.NotInsured;

        finances.InsuranceWeeksRemaining--;

        if (finances.InsuranceWeeksRemaining == 0)
        {
            finances.InsurancePremium = 0;
            return InsuranceTickResult.Expired;
        }

        return InsuranceTickResult.Active;
    }

    // ── Payout calculation (subroutine 2509) ─────────────────────────────────

    /// <summary>
    /// Calculates the weekly insurance payout for players currently injured and
    /// placed on loan (J(I)=35 and u(I)>0).
    ///
    /// BASIC line 2509:
    ///   IF NQ>0 AND OO>0 THEN print payout and add to JR.
    ///   OO is the sum of wages of currently injured players on loan.
    /// </summary>
    public static double CalculateWeeklyPayout(Player?[] squad, Finances finances)
    {
        if (finances.InsuranceWeeksRemaining <= 0) return 0;

        double totalPayout = 0;
        for (int slot = 1; slot <= 20; slot++)
        {
            var player = squad[slot];
            if (player == null) continue;

            // Payout covers injured players placed in reserve on loan (J=35 + u>0)
            if (player.Status == PlayerStatus.Injured && player.WeeksUnavailable > 0)
                totalPayout += player.WeeklyWage;
        }

        return totalPayout;
    }
}

// ── Data classes ─────────────────────────────────────────────────────────────

public class InsuranceQuote
{
    public int    WeeksCovered { get; set; }
    public double PremiumCost  { get; set; }
}

public enum InsuranceTickResult
{
    NotInsured,
    Active,
    Expired
}
