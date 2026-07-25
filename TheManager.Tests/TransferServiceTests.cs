using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

/// <summary>
/// Covers TransferService.CalculateAskingPrice's division scaling
/// (docs/specs/player-wage-scaling.md — uses Constants.TransferFeeDivisionMultiplier,
/// a softer scale than player wages use).
/// </summary>
public class TransferServiceTests
{
    [Fact]
    public void CalculateAskingPrice_ReturnsPositiveValue()
    {
        var player = new Player { Skill = 5.0, Age = 24 };
        double price = TransferService.CalculateAskingPrice(player, sellingDivision: 3, new Random(0));
        Assert.True(price > 0);
    }

    [Fact]
    public void CalculateAskingPrice_HigherDivisionAsksMore()
    {
        // Same player, same seed; only the selling club's division differs.
        var playerOne  = new Player { Skill = 7.0, Age = 24 };
        var playerFour = new Player { Skill = 7.0, Age = 24 };

        double priceOne  = TransferService.CalculateAskingPrice(playerOne,  sellingDivision: 1, new Random(9));
        double priceFour = TransferService.CalculateAskingPrice(playerFour, sellingDivision: 4, new Random(9));

        Assert.True(priceOne > priceFour);
        Assert.Equal(
            Constants.TransferFeeDivisionMultiplier(1) / Constants.TransferFeeDivisionMultiplier(4),
            priceOne / priceFour, precision: 6);
    }

    [Fact]
    public void CalculateAskingPrice_StarPlayerReceivesBonus()
    {
        var star    = new Player { Skill = 9.9, Age = 24 };
        var nonStar = new Player { Skill = 9.0, Age = 24 };

        double starPrice    = TransferService.CalculateAskingPrice(star,    sellingDivision: 1, new Random(0));
        double nonStarPrice = TransferService.CalculateAskingPrice(nonStar, sellingDivision: 1, new Random(0));

        Assert.True(starPrice > nonStarPrice);
    }

    [Fact]
    public void CalculateAskingPrice_OlderPlayerAsksLess()
    {
        var young = new Player { Skill = 7.0, Age = 24 };
        var old   = new Player { Skill = 7.0, Age = 34 };

        double youngPrice = TransferService.CalculateAskingPrice(young, sellingDivision: 1, new Random(3));
        double oldPrice   = TransferService.CalculateAskingPrice(old,   sellingDivision: 1, new Random(3));

        Assert.True(youngPrice > oldPrice);
    }
}
