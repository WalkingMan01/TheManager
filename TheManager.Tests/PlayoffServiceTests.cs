using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class PlayoffServiceTests
{
    private static LeagueTable MakeTable(params string[] teamNamesInOrder)
    {
        var table = new LeagueTable { Division = Division.Two };
        foreach (var name in teamNamesInOrder)
            table.Entries.Add(new LeagueEntry { TeamName = name });
        return table;
    }

    // ── BuildSemiFinals ────────────────────────────────────────────────────────

    [Fact]
    public void BuildSemiFinals_ChampionshipShape_PairsThirdVsSixthAndFourthVsFifth()
    {
        var table = MakeTable("1st", "2nd", "3rd", "4th", "5th", "6th", "7th");

        var (higherA, lowerA, higherB, lowerB) = PlayoffService.BuildSemiFinals(table, autoSpots: 2);

        Assert.Equal("3rd", higherA);
        Assert.Equal("6th", lowerA);
        Assert.Equal("4th", higherB);
        Assert.Equal("5th", lowerB);
    }

    [Fact]
    public void BuildSemiFinals_LeagueTwoShape_PairsFourthVsSeventhAndFifthVsSixth()
    {
        var table = MakeTable("1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th");

        var (higherA, lowerA, higherB, lowerB) = PlayoffService.BuildSemiFinals(table, autoSpots: 3);

        Assert.Equal("4th", higherA);
        Assert.Equal("7th", lowerA);
        Assert.Equal("5th", higherB);
        Assert.Equal("6th", lowerB);
    }

    // ── SimulateTie ────────────────────────────────────────────────────────────

    [Fact]
    public void SimulateTie_AlwaysProducesANonEmptyWinner()
    {
        var rng = new Random(1);
        for (int i = 0; i < 200; i++)
        {
            var result = PlayoffService.SimulateTie("Home", 3, "Away", 6, rng);
            Assert.False(string.IsNullOrEmpty(result.Winner));
            Assert.True(result.Winner is "Home" or "Away");
        }
    }

    [Fact]
    public void SimulateTie_WinnerIsWhicheverScoredMore()
    {
        var rng = new Random(2);
        for (int i = 0; i < 200; i++)
        {
            var result = PlayoffService.SimulateTie("Home", 3, "Away", 6, rng);
            if (result.WonOnPenalties) continue;

            string expectedWinner = result.HomeScore > result.AwayScore ? "Home" : "Away";
            Assert.Equal(expectedWinner, result.Winner);
        }
    }

    [Fact]
    public void SimulateTie_LevelScore_GoesToPenalties()
    {
        // Deterministic seed sweep: walk seeds until a level-score tie occurs,
        // then assert penalties were used to decide it. The first such seed is
        // fixed forever, so this never flakes.
        for (int seed = 0; seed < 500; seed++)
        {
            var rng    = new Random(seed);
            var result = PlayoffService.SimulateTie("Home", 3, "Away", 4, rng);

            if (result.HomeScore != result.AwayScore) continue;

            Assert.True(result.WonOnPenalties);
            Assert.NotNull(result.HomePenalties);
            Assert.NotNull(result.AwayPenalties);
            Assert.NotEqual(result.HomePenalties, result.AwayPenalties);
            return;
        }

        Assert.Fail("no level-score tie in 500 seeds");
    }
}
