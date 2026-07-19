using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.Tests;

/// <summary>
/// End-to-end season playthroughs exercising the FA Cup calendar: cup rounds
/// fire on their fixed matchdays, rest days tick, shootouts resolve drawn ties,
/// and the season rolls over cleanly into a fresh competition.
/// </summary>
public class GameServiceCupIntegrationTests
{
    private static GameService MakeGame(Division division, Random? rng = null)
    {
        string team = TeamData.GetDivisionTeams(division)[0];
        var game = new GameService(rng)
        {
            Team     = team,
            Division = division,
            Manager  = "TEST"
        };
        game.StartGame();
        return game;
    }

    private static int PlaySeason(GameService game, int maxMatchdays = 80)
    {
        for (int i = 0; i < maxMatchdays; i++)
        {
            game.PrepareCupMatchday();
            var result = game.PlayMatch();
            if (result.WasEndOfSeason) return i;
        }
        Assert.Fail("season never reached end-of-season processing");
        return -1;
    }

    [Fact]
    public void FullSeason_DivisionThree_CupRunsAllEightRoundsAndSeasonEnds()
    {
        var game = MakeGame(Division.Three);

        Assert.Equal(Constants.SeasonMatchdays, game.State.Fixtures.Count);

        int matchdaysPlayed = PlaySeason(game);

        // 54 matchdays + 1 end-of-season call.
        Assert.Equal(Constants.SeasonMatchdays, matchdaysPlayed);

        // A completed season leaves a fresh calendar and a redrawn round 1.
        Assert.Equal(1, game.State.CurrentWeek);
        Assert.Equal(Constants.SeasonMatchdays, game.State.Fixtures.Count);
        Assert.NotEmpty(game.State.FACup.CurrentRoundFixtures);
        Assert.Empty(game.State.FACup.RoundHistory);
    }

    [Fact]
    public void FullSeason_EveryCupRoundIsRecordedWithTheRightFieldSize()
    {
        var game = MakeGame(Division.Four);

        // Capture the history just before the season wraps (it is cleared on rollover).
        List<CupRoundRecord>? history = null;
        for (int i = 0; i < 80; i++)
        {
            game.PrepareCupMatchday();
            var scheduled = FixtureSchedulerService.GetCurrentMatch(game.State.CurrentWeek, game.State.Fixtures);
            if (scheduled.MatchType == MatchType.EndOfSeason)
                history = game.State.FACup.RoundHistory.ToList();

            var result = game.PlayMatch();
            if (result.WasEndOfSeason) break;
        }

        Assert.NotNull(history);

        // The manager may be sacked mid-season and switch divisions, but the cup
        // itself always completes all eight rounds.
        Assert.Equal(8, history!.Count);
        int[] expectedTies = [40, 20, 32, 16, 8, 4, 2, 1];
        for (int i = 0; i < 8; i++)
        {
            Assert.Equal(CupService.RoundForIndex(i), history[i].Round);
            Assert.Equal(expectedTies[i], history[i].Results.Count);
            Assert.All(history[i].Results, tie => Assert.False(string.IsNullOrEmpty(tie.Winner)));
        }
    }

    [Fact]
    public void FullSeason_DivisionOne_RestDaysAndCupDaysDoNotAdvanceFixturesPlayed()
    {
        var game = MakeGame(Division.One);

        for (int i = 0; i < 80; i++)
        {
            game.PrepareCupMatchday();
            var result = game.PlayMatch();
            if (result.WasEndOfSeason) break;

            // The league counter can never exceed the division's fixture count
            // (a mid-season sacking can move the club into a 46-game division).
            Assert.InRange(game.State.FixturesPlayed, 0,
                Constants.FixturesInSeason(game.State.Club.Division));
        }
    }

    [Fact]
    public void TwoSeasons_RunBackToBackWithoutError()
    {
        var game = MakeGame(Division.Two);

        PlaySeason(game);
        PlaySeason(game);

        Assert.Equal(1, game.State.CurrentWeek);
        Assert.Equal(Constants.SeasonMatchdays, game.State.Fixtures.Count);
    }

    // ── Finance report wiring ─────────────────────────────────────────────────

    [Fact]
    public void PlayMatch_LeagueFixture_PopulatesFinanceReport()
    {
        var game   = MakeGame(Division.Four);
        var result = game.PlayMatch();

        Assert.NotNull(result.FinanceReport);
        Assert.True(result.FinanceReport!.PlayerWageBill > 0);
    }

