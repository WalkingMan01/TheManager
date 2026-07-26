namespace TheManager.Models;

/// <summary>
/// Line-by-line breakdown of the week's finances, produced by
/// FinanceService.CalculateWeeklyReport and surfaced to the UI via
/// <see cref="MatchResult.FinanceReport"/>. Corresponds to the "WEEKLY NEWS"
/// section of FOOT.BAS (subroutines 2501–2563).
/// </summary>
public class WeeklyReport
{
    public double GateMoney            { get; set; }
    public double PlayerWageBill       { get; set; }
    public double RunningCosts         { get; set; }
    public double BankInterest         { get; set; }
    public double PoliceBill           { get; set; }
    public double LotteryIncome        { get; set; }
    public double InsurancePayout      { get; set; }
    public double LeagueBonusPaid      { get; set; }
    public double CupBonusPaid         { get; set; }
    public double SponsorPayment       { get; set; }
    public double TvBroadcastIncome    { get; set; }
    public double ManagerOfMonthBonus  { get; set; }
    public double LoanRepayment        { get; set; }
    public double MortgageRepayment    { get; set; }
    public double VatBill              { get; set; }
    public double WeeklyProfit         { get; set; }
}
