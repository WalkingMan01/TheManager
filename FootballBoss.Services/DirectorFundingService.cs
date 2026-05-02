using FootballBoss.Models;

namespace FootballBoss.Services;

/// <summary>
/// Handles the manager's request to the board of directors for a lump-sum
/// transfer budget injection.
///
/// Corresponds to subroutines 1801–1809 in FOOT.BAS (lines 1197–1226).
///
/// The directors' decision depends on the club's current bank balance:
///   Struggling club (balance ≤ £50,000): probability-based approval.
///   Wealthy club   (balance  > £50,000): almost certainly refused or they
///     withdraw an existing sum instead.
/// </summary>
public static class DirectorFundingService
{
    // ── Request a budget injection ────────────────────────────────────────────

    /// <summary>
    /// The manager requests <paramref name="requestedAmount"/> from the board.
    /// Returns a <see cref="FundingDecision"/> indicating what the directors did.
    ///
    /// BASIC lines 1804–1808:
    ///   IF AI &gt; 50000 THEN 1807         ← wealthy-club path
    ///   RA = 200000 + RND(0–479999)      ← random board generosity ceiling
    ///   RB = 1 + RND(0–2)               ← random divisor 1–3
    ///   IF RA/RB &gt; requested THEN agree  ← agree if ceiling ÷ divisor &gt; request
    ///   ELSE refuse
    ///
    ///   Wealthy path (line 1807–1808):
    ///   RA = 1 + RND(0–2); IF RA &lt; 3 THEN refuse  ← 2/3 chance refuse
    ///   ELSE withdraw: RA = INT(AI/100)*20; AI -= RA  ← withdraw 20% of balance
    /// </summary>
    public static FundingDecision RequestBudget(
        Finances finances,
        Division division,
        double   requestedAmount,
        Random   rng)
    {
        if (requestedAmount < 1)
            return new FundingDecision(FundingOutcome.Refused, 0,
                "Invalid amount requested.");

        // ── Wealthy club path (AI > 50,000) ───────────────────────────────────
        if (finances.BankBalance > 50_000)
        {
            int wealthyRoll = 1 + rng.Next(3);

            if (wealthyRoll < 3)
                return new FundingDecision(FundingOutcome.Refused, 0,
                    "Your directors refuse.");

            // Directors withdraw 20% of balance (line 1808: RA = INT(AI/100)*20)
            double withdrawalAmount = (int)(finances.BankBalance / 100) * 20;
            finances.BankBalance   -= (int)withdrawalAmount;

            return new FundingDecision(FundingOutcome.DirectorsWithdrew, withdrawalAmount,
                $"Your directors withdraw £{(int)withdrawalAmount:N0}.");
        }

        // ── Struggling club path ───────────────────────────────────────────────
        // Board generosity ceiling: (200,000 + RND(0–479,999)) / division / RB
        double generosityCeiling = (200_000 + rng.Next(480_000)) / (double)(int)division;
        int    divisor           = 1 + rng.Next(3);

        if (generosityCeiling / divisor > requestedAmount)
        {
            finances.BankBalance += requestedAmount;
            return new FundingDecision(FundingOutcome.Agreed, requestedAmount,
                $"Your directors agree and transfer £{(int)requestedAmount:N0}.");
        }

        return new FundingDecision(FundingOutcome.Refused, 0,
            "Your directors refuse.");
    }
}

// ── Data classes ─────────────────────────────────────────────────────────────

public enum FundingOutcome
{
    Agreed,
    Refused,
    DirectorsWithdrew   // wealthy club: directors take money back instead
}

public class FundingDecision
{
    public FundingOutcome Outcome     { get; }
    public double         Amount      { get; }
    public string         Description { get; }

    public FundingDecision(FundingOutcome outcome, double amount, string description)
    {
        Outcome     = outcome;
        Amount      = amount;
        Description = description;
    }
}
