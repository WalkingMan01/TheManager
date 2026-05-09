using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Calculates the weekly financial report — gate money, wages, running costs,
/// loan repayments, bonuses, insurance, TV income, and the net profit/loss.
///
/// Corresponds to the "WEEKLY NEWS" section of FOOT.BAS (subroutines 2501–2563,
/// 2528, 2543, 2546–2548).
/// </summary>
public static class FinanceService
{
    // ── Weekly report entry point (lines 2501–2563) ───────────────────────────

    /// <summary>
    /// Computes every line item for the weekly financial report and returns a
    /// <see cref="WeeklyReport"/> with the full breakdown.
    /// Mutates <paramref name="finances"/> with the updated bank balance and
    /// loan/mortgage balances.
    /// </summary>
    public static WeeklyReport CalculateWeeklyReport(
        WeeklyReportInput input,
        Finances finances,
        Random rng)
    {
        var report = new WeeklyReport();

        // Running total for the week — corresponds to JR in FOOT.BAS
        double weeklyBalance = input.GateMoney - input.PlayerWageBill;

        report.GateMoney      = input.GateMoney;
        report.PlayerWageBill = input.PlayerWageBill;

        // ── General running costs (lines 2561–2562) ──────────────────────────
        // FT = co/4  (testimonial/sponsor payment base)
        // FP = co/31 (base running costs, then randomised upward)
        // FQ = co/59, FR = co/99 (variance factors)
        double testimonialPaymentBase  = finances.OverdraftMaximum / 4.0;
        double runningCostsBase        = finances.OverdraftMaximum / 31.0;
        double runningCostMidFactor    = finances.OverdraftMaximum / 59.0;
        double runningCostLowFactor    = finances.OverdraftMaximum / 99.0;
        double costVariance            = runningCostLowFactor
                                       + rng.NextDouble() * runningCostMidFactor
                                       + runningCostsBase / input.Division;
        double weeklyRunningCosts      = (int)(runningCostsBase + costVariance);

        double bankInterestEarned      = (int)(finances.BankBalance * 0.3 / 100);

        report.RunningCosts  = weeklyRunningCosts;
        report.BankInterest  = bankInterestEarned;
        weeklyBalance        = weeklyBalance - weeklyRunningCosts + bankInterestEarned;

        // ── Police match-day bill (line 2503) ─────────────────────────────────
        if (input.PoliceExpenses > 0)
        {
            report.PoliceBill  = input.PoliceExpenses;
            weeklyBalance     -= input.PoliceExpenses;
        }

        // ── Club lottery income (line 2504) ───────────────────────────────────
        if (input.LotteryIncome > 0)
        {
            report.LotteryIncome = input.LotteryIncome;
            weeklyBalance       += input.LotteryIncome;
        }

        // ── Testimonial payment (lines 2506–2508) ─────────────────────────────
        // Fires when a player has >400 games played (ABS(x(I))>400)
        if (input.TestimonialPayment)
        {
            // Testimonial amount = INT(FT - FP) = testimonialPaymentBase - runningCostsBase
            double testimonialAmount = (int)(testimonialPaymentBase - runningCostsBase);
            report.TestimonialPayment = testimonialAmount;
            weeklyBalance            += testimonialAmount;
        }

        // ── Insurance payout (line 2509) ──────────────────────────────────────
        if (finances.InsuranceWeeksRemaining > 0 && input.InsurancePayout > 0)
        {
            report.InsurancePayout = input.InsurancePayout;
            weeklyBalance         += input.InsurancePayout;
        }

        // ── Match bonuses (lines 2512–2513) ───────────────────────────────────
        if (input.WonLeagueMatch)
        {
            report.LeagueBonusPaid = 11.0 * input.LeagueBonus;
            weeklyBalance         -= report.LeagueBonusPaid;
        }
        if (input.WonCupMatch)
        {
            report.CupBonusPaid = 11.0 * input.CupBonus;
            weeklyBalance      -= report.CupBonusPaid;
        }

        // ── Sponsor payment (lines 2516–2517: random 1/35 chance, = FT-FP) ───
        int sponsorRoll = 1 + rng.Next(35);
        if (sponsorRoll == 3)
        {
            double sponsorPayment  = (int)(testimonialPaymentBase - runningCostsBase);
            report.SponsorPayment  = sponsorPayment;
            weeklyBalance         += sponsorPayment;
        }

        // ── TV broadcast income (lines 2518) — £20,000 every 5+ weeks ─────────
        if (finances.WeeksSinceLastTvBroadcast >= 5)
        {
            report.TvBroadcastIncome              = 20_000;
            weeklyBalance                        += 20_000;
            finances.WeeksSinceLastTvBroadcast    = 0;
        }
        else
        {
            finances.WeeksSinceLastTvBroadcast++;
        }

        // ── Manager of the month award (line 2519: MA>=10, ag<=3, CI==MB) ─────
        if (input.IsManagerOfMonthEligible)
        {
            report.ManagerOfMonthBonus = 5_000;
            weeklyBalance             += 5_000;
        }

        // ── Bank loan repayment (lines 2520) ──────────────────────────────────
        if (finances.LoanOutstanding > 0 && finances.WeeklyLoanRepayment > 0)
        {
            report.LoanRepayment        = finances.WeeklyLoanRepayment;
            weeklyBalance              -= finances.WeeklyLoanRepayment;
            finances.LoanOutstanding   -= finances.WeeklyLoanRepayment;
            finances.OverdraftAvailable += finances.WeeklyLoanRepayment;

            if (finances.OverdraftAvailable > finances.OverdraftMaximum)
                finances.OverdraftAvailable = finances.OverdraftMaximum;

            if (finances.LoanOutstanding <= 0)
            {
                finances.LoanOutstanding    = 0;
                finances.WeeklyLoanRepayment = 0;
            }
        }

        // ── Mortgage repayment (lines 2521) ───────────────────────────────────
        if (finances.MortgageOutstanding > 0 && finances.WeeklyMortgageRepayment > 0)
        {
            report.MortgageRepayment          = finances.WeeklyMortgageRepayment;
            weeklyBalance                    -= finances.WeeklyMortgageRepayment;
            finances.MortgageOutstanding     -= finances.WeeklyMortgageRepayment;

            if (finances.MortgageOutstanding <= 0)
            {
                finances.MortgageOutstanding      = 0;
                finances.WeeklyMortgageRepayment  = 0;
            }
        }

        // ── VAT returns (line 2546: 1/15 chance, 15% of balance) ─────────────
        if (rng.Next(15) == 0 && finances.BankBalance > 0)
        {
            report.VatBill = (int)(finances.BankBalance / 100) * 15;
            weeklyBalance -= report.VatBill;
        }

        // ── Directors withdraw (line l2547: triggered by large balance) ───────
        if ((rng.Next(9) == 0 && finances.BankBalance > 300_000)
            || finances.BankBalance > 1_500_000)
        {
            double directorsWithdrawal      = (int)(finances.BankBalance / 2);
            report.DirectorsWithdrawal      = directorsWithdrawal;
            weeklyBalance                  -= directorsWithdrawal;
        }

        // ── Apply net result ──────────────────────────────────────────────────
        report.WeeklyProfit     = (int)weeklyBalance;
        finances.BankBalance    = (int)(finances.BankBalance + weeklyBalance);

        return report;
    }

