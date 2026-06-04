using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class FinancialCrisisServiceTests
{
    // ── CalculateFormPoints ───────────────────────────────────────────────────

    [Fact]
    public void CalculateFormPoints_Win_Scores3Points()
    {
        // result[0] = "12": opponent=1, us=2 → win
        var results = new string[38];
        results[0] = "12";
        Assert.Equal(3, FinancialCrisisService.CalculateFormPoints(results, fixturesPlayed: 1));
    }

    [Fact]
    public void CalculateFormPoints_Draw_Scores1Point()
    {
        var results = new string[38];
        results[0] = "11";
        Assert.Equal(1, FinancialCrisisService.CalculateFormPoints(results, fixturesPlayed: 1));
    }

    [Fact]
    public void CalculateFormPoints_Loss_Scores0Points()
    {
        var results = new string[38];
        results[0] = "21";   // opponent=2, us=1 → loss
        Assert.Equal(0, FinancialCrisisService.CalculateFormPoints(results, fixturesPlayed: 1));
    }

    [Fact]
    public void CalculateFormPoints_UsesOnly_Last15Results()
    {
        // fixturesPlayed=24 → window covers indices 9..23 (15 entries)
        // Put wins at 0..8 (outside window) and losses at 9..23 (inside window)
        var results = new string[38];
        for (int i = 0; i < 9;  i++) results[i] = "12";   // wins — outside window
        for (int i = 9; i < 24; i++) results[i] = "20";   // losses — inside window
        Assert.Equal(0, FinancialCrisisService.CalculateFormPoints(results, fixturesPlayed: 24));
    }

    [Fact]
    public void CalculateFormPoints_EmptyResults_Returns0()
    {
        Assert.Equal(0, FinancialCrisisService.CalculateFormPoints(new string[38], fixturesPlayed: 0));
    }

    // ── Evaluate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_PositiveBalance_ReturnsNoAction()
    {
        var (finances, club, squad) = MakeHealthySetup();
        finances.BankBalance = 10_000;

        var result = FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 0, weeklyResults: new string[38], leaguePosition: 10, new Random(0));

        Assert.Equal(CrisisOutcome.NoAction, result.Outcome);
    }

    [Fact]
    public void Evaluate_SharesSoldDanger_ReturnsSackedSharesSold()
    {
        var (finances, club, squad) = MakeHealthySetup();
        finances.SharesSoldDanger = true;

        var result = FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 0, weeklyResults: new string[38], leaguePosition: 10, new Random(0));

        Assert.Equal(CrisisOutcome.SackedSharesSold, result.Outcome);
    }

    [Fact]
    public void Evaluate_SharesSoldDanger_DoesNotSackManager()
    {
        var (finances, club, squad) = MakeHealthySetup();
        finances.SharesSoldDanger = true;

        var result = FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 0, weeklyResults: new string[38], leaguePosition: 10, new Random(0));

        Assert.False(result.ManagerSacked);
    }

    [Fact]
    public void Evaluate_SharesSoldDanger_ZerosManagerContract()
    {
        var (finances, club, squad) = MakeHealthySetup();
        finances.SharesSoldDanger       = true;
        club.ManagerContractWeeks       = 52;

        FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 0, weeklyResults: new string[38], leaguePosition: 10, new Random(0));

        Assert.Equal(0, club.ManagerContractWeeks);
    }

    [Fact]
    public void Evaluate_GoodFormPositiveBalance_ReturnsNoAction()
    {
        var (finances, club, squad) = MakeHealthySetup();
        finances.BankBalance = 50_000;

        // 20 fixtures all wins → well above 6 form points
        var results = new string[38];
        for (int i = 0; i < 20; i++) results[i] = "12";

        var result = FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 20, weeklyResults: results, leaguePosition: 10, new Random(0));

        Assert.Equal(CrisisOutcome.NoAction, result.Outcome);
    }

    [Fact]
    public void Evaluate_PoorForm_PositionNotBottomThree_ReturnsNoAction()
    {
        // Poor form but safe league position (17th) → no notice
        var (finances, club, squad) = MakeHealthySetup();
        finances.BankBalance = 50_000;

        var results = AllLossResults(fixturesPlayed: 20);
        var result  = FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 20, weeklyResults: results, leaguePosition: 17, new Random(0));

        Assert.Equal(CrisisOutcome.NoAction, result.Outcome);
    }

    [Fact]
    public void Evaluate_NegativeBalance_TriggersRescue()
    {
        var (finances, club, squad) = MakeHealthySetup();
        finances.BankBalance      = -50_000;
        finances.OverdraftAvailable = 100_000;

        var result = FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 0, weeklyResults: new string[38], leaguePosition: 10, new Random(0));

        Assert.Equal(CrisisOutcome.Rescued, result.Outcome);
    }

    [Fact]
    public void Evaluate_Rescued_BalanceIsNonNegativeAfter()
    {
        var (finances, club, squad) = MakeHealthySetup();
        finances.BankBalance        = -50_000;
        finances.OverdraftAvailable = 100_000;

        FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 0, weeklyResults: new string[38], leaguePosition: 10, new Random(0));

        Assert.True(finances.BankBalance >= 0);
    }

    [Fact]
    public void Evaluate_PoorForm_OnNotice_WhenVoteRoll1()
    {
        // Find a seed where 1+rng.Next(2) == 1 (roll = 1 → OnNotice)
        int seed = FindSeedForRoll(targetValue: 0, maxValue: 2);
        var (finances, club, squad) = MakeHealthySetup();
        finances.BankBalance = 50_000;

        var results = AllLossResults(fixturesPlayed: 20);
        var result  = FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 20, weeklyResults: results, leaguePosition: 18, new Random(seed));

        Assert.Equal(CrisisOutcome.OnNotice, result.Outcome);
        Assert.True(result.ManagerSacked);
        Assert.Equal(0, club.ManagerContractWeeks);
    }

    [Fact]
    public void Evaluate_PoorForm_AnotherChance_WhenVoteRoll2()
    {
        // Find a seed where 1+rng.Next(2) == 2 (roll = 2 → AnotherChance)
        int seed = FindSeedForRoll(targetValue: 1, maxValue: 2);
        var (finances, club, squad) = MakeHealthySetup();
        finances.BankBalance = 50_000;

        var results = AllLossResults(fixturesPlayed: 20);
        var result  = FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 20, weeklyResults: results, leaguePosition: 18, new Random(seed));

        Assert.Equal(CrisisOutcome.AnotherChance, result.Outcome);
        Assert.False(result.ManagerSacked);
        Assert.Equal(52, club.ManagerContractWeeks);   // contract unchanged
    }

    [Fact]
    public void Evaluate_Sacked_WhenBalanceIrrecoverable()
    {
        // No overdraft, mortgage already taken, ≤1 share, no listed players → Sacked
        var (finances, club, squad) = MakeHealthySetup();
        finances.BankBalance        = -10_000;
        finances.OverdraftAvailable = 0;
        finances.MortgageOutstanding = 50_000;  // already has a mortgage
        finances.SharesOwned        = 1;         // can't sell (need ≥ 2)

        var result = FinancialCrisisService.Evaluate(
            finances, club, squad, fixturesPlayed: 0, weeklyResults: new string[38], leaguePosition: 10, new Random(0));

        Assert.Equal(CrisisOutcome.Sacked, result.Outcome);
        Assert.False(result.ManagerSacked);   // financial crisis does not trigger sacking flow
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (Finances finances, Club club, Player?[] squad) MakeHealthySetup()
    {
        var finances = new Finances { BankBalance = 100_000 };
        var club     = new Club { Division = Division.Two, ManagerContractWeeks = 52 };
        var squad    = new Player?[29];
        return (finances, club, squad);
    }

    /// <summary>Builds a results array with all losses in the last <paramref name="fixturesPlayed"/> slots.</summary>
    private static string[] AllLossResults(int fixturesPlayed)
    {
        var results = new string[38];
        for (int i = 0; i < fixturesPlayed; i++) results[i] = "20";   // opponent=2, us=0 → loss
        return results;
    }

    /// <summary>Finds a seed where rng.Next(maxValue) returns targetValue on the first call.</summary>
    private static int FindSeedForRoll(int targetValue, int maxValue)
    {
        for (int seed = 0; seed < 100_000; seed++)
            if (new Random(seed).Next(maxValue) == targetValue)
                return seed;
        throw new InvalidOperationException($"No seed found for roll {targetValue}/{maxValue}");
    }
}
