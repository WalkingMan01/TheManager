using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

/// <summary>
/// Covers the division-scaled TV broadcast income
/// (docs/specs/player-wage-scaling.md — mirrors Constants.DivisionWageMultiplier).
/// </summary>
public class FinanceServiceTests
{
    private static WeeklyReportInput MakeInput(int division) => new()
    {
        GateMoney      = 0,
        PlayerWageBill = 0,
        Division       = division
    };

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
}
