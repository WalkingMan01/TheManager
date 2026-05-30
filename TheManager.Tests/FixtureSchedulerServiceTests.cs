using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.Tests;

public class FixtureSchedulerServiceTests
{
    // ── GetRoundPairings ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(37)]
    public void GetRoundPairings_EachTeamAppearsExactlyOnce(int round)
    {
        var pairs = FixtureSchedulerService.GetRoundPairings(round);
        var seen  = new HashSet<int>();

        foreach (var (home, away) in pairs)
        {
            Assert.True(seen.Add(home), $"Team {home} appears twice in round {round}");
            Assert.True(seen.Add(away), $"Team {away} appears twice in round {round}");
        }

        Assert.Equal(20, seen.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(18)]
    [InlineData(37)]
    public void GetRoundPairings_NoTeamPlayesItself(int round)
    {
        var pairs = FixtureSchedulerService.GetRoundPairings(round);
        Assert.All(pairs, p => Assert.NotEqual(p.homeIdx, p.awayIdx));
    }

    [Fact]
    public void GetRoundPairings_FullSeasonCoversAllPairings()
    {
        // Each of the 20×19=380 ordered (home,away) pairs should appear exactly once
        var seen = new HashSet<(int, int)>();
        for (int round = 0; round < 38; round++)
        {
            foreach (var pair in FixtureSchedulerService.GetRoundPairings(round))
                Assert.True(seen.Add(pair), $"Duplicate pair {pair} in round {round}");
        }

        Assert.Equal(20 * 19, seen.Count);
    }

    [Fact]
    public void GetRoundPairings_FirstHalf_FixedTeamIsHome()
    {
        // In rounds 0-18 the fixed team (index 19) should be the home team for pair[0]
        for (int round = 0; round < 19; round++)
        {
            var pairs = FixtureSchedulerService.GetRoundPairings(round);
            Assert.Equal(19, pairs[0].homeIdx);
        }
    }

    [Fact]
    public void GetRoundPairings_SecondHalf_FixedTeamIsAway()
    {
        for (int round = 19; round < 38; round++)
        {
            var pairs = FixtureSchedulerService.GetRoundPairings(round);
            Assert.Equal(19, pairs[0].awayIdx);
        }
    }

    // ── AdvanceWeek ───────────────────────────────────────────────────────────

    [Fact]
    public void AdvanceWeek_IncrementsCurrentWeek()
    {
        var result = FixtureSchedulerService.AdvanceWeek(currentWeek: 5, fixturesPlayed: 4);
        Assert.Equal(6, result.CurrentWeek);
    }

    [Fact]
    public void AdvanceWeek_IncrementsFixturesPlayed()
    {
        var result = FixtureSchedulerService.AdvanceWeek(currentWeek: 1, fixturesPlayed: 10);
        Assert.Equal(11, result.FixturesPlayed);
    }

    [Fact]
    public void AdvanceWeek_MatchesRemainingDecrements()
    {
        var result = FixtureSchedulerService.AdvanceWeek(currentWeek: 1, fixturesPlayed: 10);
        Assert.Equal(38 - 11, result.MatchesRemainingThisSeason);
    }

    [Fact]
    public void AdvanceWeek_CapsFixturesAt38()
    {
        var result = FixtureSchedulerService.AdvanceWeek(currentWeek: 38, fixturesPlayed: 38);
        Assert.Equal(38, result.FixturesPlayed);
        Assert.Equal(0, result.MatchesRemainingThisSeason);
    }

    // ── GetDivisionStartIndex ─────────────────────────────────────────────────

    [Theory]
    [InlineData(Division.One,   1)]
    [InlineData(Division.Two,  21)]
    [InlineData(Division.Three,41)]
    [InlineData(Division.Four, 61)]
    public void GetDivisionStartIndex_ReturnsCorrectIndex(Division division, int expected)
    {
        Assert.Equal(expected, FixtureSchedulerService.GetDivisionStartIndex(division));
    }

    // ── GetCurrentMatch ───────────────────────────────────────────────────────

    [Fact]
    public void GetCurrentMatch_PastEndOfSeason_ReturnsEndOfSeason()
    {
        var result = FixtureSchedulerService.GetCurrentMatch(currentWeek: 39, fixtures: new List<ScheduledMatch>());
        Assert.Equal(MatchType.EndOfSeason, result.MatchType);
    }

    [Fact]
    public void GetCurrentMatch_MatchingWeek_ReturnsFixture()
    {
        var fixture = new ScheduledMatch { Week = 5, MatchType = MatchType.League, OpponentName = "Rivals" };
        var result  = FixtureSchedulerService.GetCurrentMatch(5, new List<ScheduledMatch> { fixture });

        Assert.Equal("Rivals", result.OpponentName);
        Assert.Equal(MatchType.League, result.MatchType);
    }

    [Fact]
    public void GetCurrentMatch_NoMatchingWeek_ReturnsEndOfSeason()
    {
        var result = FixtureSchedulerService.GetCurrentMatch(5, new List<ScheduledMatch>());
        Assert.Equal(MatchType.EndOfSeason, result.MatchType);
    }

    // ── GenerateSeasonFixtures ────────────────────────────────────────────────

    [Fact]
    public void GenerateSeasonFixtures_Returns38Fixtures()
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.One, clubIndex: 5);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.One, clubName, names);
        Assert.Equal(38, fixtures.Count);
    }

    [Fact]
    public void GenerateSeasonFixtures_19HomeAnd19Away()
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.One, clubIndex: 5);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.One, clubName, names);

        Assert.Equal(19, fixtures.Count(f => f.IsHomeGame));
        Assert.Equal(19, fixtures.Count(f => !f.IsHomeGame));
    }

    [Fact]
    public void GenerateSeasonFixtures_EachOpponentAppearsExactlyTwice()
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.One, clubIndex: 5);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.One, clubName, names);

        var opponentCounts = fixtures.GroupBy(f => f.OpponentName).ToDictionary(g => g.Key, g => g.Count());
        Assert.All(opponentCounts, kvp => Assert.Equal(2, kvp.Value));
    }

    [Fact]
    public void GenerateSeasonFixtures_WeekNumbersAre1To38()
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.One, clubIndex: 1);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.One, clubName, names);

        var weeks = fixtures.Select(f => f.Week).OrderBy(w => w).ToList();
        Assert.Equal(Enumerable.Range(1, 38).ToList(), weeks);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (string[] names, string clubName) MakeTeamNamesWithClub(
        Division division, int clubIndex)
    {
        var names = new string[81];
        for (int i = 1; i <= 80; i++) names[i] = $"Team{i:D2}";
        int divStart = (int)division * 20 - 19;
        string clubName = names[divStart + clubIndex];
        return (names, clubName);
    }
}
