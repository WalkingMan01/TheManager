using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class OpponentRatingServiceTests
{
    private static OpponentRatings EstimateCupOpponent(int opponentDivision, int seed, int difficulty = 0)
        => OpponentRatingService.Estimate(
            "Someone FC",
            new LeagueTable(),          // cup opponents are not in our table
            Division.Three,
            difficulty,
            opponentDivision: opponentDivision,
            isCupMatch: true,
            new Random(seed));

    // ── Cup ratings follow the opponent's real division ───────────────────────

    [Fact]
    public void Estimate_CupOpponentFromDivisionOne_RollsTopFlightRatings()
    {
        // Normal difficulty: base = 6 − (1 − 0) = 5, random 0–2 → 5–7.
        for (int seed = 0; seed < 100; seed++)
        {
            var ratings = EstimateCupOpponent(opponentDivision: 1, seed);
            Assert.InRange(ratings.GoalkeeperRating, 5, 7);
            Assert.InRange(ratings.DefenceRating,    5, 7);
            Assert.InRange(ratings.MidRating,        5, 7);
            Assert.InRange(ratings.AttackRating,     5, 7);
        }
    }

    [Fact]
    public void Estimate_NonLeagueCupOpponent_RollsLikeALowerHalfLeagueTwoSide()
    {
        // Non-league (division 5) clamps to division 4: base = 6 − 4 = 2 → 2–4.
        for (int seed = 0; seed < 100; seed++)
        {
            var ratings = EstimateCupOpponent(opponentDivision: 5, seed);
            Assert.InRange(ratings.GoalkeeperRating, 2, 4);
            Assert.InRange(ratings.DefenceRating,    2, 4);
            Assert.InRange(ratings.MidRating,        2, 4);
            Assert.InRange(ratings.AttackRating,     2, 4);
        }
    }

    [Fact]
    public void Estimate_NonLeagueOpponent_MatchesDivisionFourDistribution()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var nonLeague    = EstimateCupOpponent(opponentDivision: 5, seed);
            var divisionFour = EstimateCupOpponent(opponentDivision: 4, seed);

            Assert.Equal(divisionFour.GoalkeeperRating, nonLeague.GoalkeeperRating);
            Assert.Equal(divisionFour.DefenceRating,    nonLeague.DefenceRating);
            Assert.Equal(divisionFour.MidRating,        nonLeague.MidRating);
            Assert.Equal(divisionFour.AttackRating,     nonLeague.AttackRating);
        }
    }

    [Fact]
    public void Estimate_CupOpponentNotInOurTable_GetsNoTopThreeBonus()
    {
        // The fallback league position for unknown cup opponents is mid-table,
        // so even on Normal/Hard the +1 top-3 bonus can never apply — a Div 1
        // cup opponent stays within the 5–7 base band.
        for (int seed = 0; seed < 100; seed++)
        {
            var ratings = EstimateCupOpponent(opponentDivision: 1, seed);
            Assert.True(ratings.AttackRating <= 7);
            Assert.Equal(12, ratings.LeaguePosition);
        }
    }

    [Fact]
    public void Estimate_CupMorale_UsesTheHighCupFormula()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var ratings = EstimateCupOpponent(opponentDivision: 3, seed);
            Assert.InRange(ratings.Morale, 75, 98);
        }
    }
}
