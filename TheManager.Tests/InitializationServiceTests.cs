using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class InitializationServiceTests
{
    // ── CalculateWage ─────────────────────────────────────────────────────────

    [Fact]
    public void CalculateWage_ReturnsAtLeastTheScaledFloor()
    {
        var rng = new Random(0);
        double wage = InitializationService.CalculateWage(skill: 1.0, age: 18, divisionNumber: 4, rng);
        Assert.True(wage >= 50 * Constants.WageScaleFactor * Constants.DivisionWageMultiplier(4));
    }

    [Fact]
    public void CalculateWage_StarPlayerReceivesBonus()
    {
        // Fix the RNG roll to its minimum (1+0+50=51) so the only variable is the star bonus.
        var rngStar    = new Random(0);
        var rngNonStar = new Random(0);

        double starWage    = InitializationService.CalculateWage(skill: 9.9, age: 18, divisionNumber: 1, rngStar);
        double nonStarWage = InitializationService.CalculateWage(skill: 9.0, age: 18, divisionNumber: 1, rngNonStar);

        Assert.True(starWage > nonStarWage);
    }

    [Fact]
    public void CalculateWage_OlderPlayerEarnsLess()
    {
        // Same seed and skill; older player has a higher age divisor.
        double wageYoung = InitializationService.CalculateWage(skill: 7.0, age: 22, divisionNumber: 1, new Random(42));
        double wageOld   = InitializationService.CalculateWage(skill: 7.0, age: 35, divisionNumber: 1, new Random(42));

        Assert.True(wageYoung > wageOld);
    }

    [Fact]
    public void CalculateWage_AgeDivisorFloorsAtOne()
    {
        // Ages ≤ 28 all produce divisor = 1; wages should be equal for same seed.
        double wage25 = InitializationService.CalculateWage(skill: 5.0, age: 25, divisionNumber: 2, new Random(1));
        double wage28 = InitializationService.CalculateWage(skill: 5.0, age: 28, divisionNumber: 2, new Random(1));

        Assert.Equal(wage25, wage28);
    }

    [Fact]
    public void CalculateWage_HigherDivisionEarnsMore()
    {
        // Same seed, skill, and age; only the division multiplier differs.
        double divisionOneWage  = InitializationService.CalculateWage(skill: 5.0, age: 24, divisionNumber: 1, new Random(7));
        double divisionFourWage = InitializationService.CalculateWage(skill: 5.0, age: 24, divisionNumber: 4, new Random(7));

        Assert.True(divisionOneWage > divisionFourWage);
        Assert.Equal(
            Constants.DivisionWageMultiplier(1) / Constants.DivisionWageMultiplier(4),
            divisionOneWage / divisionFourWage);
    }

    [Fact]
    public void CalculateWage_FloorScalesWithDivision()
    {
        // Skill 0 zeroes out the base formula entirely, so the result is always
        // exactly the division-scaled floor, not the old unscaled £50.
        double wage = InitializationService.CalculateWage(skill: 0.0, age: 18, divisionNumber: 1, new Random(0));
        Assert.Equal(50 * Constants.WageScaleFactor * Constants.DivisionWageMultiplier(1), wage);
    }

    // ── GeneratePlayer ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1,  PlayerPosition.Goalkeeper)]
    [InlineData(2,  PlayerPosition.Defender)]
    [InlineData(5,  PlayerPosition.Defender)]
    [InlineData(6,  PlayerPosition.Midfielder)]
    [InlineData(8,  PlayerPosition.Midfielder)]
    [InlineData(9,  PlayerPosition.Attacker)]
    [InlineData(11, PlayerPosition.Attacker)]
    public void GeneratePlayer_SlotsOneToEleven_HaveFixedPositions(int slot, PlayerPosition expected)
    {
        var player = InitializationService.GeneratePlayer(slot, divNum: 1, new Random(0));
        Assert.Equal(expected, player.Position);
    }

    [Fact]
    public void GeneratePlayer_AgeIsInRange()
    {
        var player = InitializationService.GeneratePlayer(slot: 5, divNum: 2, new Random(0));
        Assert.InRange(player.DisplayAge, 18, 35);
    }

    [Fact]
    public void GeneratePlayer_TemperIsInRange()
    {
        var player = InitializationService.GeneratePlayer(slot: 5, divNum: 2, new Random(0));
        Assert.InRange(player.Temper, 0, 9);
    }

    [Fact]
    public void GeneratePlayer_ContractWeeksIsInRange()
    {
        var player = InitializationService.GeneratePlayer(slot: 5, divNum: 2, new Random(0));
        Assert.InRange(player.ContractWeeks, 20, 75);
    }

    [Fact]
    public void GeneratePlayer_HigherDivisionProducesHigherSkill()
    {
        // Division 1 (divNum=1): skill base = |1-5| = 4. Division 4 (divNum=4): base = |4-5| = 1.
        var playerDiv1 = InitializationService.GeneratePlayer(slot: 5, divNum: 1, new Random(0));
        var playerDiv4 = InitializationService.GeneratePlayer(slot: 5, divNum: 4, new Random(0));

        Assert.True(playerDiv1.Skill > playerDiv4.Skill);
    }

    [Fact]
    public void GeneratePlayer_HasNonEmptyName()
    {
        var player = InitializationService.GeneratePlayer(slot: 1, divNum: 1, new Random(0));
        Assert.False(string.IsNullOrWhiteSpace(player.Name));
    }

    [Fact]
    public void GeneratePlayer_WeeklyWageIsAtLeast50()
    {
        var player = InitializationService.GeneratePlayer(slot: 5, divNum: 2, new Random(0));
        Assert.True(player.WeeklyWage >= 50);
    }

    [Fact]
    public void GeneratePlayer_PotentialSkillExceedsStartingSkill()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var player = InitializationService.GeneratePlayer(slot: 5, divNum: 1, new Random(seed));

            Assert.True(player.PotentialSkill > player.Skill,
                $"Seed {seed}: potential {player.PotentialSkill} not above skill {player.Skill}");
        }
    }

    [Fact]
    public void GeneratePlayer_PeakAgeBetween26And30()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var player = InitializationService.GeneratePlayer(slot: 5, divNum: 2, new Random(seed));
            Assert.InRange(player.PeakAge, 26, 30);
        }
    }

    // ── GenerateStartingSquad ─────────────────────────────────────────────────

    [Fact]
    public void GenerateStartingSquad_SlotZeroIsNull()
    {
        var result = InitializationService.GenerateStartingSquad(Division.Two, 22_000, new Random(0));
        Assert.Null(result.Squad[0]);
    }

    [Fact]
    public void GenerateStartingSquad_FirstTeamSlotsArePopulated()
    {
        var result = InitializationService.GenerateStartingSquad(Division.One, 30_000, new Random(0));
        for (int slot = 1; slot <= 11; slot++)
            Assert.NotNull(result.Squad[slot]);
    }

    [Fact]
    public void GenerateStartingSquad_MoraleIsInRange()
    {
        var result = InitializationService.GenerateStartingSquad(Division.One, 30_000, new Random(0));
        Assert.InRange(result.TeamMorale, 2, 99);
    }

    [Fact]
    public void GenerateStartingSquad_PlayerWageBillMatchesSumOfSquadWages()
    {
        var result    = InitializationService.GenerateStartingSquad(Division.Two, 22_000, new Random(0));
        double expected = result.Squad.Skip(1).Take(20)
            .Where(p => p is not null)
            .Sum(p => p!.WeeklyWage);

        Assert.Equal(expected, result.PlayerWageBill);
    }

    [Fact]
    public void GenerateStartingSquad_BankBalanceIsPositive()
    {
        var result = InitializationService.GenerateStartingSquad(Division.Three, 12_000, new Random(0));
        Assert.True(result.BankBalance > 0);
    }

    [Fact]
    public void GenerateStartingSquad_LargerGroundGivesHigherBankBalance()
    {
        // Same seed for both calls: GenerateStartingSquad only consumes rng for
        // squad/morale generation before scaling by ground capacity, so with an
        // identical seed the only difference between the two results is capacity.
        var small = InitializationService.GenerateStartingSquad(Division.Three, 6_000,  new Random(0));
        var large = InitializationService.GenerateStartingSquad(Division.Three, 30_000, new Random(0));

        Assert.True(large.BankBalance > small.BankBalance);
    }

    [Fact]
    public void GenerateStartingSquad_BankBalanceRatioIsClamped()
    {
        // A tiny (1-seat) ground and a huge (500,000-seat) one should both hit the
        // 0.5x/2.5x clamp rather than producing arbitrarily small/large balances.
        // For Division Three the unscaled range is 150,000 to ~316,666.
        var tiny = InitializationService.GenerateStartingSquad(Division.Three, 1,       new Random(0));
        var huge = InitializationService.GenerateStartingSquad(Division.Three, 500_000, new Random(0));

        Assert.InRange(tiny.BankBalance, 150_000 * 0.5, 316_666 * 0.5);
        Assert.InRange(huge.BankBalance, 150_000 * 2.5, 316_666 * 2.5);
    }

    // ── GenerateStartingStaff ─────────────────────────────────────────────────

    [Fact]
    public void GenerateStartingStaff_CoachAndPhysioAreAlwaysPresent()
    {
        var staff = InitializationService.GenerateStartingStaff(Division.Two, new Random(0));
        Assert.NotNull(staff.Coach);
        Assert.NotNull(staff.Physio);
    }

    [Fact]
    public void GenerateStartingStaff_ScoutCountIsInRange()
    {
        var staff = InitializationService.GenerateStartingStaff(Division.Two, new Random(0));
        Assert.InRange(staff.Scouts.Count, 0, 3);
    }

    [Fact]
    public void GenerateStartingStaff_YouthPlayerCountIsInRange()
    {
        var staff = InitializationService.GenerateStartingStaff(Division.Two, new Random(0));
        Assert.InRange(staff.YouthPlayers.Count, 0, 4);
    }

    [Fact]
    public void GenerateStartingStaff_IsDeterministicWithSameSeed()
    {
        var staff1 = InitializationService.GenerateStartingStaff(Division.Two, new Random(99));
        var staff2 = InitializationService.GenerateStartingStaff(Division.Two, new Random(99));

        Assert.Equal(staff1.Coach.Name,        staff2.Coach.Name);
        Assert.Equal(staff1.Physio.Name,       staff2.Physio.Name);
        Assert.Equal(staff1.Scouts.Count,      staff2.Scouts.Count);
        Assert.Equal(staff1.YouthPlayers.Count, staff2.YouthPlayers.Count);
    }
}
