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
    public void GetRoundPairings_NoTeamPlaysItself(int round)
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
    public void GetRoundPairings_FirstHalfRounds_SameTeamIsAlwaysHome()
    {
        // In rounds 0-18 the fixed team (index 19) should be the home team for pair[0]
        for (int round = 0; round < 19; round++)
        {
            var pairs = FixtureSchedulerService.GetRoundPairings(round);
            Assert.Equal(19, pairs[0].homeIdx);
        }
    }

    [Fact]
    public void GetRoundPairings_SecondHalfRounds_SameTeamIsAlwaysAway()
    {
        for (int round = 19; round < 38; round++)
        {
            var pairs = FixtureSchedulerService.GetRoundPairings(round);
            Assert.Equal(19, pairs[0].awayIdx);
        }
    }

    // ── AdvanceWeek ───────────────────────────────────────────────────────────

    [Fact]
    public void AdvanceWeek_ReturnsNextWeekNumber()
    {
        var result = FixtureSchedulerService.AdvanceWeek(currentWeek: 5, fixturesPlayed: 4);
        Assert.Equal(6, result.CurrentWeek);
    }

    [Fact]
    public void AdvanceWeek_FixturesPlayedIncreasesBy1()
    {
        var result = FixtureSchedulerService.AdvanceWeek(currentWeek: 1, fixturesPlayed: 10);
        Assert.Equal(11, result.FixturesPlayed);
    }

    [Fact]
    public void AdvanceWeek_MatchesRemainingReducesBy1()
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

    [Fact]
    public void AdvanceWeek_37FixturesPlayed_FixturesPlayedCapsAt38()
    {
        var result = FixtureSchedulerService.AdvanceWeek(currentWeek: 37, fixturesPlayed: 37);
        Assert.Equal(38, result.FixturesPlayed);
        Assert.Equal(0, result.MatchesRemainingThisSeason);
    }

    // ── GetDivisionStartIndex ─────────────────────────────────────────────────

    [Theory]
    [InlineData(Division.One,   1)]
    [InlineData(Division.Two,  21)]
    [InlineData(Division.Three,41)]
    [InlineData(Division.Four, 61)]
    public void GetDivisionStartIndex_ReturnsFirstSlotForDivision(Division division, int expected)
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

    [Fact]
    public void GetCurrentMatch_LastValidWeek_ReturnsFixture()
    {
        // Week 38 is the last league week; the guard is >, so it must not trigger EndOfSeason.
        var fixture = new ScheduledMatch { Week = 38, MatchType = MatchType.League, OpponentName = "Final Opponent" };
        var result  = FixtureSchedulerService.GetCurrentMatch(38, new List<ScheduledMatch> { fixture });

        Assert.Equal(MatchType.League, result.MatchType);
        Assert.Equal("Final Opponent", result.OpponentName);
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

    [Fact]
    public void GenerateSeasonFixtures_AllFixturesAreLeagueType()
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.Two, clubIndex: 3);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.Two, clubName, names);

        Assert.All(fixtures, f => Assert.Equal(MatchType.League, f.MatchType));
    }

    [Fact]
    public void GenerateSeasonFixtures_EachOpponentOnceHomeOnceAway()
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.One, clubIndex: 7);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.One, clubName, names);

        var homeOpponents = fixtures.Where(f => f.IsHomeGame).Select(f => f.OpponentName).ToList();
        var awayOpponents = fixtures.Where(f => !f.IsHomeGame).Select(f => f.OpponentName).ToList();

        // Every away opponent must also appear as a home opponent and vice versa
        Assert.Equal(homeOpponents.OrderBy(x => x), awayOpponents.OrderBy(x => x));
    }

    [Fact]
    public void GenerateSeasonFixtures_ClubNeverListedAsOpponent()
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.Three, clubIndex: 10);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.Three, clubName, names);

        Assert.DoesNotContain(fixtures, f =>
            f.OpponentName.Trim().Equals(clubName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenerateSeasonFixtures_OpponentTeamIndicesWithinDivisionRange()
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.Two, clubIndex: 5);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.Two, clubName, names);

        // Division 2 occupies AllTeamNames indices 21–40
        Assert.All(fixtures, f => Assert.InRange(f.OpponentTeamIndex, 21, 40));
    }

    [Theory]
    [InlineData(0)]   // first slot
    [InlineData(9)]   // mid division
    [InlineData(18)]  // second-to-last slot
    [InlineData(19)]  // pivot slot — the regression case (all home then all away)
    public void GenerateSeasonFixtures_NoConsecutiveSameOpponent(int clubIndex)
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.One, clubIndex);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.One, clubName, names);

        for (int i = 0; i < fixtures.Count - 1; i++)
        {
            Assert.True(
                fixtures[i].OpponentName != fixtures[i + 1].OpponentName,
                $"Same opponent '{fixtures[i].OpponentName}' in consecutive weeks {i + 1} and {i + 2} (clubIndex={clubIndex})");
        }
    }

    [Fact]
    public void GenerateSeasonFixtures_EachOpponentFacedExactlyOncePerHalfSeason()
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.One, clubIndex: 5);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.One, clubName, names);

        var firstHalf  = fixtures.Where(f => f.Week <= 19).Select(f => f.OpponentName).ToList();
        var secondHalf = fixtures.Where(f => f.Week >  19).Select(f => f.OpponentName).ToList();

        Assert.Equal(19, firstHalf.Distinct().Count());
        Assert.Equal(19, secondHalf.Distinct().Count());
        Assert.Equal(firstHalf.OrderBy(x => x), secondHalf.OrderBy(x => x));
    }

    [Fact]
    public void GenerateSeasonFixtures_OpponentNameMatchesTeamIndex()
    {
        var (names, clubName) = MakeTeamNamesWithClub(Division.Two, clubIndex: 4);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(Division.Two, clubName, names);

        Assert.All(fixtures, f => Assert.Equal(names[f.OpponentTeamIndex], f.OpponentName));
    }

    [Theory]
    [InlineData(Division.One)]
    [InlineData(Division.Two)]
    [InlineData(Division.Three)]
    [InlineData(Division.Four)]
    public void GenerateSeasonFixtures_AllDivisions_ProducesCorrectCount(Division division)
    {
        var (names, clubName) = MakeTeamNamesWithClub(division, clubIndex: 5);
        var fixtures = FixtureSchedulerService.GenerateSeasonFixtures(division, clubName, names);

        Assert.Equal(38, fixtures.Count);
        Assert.Equal(19, fixtures.Count(f => f.IsHomeGame));
        Assert.Equal(19, fixtures.Count(f => !f.IsHomeGame));
    }

    // ── FindLeagueRound ───────────────────────────────────────────────────────

    [Fact]
    public void FindLeagueRound_TeamsScheduledInRound0_ReturnsRound0()
    {
        // Round 0: pivot (19) is home vs team 0
        Assert.Equal(0, FixtureSchedulerService.FindLeagueRound(playerIdx: 19, opponentIdx: 0));
    }

    [Fact]
    public void FindLeagueRound_HomeAwayOrderDoesNotAffectResult()
    {
        // Teams 0 and 19 face each other in round 0 (first half) and round 19 (second half).
        // FindLeagueRound is order-independent and returns the earliest matching round.
        Assert.Equal(0, FixtureSchedulerService.FindLeagueRound(playerIdx: 0, opponentIdx: 19));
    }

    [Fact]
    public void FindLeagueRound_OrderIndependent_ReturnsSameRound()
    {
        // FindLeagueRound treats (a,b) and (b,a) as the same fixture pair
        int round1 = FixtureSchedulerService.FindLeagueRound(playerIdx: 5, opponentIdx: 12);
        int round2 = FixtureSchedulerService.FindLeagueRound(playerIdx: 12, opponentIdx: 5);
        Assert.Equal(round1, round2);
    }

    [Fact]
    public void FindLeagueRound_ReturnedRoundSchedulesBothTeams()
    {
        int playerIdx   = 7;
        int opponentIdx = 14;
        int round = FixtureSchedulerService.FindLeagueRound(playerIdx, opponentIdx);

        var pairs = FixtureSchedulerService.GetRoundPairings(round);
        bool found = pairs.Any(p =>
            (p.homeIdx == playerIdx && p.awayIdx == opponentIdx) ||
            (p.homeIdx == opponentIdx && p.awayIdx == playerIdx));

        Assert.True(found, $"Round {round} does not contain the ({playerIdx},{opponentIdx}) pairing");
    }

    [Fact]
    public void FindLeagueRound_AllPairingsCanBeFound()
    {
        // Every ordered pair of distinct team indices must resolve to a valid round
        for (int a = 0; a < 20; a++)
        for (int b = 0; b < 20; b++)
        {
            if (a == b) continue;
            int round = FixtureSchedulerService.FindLeagueRound(a, b);
            Assert.InRange(round, 0, 37);
        }
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