    // ── Loan obtainment (subroutine 2917) ────────────────────────────────────

    /// <summary>
    /// Applies a bank loan to club finances.
    /// Repaid over 40 weeks at 11% interest (lines 2514–2515).
    /// </summary>
    public static void ObtainLoan(Finances finances, double loanAmount)
    {
        finances.OverdraftAvailable -= loanAmount;
        finances.BankBalance        += loanAmount;

        double totalRepayable         = (int)(loanAmount + (loanAmount / 100.0) * 11);
        finances.LoanOutstanding     += totalRepayable;
        finances.WeeklyLoanRepayment  = finances.LoanOutstanding / 40.0;
    }

    // ── Mortgage obtainment (subroutines 2918–2921) ───────────────────────────

    /// <summary>
    /// Applies a mortgage offer to club finances.
    /// Amount = INT(1,800,000 / Division), repaid over 80 weeks with 26% interest.
    /// </summary>
    public static void ObtainMortgage(Finances finances, Division division)
    {
        double principalAmount                 = (int)(1_800_000.0 / (int)division);
        double interestAmount                  = (int)(principalAmount / 100.0 * 26);
        finances.MortgageOutstanding           = principalAmount + interestAmount;
        finances.WeeklyMortgageRepayment       = (int)(finances.MortgageOutstanding / 80.0);
        finances.BankBalance                  += principalAmount;
    }

