using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.Tests;

/// <summary>
/// End-to-end coverage of the promotion play-off (docs/specs/promotion-playoffs.md):
/// automatic promotion/relegation still short-circuits the play-off when the
/// player isn't involved, a play-off run schedules leg 1/leg 2/final in turn, and
/// the season always wraps up cleanly afterwards regardless of which branch fires.
/// </summary>
public class GameServicePlayoffIntegrationTests
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

    /// <summary>
    /// Fast-forwards straight to the "end of season" matchday with a hand-built,
    /// already-sorted final table that places our club at exactly
    /// <paramref name="ourPosition"/> (strictly decreasing win counts so
    /// LeagueService.Sort cannot reorder it).
    /// </summary>
    private static GameService MakeGameAtEndOfSeason(Division division, int ourPosition, Random? rng = null)
    {
        var game  = MakeGame(division, rng);
        var state = game.State;
        int teamCount      = Constants.TeamCount(division);
        var (divStart, _)  = Constants.DivisionRange(division);
        string ourName     = state.Club.Name.Trim();

        var otherNames = new List<string>();
        for (int i = 0; i < teamCount; i++)
        {
            string name = state.AllTeamNames[divStart + i].Trim();
            if (!name.Equals(ourName, StringComparison.OrdinalIgnoreCase))
                otherNames.Add(name);
        }

        otherNames.Insert(ourPosition - 1, ourName);

        var table = new LeagueTable { Division = division };
        int wins  = teamCount;
        foreach (var name in otherNames)
        {
            table.Entries.Add(new LeagueEntry
            {
                TeamName     = name,
                Played       = teamCount,
                Won          = wins,
                Drawn        = 0,
                GoalsFor     = wins,
                GoalsAgainst = 0
            });
            wins--;
        }

        state.CurrentLeague = table;
        state.CurrentWeek   = Constants.SeasonMatchdays + 1;
        return game;
    }

    // ── Player not involved in the play-off ───────────────────────────────────

    [Fact]
    public void EndOfSeason_TopTwoInChampionship_PromotedImmediatelyNoPlayoff()
    {
        var game = MakeGameAtEndOfSeason(Division.Two, ourPosition: 1);

        var result = game.PlayMatch();

        Assert.True(result.WasEndOfSeason);
        Assert.Equal(Division.One, game.State.Club.Division);
        Assert.False(game.State.Playoff.Active);
    }

    [Fact]
    public void EndOfSeason_MidTableChampionship_NoPromotionNoPlayoff()
    {
        var game = MakeGameAtEndOfSeason(Division.Two, ourPosition: 10);

        var result = game.PlayMatch();

        Assert.True(result.WasEndOfSeason);
        Assert.Equal(Division.Two, game.State.Club.Division);
    }

    [Fact]
    public void EndOfSeason_LeagueTwoTopThree_PromotedAutomaticallyNoPlayoff()
    {
        // League Two auto-promotes 3, not 2 — 3rd place goes up without a play-off.
        var game = MakeGameAtEndOfSeason(Division.Four, ourPosition: 3);

        var result = game.PlayMatch();

        Assert.True(result.WasEndOfSeason);
        Assert.Equal(Division.Three, game.State.Club.Division);
    }

    [Fact]
    public void EndOfSeason_LeagueOneBottomFour_RelegatedImmediately()
    {
        // League One relegates 4, not 3.
        var game = MakeGameAtEndOfSeason(Division.Three, ourPosition: 21);

        var result = game.PlayMatch();

        Assert.True(result.WasEndOfSeason);
        Assert.Equal(Division.Four, game.State.Club.Division);
    }

    // ── Player involved in the play-off ───────────────────────────────────────

    [Fact]
    public void EndOfSeason_ThirdPlaceChampionship_SchedulesLeg1AgainstSixthPlace_AwayAsHigherSeed()
    {
        var game = MakeGameAtEndOfSeason(Division.Two, ourPosition: 3);

        var result = game.PlayMatch();

        // The season is not over yet — leg 1 was scheduled and played instead.
        Assert.False(result.WasEndOfSeason);
        Assert.Equal(Division.Two, game.State.Club.Division);
        Assert.True(game.State.Playoff.Active);
        Assert.True(game.State.Playoff.PlayerInvolved);
        Assert.True(game.State.Playoff.PlayerIsHigherSeed); // 3rd is the higher seed

        var leg1 = game.State.Fixtures.Single(f => f.Week == Constants.PlayoffSemiFinalFirstLegMatchday);
        Assert.Equal(MatchType.Playoff, leg1.MatchType);
        Assert.False(leg1.IsHomeGame); // lower seed (6th) hosts leg 1 — we're away
        Assert.Equal(game.State.CurrentLeague.Entries[5].TeamName.Trim(), leg1.OpponentName.Trim());
    }

    [Fact]
    public void EndOfSeason_FourthPlaceChampionship_IsLowerSeedInItsTie_HomeInLeg1()
    {
        var game = MakeGameAtEndOfSeason(Division.Two, ourPosition: 5); // paired with 4th, we're the lower seed

        game.PlayMatch();

        Assert.False(game.State.Playoff.PlayerIsHigherSeed);
        var leg1 = game.State.Fixtures.Single(f => f.Week == Constants.PlayoffSemiFinalFirstLegMatchday);
        Assert.True(leg1.IsHomeGame); // lower seed hosts leg 1
    }

    [Fact]
    public void EndOfSeason_LeagueTwoFourthPlace_PlayoffFieldStartsAtFourth()
    {
        var game = MakeGameAtEndOfSeason(Division.Four, ourPosition: 4);

        var result = game.PlayMatch();

        Assert.False(result.WasEndOfSeason);
        Assert.True(game.State.Playoff.PlayerIsHigherSeed);
        var leg1 = game.State.Fixtures.Single(f => f.Week == Constants.PlayoffSemiFinalFirstLegMatchday);
        // 4th vs 7th in League Two's shape (not 3rd vs 6th).
        Assert.Equal(game.State.CurrentLeague.Entries[6].TeamName.Trim(), leg1.OpponentName.Trim());
    }

    [Fact]
    public void EndOfSeason_QualifyingForPlayoff_NeverReturnsABlankMatchWithNoOpponent()
    {
        // Regression: PlayMatch() used to return an empty MatchResult (no
        // opponent, MatchType.League, 0-0) for the call that merely scheduled
        // leg 1, since RunEndOfSeason returning false short-circuited straight to
        // "new MatchResult { WasEndOfSeason = false }" with nothing else set.
        var game = MakeGameAtEndOfSeason(Division.Two, ourPosition: 3);

        var result = game.PlayMatch();

        Assert.False(result.WasEndOfSeason);
        Assert.Equal(MatchType.Playoff, result.MatchType);
        Assert.False(string.IsNullOrWhiteSpace(result.OpponentName));
        Assert.False(string.IsNullOrWhiteSpace(result.OurClubName));
    }

    // ── Full play-off progression, leg by leg ─────────────────────────────────

    [Fact]
    public void Playoff_FullRun_EventuallyWrapsUpTheSeasonAndResetsPlayoffState()
    {
        var game = MakeGameAtEndOfSeason(Division.Two, ourPosition: 3, new Random(7));

        // Kicks off the play-off (leg 1 scheduled and played in the same call).
        var setup = game.PlayMatch();
        Assert.False(setup.WasEndOfSeason);

        bool seasonEnded = false;
        for (int i = 0; i < 5 && !seasonEnded; i++)
        {
            var result = game.PlayMatch();
            if (result.WasEndOfSeason) seasonEnded = true;
        }

        Assert.True(seasonEnded, "play-off did not resolve within a bounded number of matchdays");

        // A fresh season: calendar rebuilt, matchday reset, play-off state cleared.
        Assert.Equal(1, game.State.CurrentWeek);
        Assert.Equal(Constants.SeasonMatchdays, game.State.Fixtures.Count);
        Assert.False(game.State.Playoff.Active);
        Assert.Equal(string.Empty, game.State.Playoff.Winner);
    }

    [Fact]
    public void Playoff_Leg1_RecordsScoreAndSchedulesLeg2AtOppositeVenue()
    {
        var game = MakeGameAtEndOfSeason(Division.Two, ourPosition: 4, new Random(3));

        // A single PlayMatch() call both schedules and plays leg 1 — the
        // end-of-season matchday that only sets up the fixture never surfaces as
        // a playable "match" on its own (see the WasEndOfSeason handling above).
        var leg1Result = game.PlayMatch();
        bool wasHigherSeed = game.State.Playoff.PlayerIsHigherSeed;

        Assert.Equal(MatchType.Playoff, leg1Result.MatchType);
        Assert.NotNull(game.State.Playoff.FirstLegOurScore);
        Assert.NotNull(game.State.Playoff.FirstLegTheirScore);
        Assert.Equal(leg1Result.OurScore,   game.State.Playoff.FirstLegOurScore);
        Assert.Equal(leg1Result.TheirScore, game.State.Playoff.FirstLegTheirScore);

        var leg2 = game.State.Fixtures.Single(f => f.Week == Constants.PlayoffSemiFinalSecondLegMatchday);
        Assert.Equal(wasHigherSeed, leg2.IsHomeGame); // venue flips: higher seed now hosts
    }

    [Fact]
    public void Playoff_Leg1_NeverGoesToPenaltiesEvenIfDrawn()
    {
        // Seed sweep for a leg-1 draw; a single leg is never decisive.
        for (int seed = 0; seed < 100; seed++)
        {
            var game = MakeGameAtEndOfSeason(Division.Two, ourPosition: 3, new Random(seed));
            var leg1Result = game.PlayMatch(); // schedules and plays leg 1

            if (leg1Result.OurScore != leg1Result.TheirScore) continue;

            Assert.Null(leg1Result.Shootout);
            return;
        }

        Assert.Fail("no drawn leg 1 in 100 seeds");
    }

    [Fact]
    public void Playoff_Final_IsAtWembleyWithFixedGate()
    {
        // Seed sweep until our club wins its way through to the final.
        for (int seed = 0; seed < 200; seed++)
        {
            var game = MakeGameAtEndOfSeason(Division.Two, ourPosition: 3, new Random(seed));
            game.PlayMatch(); // schedules and plays leg 1, and schedules leg 2

            bool stillIn = game.State.Fixtures.Any(f => f.Week == Constants.PlayoffSemiFinalSecondLegMatchday);
            if (!stillIn) continue;

            game.PlayMatch(); // plays leg 2 — may schedule the final, or end the season

            bool reachedFinal = game.State.Fixtures.Any(f => f.Week == Constants.PlayoffFinalMatchday);
            if (!reachedFinal) continue;

            double ticketPrice = game.State.Club.TicketPriceInPounds;
            var finalResult = game.PlayMatch();

            Assert.Equal(MatchType.Playoff, finalResult.MatchType);
            Assert.True(finalResult.IsNeutralVenue);
            Assert.Equal(100_000, game.State.Finances.LastMatchAttendance);
            Assert.Equal(100_000 * ticketPrice / 2, game.State.Finances.LastMatchGateMoney);
            Assert.True(finalResult.WasEndOfSeason);
            return;
        }

        Assert.Fail("our club never reached the play-off final in 200 seeds");
    }
}