    [Fact]
    public void PlayMatch_RestDay_PopulatesFinanceReport()
    {
        var game = MakeGame(Division.One);
        var rest = game.State.Fixtures.First(f => f.MatchType == MatchType.NoFixture);
        game.State.CurrentWeek = rest.Week;

        var result = game.PlayMatch();

        Assert.True(result.WasRestDay);
        Assert.NotNull(result.FinanceReport);
        Assert.True(result.FinanceReport!.PlayerWageBill > 0);
    }

    // ── Wembley & shootout write-back ─────────────────────────────────────────

    [Fact]
    public void SemiFinal_CreditsHalfOfAFixedWembleyGate()
    {
        var (game, scheduled) = MakeSemiFinalMatchday();
        double ticketPrice    = game.State.Club.TicketPriceInPounds;

        var result = game.PlayMatch();

        Assert.Equal(MatchType.FACup, scheduled.MatchType);
        Assert.True(result.IsNeutralVenue);
        Assert.Equal(80_000, game.State.Finances.LastMatchAttendance);
        Assert.Equal(80_000 * ticketPrice / 2, game.State.Finances.LastMatchGateMoney);
    }

    [Fact]
    public void DrawnCupTie_WritesShootoutResultBackToTheCalendar()
    {
        // Deterministic seed sweep (same pattern as ScoutReportServiceTests):
        // walk fresh seeded semi-finals until one is drawn after 90 minutes.
        // The first drawn seed is fixed forever, so the test never flakes.
        for (int seed = 0; seed < 100; seed++)
        {
            var (game, scheduled) = MakeSemiFinalMatchday(new Random(seed));
            var result = game.PlayMatch();

            if (result.Shootout == null) continue;

            Assert.True(scheduled.WonOnPenalties);
            Assert.Equal(result.Shootout.OurScore,   scheduled.OurPenalties);
            Assert.Equal(result.Shootout.TheirScore, scheduled.TheirPenalties);
            Assert.Equal(result.OurScore, scheduled.OurScore);

            var playerTie = game.State.FACup.RoundHistory[^1].Results
                .Single(t => t.WonOnPenalties);
            Assert.Equal(result.Shootout.WeWon ? game.State.Club.Name : result.OpponentName,
                playerTie.Winner);
            return;
        }

        Assert.Fail("no drawn cup tie in 100 semi-finals");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Puts a fresh Division One game directly on the semi-final matchday with a
    /// hand-built four-team bracket, our club drawn against the second Division
    /// One team and the other semi between teams three and four.
    /// </summary>
    private static (GameService Game, ScheduledMatch Scheduled) MakeSemiFinalMatchday(Random? rng = null)
    {
        var game  = MakeGame(Division.One, rng);
        var state = game.State;

        state.CurrentWeek = Constants.FACupMatchdays[6];   // semi-final matchday

        int ourIndex = Array.FindIndex(state.AllTeamNames,
            t => t?.Trim().Equals(state.Club.Name.Trim(), StringComparison.OrdinalIgnoreCase) == true);
        Assert.InRange(ourIndex, 1, 20);
        int[] others = Enumerable.Range(1, 20).Where(i => i != ourIndex).Take(3).ToArray();

        var cup = state.FACup;
        cup.RoundHistory.Clear();
        for (int i = 0; i < 6; i++)
            cup.RoundHistory.Add(new CupRoundRecord { Round = CupService.RoundForIndex(i) });

        Array.Clear(cup.Bracket, 0, cup.Bracket.Length);
        cup.Bracket[1] = ourIndex;
        cup.Bracket[2] = others[0];
        cup.Bracket[3] = others[1];
        cup.Bracket[4] = others[2];

        CupFixture Tie(int home, int away) => new()
        {
            HomeTeam      = state.AllTeamNames[home],
            AwayTeam      = state.AllTeamNames[away],
            HomeTeamIndex = home,
            AwayTeamIndex = away,
            HomeDivision  = Division.One,
            AwayDivision  = Division.One
        };
        cup.CurrentRoundFixtures = [Tie(ourIndex, others[0]), Tie(others[1], others[2])];

        var scheduled = FixtureSchedulerService.GetCurrentMatch(state.CurrentWeek, state.Fixtures);
        Assert.Equal(MatchType.FACup, scheduled.MatchType);
        return (game, scheduled);
    }
}
