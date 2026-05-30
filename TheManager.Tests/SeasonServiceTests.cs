using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class SeasonServiceTests
{
    // ── DetermineNewDivision ──────────────────────────────────────────────────

    [Fact]
    public void DetermineNewDivision_Bottom3_Relegates()
    {
        Assert.Equal(Division.Two, SeasonService.DetermineNewDivision(18, Division.One));
        Assert.Equal(Division.Two, SeasonService.DetermineNewDivision(20, Division.One));
    }

    [Fact]
    public void DetermineNewDivision_Top3_Promotes()
    {
        Assert.Equal(Division.One, SeasonService.DetermineNewDivision(1, Division.Two));
        Assert.Equal(Division.One, SeasonService.DetermineNewDivision(3, Division.Two));
    }

    [Fact]
    public void DetermineNewDivision_Mid_NoChange()
    {
        Assert.Equal(Division.Two, SeasonService.DetermineNewDivision(10, Division.Two));
    }

    [Fact]
    public void DetermineNewDivision_NoRelegationFromDivision4()
    {
        Assert.Equal(Division.Four, SeasonService.DetermineNewDivision(20, Division.Four));
    }

    [Fact]
    public void DetermineNewDivision_NoPromotionFromDivision1()
    {
        Assert.Equal(Division.One, SeasonService.DetermineNewDivision(1, Division.One));
    }

    // ── AwardLeaguePrizeMoney ─────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AwardLeaguePrizeMoney_Top3_AddsMoney(int position)
    {
        var finances = new Finances { BankBalance = 0 };
        SeasonService.AwardLeaguePrizeMoney(finances, position, Division.One);
        Assert.True(finances.BankBalance > 0);
    }

    [Fact]
    public void AwardLeaguePrizeMoney_FourthPlace_NoChange()
    {
        var finances = new Finances { BankBalance = 50_000 };
        SeasonService.AwardLeaguePrizeMoney(finances, finalLeaguePosition: 4, Division.One);
        Assert.Equal(50_000, finances.BankBalance);
    }

    [Fact]
    public void AwardLeaguePrizeMoney_FirstPlaceMoreThanThird()
    {
        var first = new Finances { BankBalance = 0 };
        var third = new Finances { BankBalance = 0 };
        SeasonService.AwardLeaguePrizeMoney(first, 1, Division.One);
        SeasonService.AwardLeaguePrizeMoney(third, 3, Division.One);
        Assert.True(first.BankBalance > third.BankBalance);
    }

    // ── AdjustSharePrice ──────────────────────────────────────────────────────

    [Fact]
    public void AdjustSharePrice_ImprovedPosition_IncreasesPrice()
    {
        var finances = new Finances { SharePriceInPence = 100 };
        SeasonService.AdjustSharePrice(finances, currentLeaguePosition: 5, previousLeaguePosition: 10);
        Assert.True(finances.SharePriceInPence > 100);
    }

    [Fact]
    public void AdjustSharePrice_WorsenedPosition_DecreasesPrice()
    {
        var finances = new Finances { SharePriceInPence = 200 };
        SeasonService.AdjustSharePrice(finances, currentLeaguePosition: 15, previousLeaguePosition: 5);
        Assert.True(finances.SharePriceInPence < 200);
    }

    // ── CalculateManagerRating ────────────────────────────────────────────────

    [Fact]
    public void CalculateManagerRating_TopPosition_HighRating()
    {
        int rating = SeasonService.CalculateManagerRating(
            finalLeaguePosition:   1,
            leagueCupRoundReached: 4,
            faCupRoundReached:     4,
            europeanRound:         0,
            squadPlayersRemaining: 16,
            bankBalance:           500_000,
            division:              Division.One);

        Assert.True(rating > 50);
    }

    [Fact]
    public void CalculateManagerRating_BottomPosition_LowerThanTop()
    {
        int topRating = SeasonService.CalculateManagerRating(1, 0, 0, 0, 11, 0, Division.One);
        int botRating = SeasonService.CalculateManagerRating(20, 0, 0, 0, 11, 0, Division.One);
        Assert.True(topRating > botRating);
    }

    // ── ResetMatchState ───────────────────────────────────────────────────────

    [Fact]
    public void ResetMatchState_SetsLeagueCupToRound1()
    {
        var lc = new CupCompetition { CurrentRound = CupRound.Final };
        var fa = new CupCompetition { CurrentRound = CupRound.Final };
        SeasonService.ResetMatchState(lc, fa, european: null, Division.One);
        Assert.Equal(CupRound.Round1, lc.CurrentRound);
        Assert.Equal(CupRound.Round1, fa.CurrentRound);
    }

    [Fact]
    public void ResetMatchState_ResetsRoundTracker_Division1()
    {
        var lc = new CupCompetition();
        var fa = new CupCompetition();
        SeasonService.ResetMatchState(lc, fa, european: null, Division.One);
        // Division <= 2: roundTracker = 3 - 1 = 2
        Assert.Equal(2, lc.RoundTracker);
        Assert.Equal(2, fa.RoundTracker);
    }

    [Fact]
    public void ResetMatchState_ResetsRoundTracker_Division3()
    {
        var lc = new CupCompetition();
        var fa = new CupCompetition();
        SeasonService.ResetMatchState(lc, fa, european: null, Division.Three);
        // Division > 2: roundTracker = 3 - 0 = 3
        Assert.Equal(3, lc.RoundTracker);
    }

    [Fact]
    public void ResetMatchState_ResetsEuropeanRoundState()
    {
        var lc  = new CupCompetition();
        var fa  = new CupCompetition();
        var eur = new EuropeanCompetition { RoundState = 5 };
        SeasonService.ResetMatchState(lc, fa, eur, Division.One);
        Assert.Equal(0, eur.RoundState);
    }

    // ── DetermineNewDivision (exact boundaries) ───────────────────────────────

    [Fact]
    public void DetermineNewDivision_Position4_NoChange()
    {
        // Position 4 is just outside the promotion zone (≤ 3 promotes)
        Assert.Equal(Division.Two, SeasonService.DetermineNewDivision(4, Division.Two));
    }

    [Fact]
    public void DetermineNewDivision_Position17_NoChange()
    {
        // Position 17 is just inside the safe zone (> 17 relegates)
        Assert.Equal(Division.Two, SeasonService.DetermineNewDivision(17, Division.Two));
    }

    // ── RecordSeasonAndRoll ───────────────────────────────────────────────────

    [Fact]
    public void RecordSeasonAndRoll_AddsRecord()
    {
        var history = new List<SeasonRecord>();
        var record  = new SeasonRecord { SeasonNumber = 1, FinalLeaguePosition = 5 };
        SeasonService.RecordSeasonAndRoll(history, record);
        Assert.Single(history);
        Assert.Equal(1, history[0].SeasonNumber);
    }

    [Fact]
    public void RecordSeasonAndRoll_KeepsUpTo10Records()
    {
        var history = Enumerable.Range(1, 10)
            .Select(i => new SeasonRecord { SeasonNumber = i })
            .ToList();
        SeasonService.RecordSeasonAndRoll(history, new SeasonRecord { SeasonNumber = 11 });
        Assert.Equal(10, history.Count);
    }

    [Fact]
    public void RecordSeasonAndRoll_RemovesOldestWhenOver10()
    {
        var history = Enumerable.Range(1, 10)
            .Select(i => new SeasonRecord { SeasonNumber = i })
            .ToList();
        SeasonService.RecordSeasonAndRoll(history, new SeasonRecord { SeasonNumber = 11 });
        // Oldest (SeasonNumber=1) should be gone; newest (11) should be last
        Assert.Equal(2,  history[0].SeasonNumber);
        Assert.Equal(11, history[9].SeasonNumber);
    }

    // ── SwapPromotedRelegatedTeams ────────────────────────────────────────────

    [Fact]
    public void SwapPromotedRelegatedTeams_SwapsBottomOfUpperWithTopOfLower()
    {
        // upperDivisionNumber=1: bottom3 of div1 = indices 18,19,20; top3 of div2 = 21,22,23
        var names = new string[81];
        for (int i = 1; i <= 80; i++) names[i] = $"Orig{i:D2}";

        SeasonService.SwapPromotedRelegatedTeams(names, upperDivisionNumber: 1);

        Assert.Equal("Orig21", names[18]);
        Assert.Equal("Orig22", names[19]);
        Assert.Equal("Orig23", names[20]);
        Assert.Equal("Orig18", names[21]);
        Assert.Equal("Orig19", names[22]);
        Assert.Equal("Orig20", names[23]);
    }

    [Fact]
    public void SwapPromotedRelegatedTeams_LeavesOtherIndicesUnchanged()
    {
        var names = new string[81];
        for (int i = 1; i <= 80; i++) names[i] = $"Orig{i:D2}";

        SeasonService.SwapPromotedRelegatedTeams(names, upperDivisionNumber: 1);

        // Indices outside the swap zone should be unchanged
        Assert.Equal("Orig01", names[1]);
        Assert.Equal("Orig17", names[17]);
        Assert.Equal("Orig24", names[24]);
    }

    // ── AgeYouthPlayers ───────────────────────────────────────────────────────

    [Fact]
    public void AgeYouthPlayers_IncrementsAge()
    {
        var youth  = new YouthPlayer { Age = 17, Position = PlayerPosition.Defender };
        var team   = new List<YouthPlayer> { youth };
        int count  = 1;
        SeasonService.AgeYouthPlayers(team, ref count);
        Assert.Equal(18, team[0].Age);
    }

    [Fact]
    public void AgeYouthPlayers_Removes19YearOlds()
    {
        var youth = new YouthPlayer { Age = 18, Position = PlayerPosition.Defender };
        var team  = new List<YouthPlayer> { youth };
        int count = 1;
        SeasonService.AgeYouthPlayers(team, ref count);
        Assert.Empty(team);
        Assert.Equal(0, count);
    }

    [Fact]
    public void AgeYouthPlayers_NoPositionPlayersNotAged()
    {
        var youth = new YouthPlayer { Age = 17, Position = PlayerPosition.None };
        var team  = new List<YouthPlayer> { youth };
        int count = 1;
        SeasonService.AgeYouthPlayers(team, ref count);
        // No position = not aged, stays in team
        Assert.Single(team);
        Assert.Equal(17, team[0].Age);
    }
}
