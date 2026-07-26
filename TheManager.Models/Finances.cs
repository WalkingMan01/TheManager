namespace TheManager.Models;

/// <summary>
/// Club financial state. All monetary values are in pounds unless noted.
/// </summary>
public class Finances
{
    // ── Current account ───────────────────────────────────────────────────────

    /// <summary>Current bank balance. Corresponds to AI.</summary>
    public double BankBalance { get; set; }

    /// <summary>
    /// Remaining overdraft the bank will extend.
    /// Maximum = INT(310,000 / Division) − (Division × 10,000).
    /// Corresponds to KA (and co for the ceiling).
    /// </summary>
    public double OverdraftAvailable { get; set; }

    /// <summary>Division-based maximum overdraft ceiling. Corresponds to co.</summary>
    public double OverdraftMaximum { get; set; }

    // ── Loan ──────────────────────────────────────────────────────────────────

    /// <summary>Total loan balance still owed to the bank. Corresponds to KC.</summary>
    public double LoanOutstanding { get; set; }

    /// <summary>
    /// Weekly loan repayment amount. = KC / 40 weeks.
    /// Corresponds to CF.
    /// </summary>
    public double WeeklyLoanRepayment { get; set; }

    // ── Mortgage ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Mortgage balance outstanding. Offered at INT(1,800,000 / Division).
    /// Corresponds to JQ.
    /// </summary>
    public double MortgageOutstanding { get; set; }

    /// <summary>
    /// Weekly mortgage repayment. = (JQ + 26% interest) / 80 weeks.
    /// Corresponds to JP.
    /// </summary>
    public double WeeklyMortgageRepayment { get; set; }

    // ── Shares ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Number of shares owned by the club / available to sell.
    /// Starts at 100,000. Corresponds to AU.
    /// </summary>
    public long SharesOwned { get; set; }

    /// <summary>
    /// Current share price in pence. Tied to league position AK.
    /// Corresponds to AK.
    /// </summary>
    public double SharePriceInPence { get; set; }

    /// <summary>True when the board have sold enough shares to trigger a takeover risk.</summary>
    public bool SharesSoldDanger { get; set; }   // JU flag

    // ── Weekly wage bill ──────────────────────────────────────────────────────

    /// <summary>Players-only weekly wage total. Corresponds to NP (before staff added).</summary>
    public double PlayerWageBill { get; set; }

    /// <summary>Manager weekly wage. Corresponds to NA.</summary>
    public double ManagerWeeklyWage { get; set; }

    /// <summary>
    /// Directors and general staff weekly cost.
    /// Randomised each season: (5000 + RND*3000) / Division. Corresponds to NF.
    /// </summary>
    public double DirectorsCosts { get; set; }

    // ── Weekly income / expenditure breakdown ─────────────────────────────────

    /// <summary>Attendance at the last home game. Corresponds to dn.</summary>
    public double LastMatchAttendance { get; set; }

    /// <summary>Gate money from the last match. Corresponds to bl.</summary>
    public double LastMatchGateMoney { get; set; }

    /// <summary>
    /// General running costs for the week (random fraction of DirectorsCosts).
    /// Corresponds to FP.
    /// </summary>
    public double WeeklyRunningCosts { get; set; }

    /// <summary>
    /// Bank interest earned this week (AI × 0.3 / 100). Corresponds to FM.
    /// </summary>
    public double WeeklyBankInterest { get; set; }

    /// <summary>Police match-day bill this week. Corresponds to ol.</summary>
    public double WeeklyPoliceExpenses { get; set; }

    /// <summary>Club lottery income this week. Corresponds to O1N.</summary>
    public double WeeklyLotteryIncome { get; set; }

    /// <summary>Net profit or loss for the current week. Corresponds to JR.</summary>
    public double WeeklyProfit { get; set; }

    /// <summary>
    /// True once the VAT bill has fired this season. VAT is only rolled for in the
    /// last few weeks of the season and at most once — reset to false on season
    /// rollover by SeasonService.RecalculateDivisionFinancials.
    /// </summary>
    public bool VatPaidThisSeason { get; set; }

    // ── Insurance ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Weeks of squad insurance remaining.
    /// -1 = never taken out, 0 = expired. Corresponds to NQ.
    /// </summary>
    public int InsuranceWeeksRemaining { get; set; }

    /// <summary>One-off insurance premium paid. Corresponds to nr.</summary>
    public double InsurancePremium { get; set; }

    /// <summary>
    /// Weekly insurance payout for players currently on loan while injured.
    /// Added to weekly income. Corresponds to OO.
    /// </summary>
    public double InsurancePayout { get; set; }

    // ── TV / special income ───────────────────────────────────────────────────

    /// <summary>
    /// Weeks since last TV live broadcast. Broadcast income (£20,000) is paid
    /// when this reaches 5. Corresponds to DK.
    /// </summary>
    public int WeeksSinceLastTvBroadcast { get; set; }

    // ── All-time club records ─────────────────────────────────────────────────

    /// <summary>Highest transfer fee paid. Corresponds to PA.</summary>
    public double RecordSigningFee { get; set; }

    /// <summary>Highest transfer fee received. Corresponds to PB.</summary>
    public double RecordSaleFee { get; set; }

    /// <summary>Highest ever home attendance. Corresponds to PF.</summary>
    public double HighestAttendance { get; set; }

    /// <summary>Lowest ever home attendance. Corresponds to PG.</summary>
    public double LowestAttendance { get; set; }
}
