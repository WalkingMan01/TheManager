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
    public void DetermineNewDivision_Top2_PromotesAutomatically()
    {
        // Championship/League One: top 2 promote automatically — the old "top 3"
        // rule no longer applies (3rd is decided by the play-off).
        Assert.Equal(Division.One, SeasonService.DetermineNewDivision(1, Division.Two));
        Assert.Equal(Division.One, SeasonService.DetermineNewDivision(2, Division.Two));
    }

    [Fact]
    public void DetermineNewDivision_PlayoffField_NotPromotedWithoutWinningPlayoff()
    {
        // Positions 3rd-6th no longer promote automatically.
        Assert.Equal(Division.Two, SeasonService.DetermineNewDivision(3, Division.Two));
        Assert.Equal(Division.Two, SeasonService.DetermineNewDivision(6, Division.Two, promotedViaPlayoff: false));
    }

    [Fact]
    public void DetermineNewDivision_PlayoffField_PromotedWhenPlayoffWon()
    {
        Assert.Equal(Division.One, SeasonService.DetermineNewDivision(3, Division.Two, promotedViaPlayoff: true));
        Assert.Equal(Division.One, SeasonService.DetermineNewDivision(6, Division.Two, promotedViaPlayoff: true));
    }

    [Fact]
    public void DetermineNewDivision_OutsidePlayoffField_NeverPromotedEvenIfFlagSet()
    {
        // Position 7 is outside the 4-team play-off field (3rd-6th) — the flag
        // alone cannot promote a team that never got there.
        Assert.Equal(Division.Two, SeasonService.DetermineNewDivision(7, Division.Two, promotedViaPlayoff: true));
    }

    [Fact]
    public void DetermineNewDivision_LeagueTwo_Top3PromotesAutomatically()
    {
        // League Two promotes 3 automatically (autoSpots == 3), not 2.
        Assert.Equal(Division.Three, SeasonService.DetermineNewDivision(1, Division.Four));
        Assert.Equal(Division.Three, SeasonService.DetermineNewDivision(3, Division.Four));
    }

    [Fact]
    public void DetermineNewDivision_LeagueTwo_PlayoffFieldIs4To7()
    {
        Assert.Equal(Division.Four, SeasonService.DetermineNewDivision(4, Division.Four)); // not won
        Assert.Equal(Division.Three, SeasonService.DetermineNewDivision(4, Division.Four, promotedViaPlayoff: true));
        Assert.Equal(Division.Three, SeasonService.DetermineNewDivision(7, Division.Four, promotedViaPlayoff: true));
        Assert.Equal(Division.Four, SeasonService.DetermineNewDivision(8, Division.Four, promotedViaPlayoff: true)); // outside field
    }

    [Fact]
    public void DetermineNewDivision_LeagueOne_RelegatesBottom4()
    {
        // Division Three (League One) has 24 teams; relegation is bottom 4 (21-24),
        // not the usual bottom 3.
        Assert.Equal(Division.Four, SeasonService.DetermineNewDivision(21, Division.Three));
        Assert.Equal(Division.Four, SeasonService.DetermineNewDivision(24, Division.Three));
        Assert.Equal(Division.Three, SeasonService.DetermineNewDivision(20, Division.Three)); // safe
    }

    [Fact]
    public void DetermineNewDivision_Mid_NoChange()
    {
        Assert.Equal(Division.Two, SeasonService.DetermineNewDivision(10, Division.Two));
    }

    [Fact]
    public void DetermineNewDivision_NoRelegationFromDivision4()
    {
        // Position 24 (bottom of 24-team Div4) still can't relegate further
        Assert.Equal(Division.Four, SeasonService.DetermineNewDivision(24, Division.Four));
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
        // Div 2 has 24 teams; relegation threshold is position > 21. Position 17 is safe.
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
        var names = new string[120];
        for (int i = 1; i <= 92; i++) names[i] = $"Orig{i:D2}";

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
        var names = new string[120];
        for (int i = 1; i <= 92; i++) names[i] = $"Orig{i:D2}";

        SeasonService.SwapPromotedRelegatedTeams(names, upperDivisionNumber: 1);

        // Indices outside the swap zone should be unchanged
        Assert.Equal("Orig01", names[1]);
        Assert.Equal("Orig17", names[17]);
        Assert.Equal("Orig24", names[24]);
    }

    [Fact]
    public void SwapPromotedRelegatedTeams_LeagueOneLeagueTwoBoundary_Swaps4NotFlat3()
    {
        // upperDivisionNumber=3 (League One/League Two boundary): bottom 4 of
        // Div3 (65-68) swap with top 4 of Div4 (69-72) — RelegationSpots(Three) == 4.
        var names = new string[120];
        for (int i = 1; i <= 92; i++) names[i] = $"Orig{i:D2}";

        SeasonService.SwapPromotedRelegatedTeams(names, upperDivisionNumber: 3);

        Assert.Equal("Orig69", names[65]);
        Assert.Equal("Orig70", names[66]);
        Assert.Equal("Orig71", names[67]);
        Assert.Equal("Orig72", names[68]);
        Assert.Equal("Orig65", names[69]);
        Assert.Equal("Orig66", names[70]);
        Assert.Equal("Orig67", names[71]);
        Assert.Equal("Orig68", names[72]);

        // Untouched neighbours.
        Assert.Equal("Orig64", names[64]);
        Assert.Equal("Orig73", names[73]);
    }

    // ── PromoteAndRelegateActualTeams ─────────────────────────────────────────

    private static string[] MakeAllTeamNames()
    {
        var names = new string[120];
        for (int i = 1; i <= 92; i++) names[i] = $"Orig{i:D2}";
        return names;
    }

    private static LeagueTable MakeTable(Division division, params string[] teamNamesInOrder)
    {
        var table = new LeagueTable { Division = division };
        foreach (var name in teamNamesInOrder)
            table.Entries.Add(new LeagueEntry { TeamName = name });
        return table;
    }

    [Fact]
    public void PromoteAndRelegateActualTeams_Division2_MovesTop2AutoPlusPlayoffWinnerUpAndBottom3Down()
    {
        var names = MakeAllTeamNames();

        // Division 2 occupies indices 21-44 (24 teams). Top 2 (auto) = Orig30,
        // Orig25 (out of order to prove standings, not array position, drive the
        // swap). 3rd place Orig22 finished top of the play-off field but the
        // simulated winner is Orig35 (a lower seed) — the play-off winner, not
        // table position, decides the last promoted slot.
        // Bottom 3 (relegated, unaffected by the play-off) = Orig12, Orig21, Orig40.
        var entries = new List<string> { "Orig30", "Orig25", "Orig22" };
        entries.AddRange(Enumerable.Range(1, 18).Select(_ => "Filler"));
        entries.Add("Orig12");
        entries.Add("Orig21");
        entries.Add("Orig40");

        var table = MakeTable(Division.Two, entries.ToArray());

        SeasonService.PromoteAndRelegateActualTeams(names, table, playoffWinner: "Orig35");

        // Auto-promoted (top 2) + play-off winner now sit in the bottom 3 slots of division 1 (18-20).
        Assert.Contains("Orig30", new[] { names[18], names[19], names[20] });
        Assert.Contains("Orig25", new[] { names[18], names[19], names[20] });
        Assert.Contains("Orig35", new[] { names[18], names[19], names[20] });
        // 3rd place did NOT go up automatically — it lost the play-off.
        Assert.DoesNotContain("Orig22", new[] { names[18], names[19], names[20] });

        // Bottom 3 of division 2 now sit in the top 3 slots of division 3 (45-47).
        Assert.Contains("Orig12", new[] { names[45], names[46], names[47] });
        Assert.Contains("Orig21", new[] { names[45], names[46], names[47] });
        Assert.Contains("Orig40", new[] { names[45], names[46], names[47] });
    }

    [Fact]
    public void PromoteAndRelegateActualTeams_Division1_OnlyRelegatesNoPromotion()
    {
        var names = MakeAllTeamNames();

        var entries = new List<string>();
        entries.AddRange(Enumerable.Range(1, 17).Select(_ => "Filler"));
        entries.Add("Orig18");
        entries.Add("Orig19");
        entries.Add("Orig20");

        var table = MakeTable(Division.One, entries.ToArray());

        SeasonService.PromoteAndRelegateActualTeams(names, table, playoffWinner: "");

        // Bottom 3 of division 1 now sit in the top 3 slots of division 2 (21-23).
        Assert.Contains("Orig18", new[] { names[21], names[22], names[23] });
        Assert.Contains("Orig19", new[] { names[21], names[22], names[23] });
        Assert.Contains("Orig20", new[] { names[21], names[22], names[23] });
    }

    [Fact]
    public void PromoteAndRelegateActualTeams_Division4_AutoPromotesTop3PlusPlayoffWinner_NoRelegation()
    {
        var names = MakeAllTeamNames();

        // Division 4 (League Two) auto-promotes 3, not 2. Use arbitrary labels.
        var entries = new List<string> { "Orig61", "Orig62", "Orig63" };
        entries.AddRange(Enumerable.Range(1, 21).Select(_ => "Filler"));

        var table = MakeTable(Division.Four, entries.ToArray());

        SeasonService.PromoteAndRelegateActualTeams(names, table, playoffWinner: "Orig70");

        // Top 3 (auto) of division 4 sit in slots 65-67 of division 3; the
        // play-off winner (Orig70, from further down the table) fills the last
        // promoted slot, 68.
        Assert.Contains("Orig61", new[] { names[65], names[66], names[67] });
        Assert.Contains("Orig62", new[] { names[65], names[66], names[67] });
        Assert.Contains("Orig63", new[] { names[65], names[66], names[67] });
        Assert.Equal("Orig70", names[68]);
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
