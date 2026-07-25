using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

/// <summary>
/// Covers the player contract renewal wage/fee formula — in particular that its
/// stated wage shares InitializationService.CalculateWage with squad generation
/// (docs/specs/player-wage-scaling.md) rather than duplicating the formula.
/// </summary>
public class ContractServiceTests
{
    private static Player MakePlayer(double skill, int age) => new()
    {
        Skill = skill,
        Age   = age
    };

    [Fact]
    public void GetPlayerDemands_HigherDivisionStatesHigherWage()
    {
        var playerOne  = MakePlayer(skill: 5.0, age: 24);
        var playerFour = MakePlayer(skill: 5.0, age: 24);

        var demandOne  = ContractService.GetPlayerDemands(playerOne,  Division.One,  new Random(7));
        var demandFour = ContractService.GetPlayerDemands(playerFour, Division.Four, new Random(7));

        Assert.True(demandOne.StatedWeeklyWage > demandFour.StatedWeeklyWage);
    }

    [Fact]
    public void GetPlayerDemands_StatedWageMatchesSharedFormula()
    {
        var player = MakePlayer(skill: 6.5, age: 30);

        int expected = (int)InitializationService.CalculateWage(
            player.Skill, player.DisplayAge, (int)Division.Two, new Random(3));
        int actual   = ContractService.GetPlayerDemands(player, Division.Two, new Random(3)).StatedWeeklyWage;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetPlayerDemands_MinimumWageIsAtMostStatedWage()
    {
        var player = MakePlayer(skill: 4.0, age: 22);
        var demand = ContractService.GetPlayerDemands(player, Division.Three, new Random(0));

        Assert.True(demand.MinimumWeeklyWage <= demand.StatedWeeklyWage);
    }

    [Fact]
    public void GetPlayerDemands_StatedFeeMatchesScaledFormula()
    {
        // 1,000 * skill / division, then scaled by WageScaleFactor * DivisionWageMultiplier
        // (docs/specs/player-wage-scaling.md) — same constants as the wage above.
        var player = MakePlayer(skill: 6.0, age: 25);
        int expected = (int)(1_000.0 * 6 / (int)Division.Two
            * Constants.WageScaleFactor * Constants.DivisionWageMultiplier((int)Division.Two));

        int actual = ContractService.GetPlayerDemands(player, Division.Two, new Random(0)).StatedSigningFee;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetPlayerDemands_MinimumFeeIsAtMostStatedFee()
    {
        var player = MakePlayer(skill: 7.0, age: 27);
        var demand = ContractService.GetPlayerDemands(player, Division.One, new Random(0));

        Assert.True(demand.MinimumSigningFee <= demand.StatedSigningFee);
    }
}