    // ── Shares (subroutines 4307, 4310) ──────────────────────────────────────

    /// <summary>
    /// Buys shares: increases share count, drives up price, deducts cash.
    /// BASIC lines 4307: sharesBought = INT(spendAmount / sharePrice) * 100,
    /// price adjusted upward by INT(spend/4894) * 6.
    /// </summary>
    public static void BuyShares(Finances finances, double spendAmount)
    {
        if (spendAmount > finances.BankBalance || spendAmount <= 0) return;

        double priceRiseAdjustment     = (int)(spendAmount / 4894);
        finances.SharePriceInPence    += (int)(priceRiseAdjustment * 6);

        long newSharesBought           = (long)((int)(spendAmount / finances.SharePriceInPence) * 100);
        finances.SharesOwned          += newSharesBought;
        finances.BankBalance          -= (int)spendAmount;
    }

    /// <summary>
    /// Sells shares: decreases share count, drives down price, adds cash.
    /// BASIC lines 4310–4311.
    /// </summary>
    public static void SellShares(Finances finances, long sharesCount)
    {
        if (sharesCount <= 0 || sharesCount > finances.SharesOwned) return;

        finances.SharesOwned -= sharesCount;

        double sharePriceDropFactor    = Math.Max(0, (int)(sharesCount / 1000));
        finances.SharePriceInPence    -= (finances.SharePriceInPence / 100.0) * sharePriceDropFactor;

        double saleProceeds            = (int)(finances.SharePriceInPence * sharesCount) / 100.0;
        finances.BankBalance          += (int)saleProceeds;

        finances.SharesSoldDanger      = finances.SharesOwned < 1;
    }
}

// ── Input / output data classes ───────────────────────────────────────────────

/// <summary>Inputs gathered before calling <see cref="FinanceService.CalculateWeeklyReport"/>.</summary>
public class WeeklyReportInput
{
    public double GateMoney                  { get; set; }   // bl
    public double PlayerWageBill             { get; set; }   // NP
    public double PoliceExpenses             { get; set; }   // ol
    public double LotteryIncome              { get; set; }   // O1N
    public double InsurancePayout            { get; set; }   // OO
    public double LeagueBonus                { get; set; }   // NW
    public double CupBonus                   { get; set; }   // NV
    public bool   WonLeagueMatch             { get; set; }   // ot flag
    public bool   WonCupMatch                { get; set; }   // os flag
    public bool   IsManagerOfMonthEligible   { get; set; }   // MA>=10 AND ag<=3 AND CI==MB
    public bool   TestimonialPayment         { get; set; }   // ABS(x(I))>400 for any player
    public int    Division                   { get; set; }   // AP
}

/// <summary>Line-by-line breakdown of the week's finances.</summary>
public class WeeklyReport
{
    public double GateMoney            { get; set; }
    public double PlayerWageBill       { get; set; }
    public double RunningCosts         { get; set; }
    public double BankInterest         { get; set; }
    public double PoliceBill           { get; set; }
    public double LotteryIncome        { get; set; }
    public double TestimonialPayment   { get; set; }
    public double InsurancePayout      { get; set; }
    public double LeagueBonusPaid      { get; set; }
    public double CupBonusPaid         { get; set; }
    public double SponsorPayment       { get; set; }
    public double TvBroadcastIncome    { get; set; }
    public double ManagerOfMonthBonus  { get; set; }
    public double LoanRepayment        { get; set; }
    public double MortgageRepayment    { get; set; }
    public double VatBill              { get; set; }
    public double DirectorsWithdrawal  { get; set; }
    public double WeeklyProfit         { get; set; }
}
