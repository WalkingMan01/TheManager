using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class MarketServiceTests
{
    // ── ListForTransfer / UnlistFromTransfer / IsListed ───────────────────────

    [Fact]
    public void ListForTransfer_NegatesAge()
    {
        var player = new Player { Age = 25 };
        MarketService.ListForTransfer(player);
        Assert.Equal(-25, player.Age);
    }

    [Fact]
    public void ListForTransfer_AlreadyListed_NoChange()
    {
        var player = new Player { Age = -25 };
        MarketService.ListForTransfer(player);
        Assert.Equal(-25, player.Age);
    }

    [Fact]
    public void UnlistFromTransfer_RestoresAge()
    {
        var player = new Player { Age = -25 };
        MarketService.UnlistFromTransfer(player);
        Assert.Equal(25, player.Age);
    }

    [Fact]
    public void IsListed_NegativeAge_ReturnsTrue()
    {
        Assert.True(MarketService.IsListed(new Player { Age = -20 }));
    }

    [Fact]
    public void IsListed_PositiveAge_ReturnsFalse()
    {
        Assert.False(MarketService.IsListed(new Player { Age = 20 }));
    }

    // ── SellPlayer ────────────────────────────────────────────────────────────

    [Fact]
    public void SellPlayer_AddsFeeToBalance()
    {
        var (squad, finances, market) = MakeSaleSetup(squadSlot: 5, fee: 50_000);
        MarketService.SellPlayer(squad, finances, market, squadSlot: 5, fee: 50_000);
        Assert.Equal(150_000, finances.BankBalance);
    }

    [Fact]
    public void SellPlayer_NullsSquadSlot()
    {
        var (squad, finances, market) = MakeSaleSetup(squadSlot: 5, fee: 50_000);
        MarketService.SellPlayer(squad, finances, market, squadSlot: 5, fee: 50_000);
        Assert.Null(squad[5]);
    }

    [Fact]
    public void SellPlayer_SetsRecordSale_WhenFeeIsHigher()
    {
        var (squad, finances, market) = MakeSaleSetup(squadSlot: 5, fee: 500_000);
        finances.RecordSaleFee = 100_000;

        var newRecord = MarketService.SellPlayer(squad, finances, market, squadSlot: 5, fee: 500_000);

        Assert.NotNull(newRecord);
        Assert.Equal(500_000, finances.RecordSaleFee);
    }

    [Fact]
    public void SellPlayer_ReturnsNull_WhenNotNewRecord()
    {
        var (squad, finances, market) = MakeSaleSetup(squadSlot: 5, fee: 10_000);
        finances.RecordSaleFee = 500_000;

        var newRecord = MarketService.SellPlayer(squad, finances, market, squadSlot: 5, fee: 10_000);

        Assert.Null(newRecord);
    }

    [Fact]
    public void SellPlayer_RemovesMarketInterestForSlot()
    {
        var (squad, finances, market) = MakeSaleSetup(squadSlot: 5, fee: 10_000);
        market.Add(new TransferListing { SourceSquadSlot = 5 });
        market.Add(new TransferListing { SourceSquadSlot = 7 }); // different slot — should stay

        MarketService.SellPlayer(squad, finances, market, squadSlot: 5, fee: 10_000);

        Assert.Single(market);
        Assert.Equal(7, market[0].SourceSquadSlot);
    }

    [Fact]
    public void SellPlayer_NullPlayer_DoesNothing()
    {
        var squad    = new Player?[29];
        var finances = new Finances { BankBalance = 100_000 };
        var market   = new List<TransferListing>();

        MarketService.SellPlayer(squad, finances, market, squadSlot: 5, fee: 50_000);

        Assert.Equal(100_000, finances.BankBalance);
    }

    // ── GenerateIncomingInterest ──────────────────────────────────────────────

    [Fact]
    public void GenerateIncomingInterest_NoListedPlayers_ReturnsEmpty()
    {
        var squad    = new Player?[29];
        squad[1]     = new Player { Name = "Jones", Age = 25 };   // not listed

        var result = MarketService.GenerateIncomingInterest(
            squad, Division.Two, "OurClub", MakeTeamNames(), new Random(0));

        Assert.Empty(result);
    }

    [Fact]
    public void GenerateIncomingInterest_WithListedPlayer_ReturnsAtMost3Listings()
    {
        var squad = MakeSquadWithListedPlayer(slot: 5);

        var result = MarketService.GenerateIncomingInterest(
            squad, Division.Two, "Team21", MakeTeamNames(), new Random(42));

        Assert.True(result.Count <= 3);
    }

    [Fact]
    public void GenerateIncomingInterest_NoDuplicatesForSamePlayer()
    {
        var squad = MakeSquadWithListedPlayer(slot: 5);

        var result = MarketService.GenerateIncomingInterest(
            squad, Division.Two, "Team21", MakeTeamNames(), new Random(0));

        var sourceSlots = result.Select(l => l.SourceSquadSlot).ToList();
        Assert.Equal(sourceSlots.Distinct().Count(), sourceSlots.Count);
    }

    // ── GenerateRivalBid ──────────────────────────────────────────────────────

    [Fact]
    public void GenerateRivalBid_ReturnsPositiveValue()
    {
        var player = new Player { Skill = 7.0, Age = 25 };
        double bid = MarketService.GenerateRivalBid(player, rivalDivision: 2, new Random(0));
        Assert.True(bid > 0);
    }

    [Fact]
    public void GenerateRivalBid_IsRoundedToNearest1000()
    {
        var player = new Player { Skill = 7.0, Age = 25 };
        double bid = MarketService.GenerateRivalBid(player, rivalDivision: 2, new Random(0));
        Assert.Equal(0, bid % 1000);
    }

    [Fact]
    public void GenerateRivalBid_Division1_BidsMoreThanDivision4()
    {
        // Same player, same seed → same asking price and same NextDouble fraction sample.
        // div1 minFraction (0.85) > div4 minFraction (0.60) so div1 bid is always higher.
        var player   = new Player { Skill = 7.0, Age = 25 };
        double div1Bid = MarketService.GenerateRivalBid(player, rivalDivision: 1, new Random(42));
        double div4Bid = MarketService.GenerateRivalBid(player, rivalDivision: 4, new Random(42));
        Assert.True(div1Bid > div4Bid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void GenerateRivalBid_AllDivisions_ReturnPositiveBid(int division)
    {
        var player = new Player { Skill = 6.0, Age = 28 };
        double bid = MarketService.GenerateRivalBid(player, division, new Random(0));
        Assert.True(bid > 0);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (Player?[] squad, Finances finances, List<TransferListing> market)
        MakeSaleSetup(int squadSlot, double fee)
    {
        var squad    = new Player?[29];
        squad[squadSlot] = new Player { Name = "Smith", Skill = 6.0, Age = 25 };
        var finances = new Finances { BankBalance = 100_000 };
        var market   = new List<TransferListing>();
        return (squad, finances, market);
    }

    private static Player?[] MakeSquadWithListedPlayer(int slot)
    {
        var squad  = new Player?[29];
        squad[slot] = new Player { Name = "Jones", Skill = 5.0, Age = -25 }; // listed
        return squad;
    }

    private static string[] MakeTeamNames()
    {
        var names = new string[81];
        for (int i = 1; i <= 80; i++) names[i] = $"Team{i:D2}";
        return names;
    }
}
