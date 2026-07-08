using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class InitializationServiceTests
{
    // ── CalculateWage ─────────────────────────────────────────────────────────

    [Fact]
    public void CalculateWage_ReturnsAtLeast50()
    {
        var rng = new Random(0);
        double wage = InitializationService.CalculateWage(skill: 1.0, age: 18, rng);
        Assert.True(wage >= 50);
    }

    [Fact]
    public void CalculateWage_StarPlayerReceivesBonus()
    {
        // Fix the RNG roll to its minimum (1+0+50=51) so the only variable is the star bonus.
        var rngStar    = new Random(0);
        var rngNonStar = new Random(0);

        double starWage    = InitializationService.CalculateWage(skill: 9.9, age: 18, rngStar);
        double nonStarWage = InitializationService.CalculateWage(skill: 9.0, age: 18, rngNonStar);

        Assert.True(starWage > nonStarWage);
    }

    [Fact]
    public void CalculateWage_OlderPlayerEarnsLess()
    {
        // Same seed and skill; older player has a higher age divisor.
        double wageYoung = InitializationService.CalculateWage(skill: 7.0, age: 22, new Random(42));
        double wageOld   = InitializationService.CalculateWage(skill: 7.0, age: 35, new Random(42));

        Assert.True(wageYoung > wageOld);
    }

    [Fact]
    public void CalculateWage_AgeDivisorFloorsAtOne()
    {
        // Ages ≤ 28 all produce divisor = 1; wages should be equal for same seed.
        double wage25 = InitializationService.CalculateWage(skill: 5.0, age: 25, new Random(1));
        double wage28 = InitializationService.CalculateWage(skill: 5.0, age: 28, new Random(1));

        Assert.Equal(wage25, wage28);
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
        var result = InitializationService.GenerateStartingSquad(Division.Two, new Random(0));
        Assert.Null(result.Squad[0]);
    }

    [Fact]
    public void GenerateStartingSquad_FirstTeamSlotsArePopulated()
    {
        var result = InitializationService.GenerateStartingSquad(Division.One, new Random(0));
        for (int slot = 1; slot <= 11; slot++)
            Assert.NotNull(result.Squad[slot]);
    }

    [Fact]
    public void GenerateStartingSquad_MoraleIsInRange()
    {
        var result = InitializationService.GenerateStartingSquad(Division.One, new Random(0));
        Assert.InRange(result.TeamMorale, 2, 99);
    }

    [Fact]
    public void GenerateStartingSquad_PlayerWageBillMatchesSumOfSquadWages()
    {
        var result    = InitializationService.GenerateStartingSquad(Division.Two, new Random(0));
        double expected = result.Squad.Skip(1).Take(20)
            .Where(p => p is not null)
            .Sum(p => p!.WeeklyWage);

        Assert.Equal(expected, result.PlayerWageBill);
    }

    [Fact]
    public void GenerateStartingSquad_BankBalanceIsPositive()
    {
        var result = InitializationService.GenerateStartingSquad(Division.Three, new Random(0));
        Assert.True(result.BankBalance > 0);
    }

    // ── GenerateStartingStaff ─────────────────────────────────────────────────

    [Fact]
    public void GenerateStartingStaff_CoachAndPhysioAreAlwaysPresent()
    {
        var staff = InitializationService.GenerateStartingStaff(new Random(0));
        Assert.NotNull(staff.Coach);
        Assert.NotNull(staff.Physio);
    }

    [Fact]
    public void GenerateStartingStaff_ScoutCountIsInRange()
    {
        var staff = InitializationService.GenerateStartingStaff(new Random(0));
        Assert.InRange(staff.Scouts.Count, 0, 3);
    }

    [Fact]
    public void GenerateStartingStaff_YouthPlayerCountIsInRange()
    {
        var staff = InitializationService.GenerateStartingStaff(new Random(0));
        Assert.InRange(staff.YouthPlayers.Count, 0, 4);
    }

    [Fact]
    public void GenerateStartingStaff_IsDeterministicWithSameSeed()
    {
        var staff1 = InitializationService.GenerateStartingStaff(new Random(99));
        var staff2 = InitializationService.GenerateStartingStaff(new Random(99));

        Assert.Equal(staff1.Coach.Name,        staff2.Coach.Name);
        Assert.Equal(staff1.Physio.Name,       staff2.Physio.Name);
        Assert.Equal(staff1.Scouts.Count,      staff2.Scouts.Count);
        Assert.Equal(staff1.YouthPlayers.Count, staff2.YouthPlayers.Count);
    }
}
