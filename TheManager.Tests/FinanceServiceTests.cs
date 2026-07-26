using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

/// <summary>
/// Covers the division-scaled TV broadcast income
/// (docs/specs/player-wage-scaling.md — mirrors Constants.DivisionWageMultiplier).
/// </summary>
public class FinanceServiceTests
{
    private static WeeklyReportInput MakeInput(int division, int groundCapacity = 0, int currentWeek = 0) => new()
    {
        GateMoney      = 0,
        PlayerWageBill = 0,
        Division       = division,
        GroundCapacity = groundCapacity,
        CurrentWeek    = currentWeek
    };

    /// <summary>Forces every "1 in N" roll to succeed, so VAT-window tests don't
    /// depend on finding a lucky seed.</summary>
    private sealed class AlwaysRollsZero : Random
    {
        public override int Next(int maxValue) => 0;
    }

    [Fact]
    public void CalculateWeeklyReport_TvBroadcastFires_ScalesWithDivision()
    {
        var financesOne  = new Finances { WeeksSinceLastTvBroadcast = 5 };
        var financesFour = new Finances { WeeksSinceLastTvBroadcast = 5 };

        var reportOne  = FinanceService.CalculateWeeklyReport(MakeInput(1), financesOne,  new Random(0));
        var reportFour = FinanceService.CalculateWeeklyReport(MakeInput(4), financesFour, new Random(0));

        Assert.Equal(20_000 * Constants.DivisionWageMultiplier(1), reportOne.TvBroadcastIncome);
        Assert.Equal(20_000 * Constants.DivisionWageMultiplier(4), reportFour.TvBroadcastIncome);
        Assert.True(reportOne.TvBroadcastIncome > reportFour.TvBroadcastIncome);
    }

    [Fact]
    public void CalculateWeeklyReport_TvBroadcastDoesNotFireBeforeFiveWeeks()
    {
        var finances = new Finances { WeeksSinceLastTvBroadcast = 3 };

        var report = FinanceService.CalculateWeeklyReport(MakeInput(2), finances, new Random(0));

        Assert.Equal(0, report.TvBroadcastIncome);
        Assert.Equal(4, finances.WeeksSinceLastTvBroadcast);
    }

    [Fact]
    public void CalculateWeeklyReport_TvBroadcastResetsCounterAfterFiring()
    {
        var finances = new Finances { WeeksSinceLastTvBroadcast = 5 };

        FinanceService.CalculateWeeklyReport(MakeInput(3), finances, new Random(0));

        Assert.Equal(0, finances.WeeksSinceLastTvBroadcast);
    }

    // ── Sponsorship (docs/specs/… — paid unconditionally, scaled by ground size) ─

    [Fact]
    public void CalculateWeeklyReport_SponsorshipIsPaidEveryWeek()
    {
        var finances = new Finances();

        var report = FinanceService.CalculateWeeklyReport(
            MakeInput(2, Constants.FallbackGroundCapacity(Division.Two)), finances, new Random(0));

        Assert.True(report.SponsorPayment > 0);
    }

    [Fact]
    public void CalculateWeeklyReport_SponsorshipScalesWithGroundCapacity()
    {
        var financesSmall = new Finances();
        var financesLarge = new Finances();

        int typical = Constants.FallbackGroundCapacity(Division.Two);

        var reportSmall = FinanceService.CalculateWeeklyReport(
            MakeInput(2, typical / 2), financesSmall, new Random(0));
        var reportLarge = FinanceService.CalculateWeeklyReport(
            MakeInput(2, typical * 2), financesLarge, new Random(0));

        Assert.True(reportLarge.SponsorPayment > reportSmall.SponsorPayment);
    }

    [Fact]
    public void CalculateWeeklyReport_SponsorshipIsNotCoupledToOverdraftMaximum()
    {
        // OverdraftMaximum is scaled 50x for the debt-cushion feature
        // (Constants.OverdraftScaleFactor) — sponsorship must not inherit that.
        var finances = new Finances { OverdraftMaximum = 6_750_000 }; // Division Two, scaled

        var report = FinanceService.CalculateWeeklyReport(
            MakeInput(2, Constants.FallbackGroundCapacity(Division.Two)), finances, new Random(0));

        Assert.True(report.SponsorPayment < 50_000);
    }

    [Fact]
    public void CalculateWeeklyReport_SponsorshipIsIdenticalAcrossSeedsWhileRunningCostsVary()
    {
        // Sponsorship has its own independent, non-random base (Constants.SponsorshipWeeklyBase)
        // so it must come out the same regardless of the rng draw that only running costs use.
        var input = MakeInput(2, Constants.FallbackGroundCapacity(Division.Two));

        var reportA = FinanceService.CalculateWeeklyReport(input, new Finances(), new Random(1));
        var reportB = FinanceService.CalculateWeeklyReport(input, new Finances(), new Random(42));

        Assert.Equal(reportA.SponsorPayment, reportB.SponsorPayment);
        Assert.NotEqual(reportA.RunningCosts, reportB.RunningCosts);
    }

    // ── VAT bill (docs/specs/… — last-few-weeks window, once per season) ────────

    [Fact]
    public void CalculateWeeklyReport_VatBillDoesNotFireOutsideLastFewWeeks()
    {
        var finances = new Finances { BankBalance = 100_000 };
        var input    = MakeInput(2, currentWeek: Constants.SeasonMatchdays - Constants.VatBillWindowWeeks);

        var report = FinanceService.CalculateWeeklyReport(input, finances, new AlwaysRollsZero());

        Assert.Equal(0, report.VatBill);
        Assert.False(finances.VatPaidThisSeason);
    }

    [Fact]
    public void CalculateWeeklyReport_VatBillCanFireInsideLastFewWeeks()
    {
        var finances = new Finances { BankBalance = 100_000 };
        var input    = MakeInput(2, currentWeek: Constants.SeasonMatchdays);

        var report = FinanceService.CalculateWeeklyReport(input, finances, new AlwaysRollsZero());

        Assert.True(report.VatBill > 0);
        Assert.True(finances.VatPaidThisSeason);
    }

    [Fact]
    public void CalculateWeeklyReport_VatBillFiresAtMostOncePerSeason()
    {
        var finances = new Finances { BankBalance = 100_000 };
        var input    = MakeInput(2, currentWeek: Constants.SeasonMatchdays);

        var first = FinanceService.CalculateWeeklyReport(input, finances, new AlwaysRollsZero());
        Assert.True(first.VatBill > 0);

        finances.BankBalance = 100_000;
        var second = FinanceService.CalculateWeeklyReport(input, finances, new AlwaysRollsZero());

        Assert.Equal(0, second.VatBill);
    }

    [Fact]
    public void RecalculateDivisionFinancials_ResetsVatPaidFlagForNewSeason()
    {
        var finances = new Finances { VatPaidThisSeason = true };

        SeasonService.RecalculateDivisionFinancials(finances, Division.Two);

        Assert.False(finances.VatPaidThisSeason);
    }
}
