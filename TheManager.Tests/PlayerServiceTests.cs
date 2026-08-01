using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class PlayerServiceTests
{
    // ── ToggleTransferListed ──────────────────────────────────────────────────

    [Fact]
    public void ToggleTransferListed_NotListed_ListsPlayer()
    {
        var player = new Player { Age = 25 };

        var result = PlayerService.ToggleTransferListed(player);

        Assert.True(result);
        Assert.Equal(-25, player.Age);
        Assert.True(player.IsTransferListed);
    }

    [Fact]
    public void ToggleTransferListed_AlreadyListed_UnlistsPlayer()
    {
        var player = new Player { Age = -25 };

        var result = PlayerService.ToggleTransferListed(player);

        Assert.True(result);
        Assert.Equal(25, player.Age);
        Assert.False(player.IsTransferListed);
    }

    [Fact]
    public void ToggleTransferListed_Retiring_ReturnsFalseAndDoesNotChangeAge()
    {
        var player = new Player { Age = 25, IsRetiring = true };

        var result = PlayerService.ToggleTransferListed(player);

        Assert.False(result);
        Assert.Equal(25, player.Age);
        Assert.False(player.IsTransferListed);
    }

    // ── AssignPotential ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(18)]
    [InlineData(22)]
    [InlineData(26)]
    [InlineData(30)]
    [InlineData(35)]
    public void AssignPotential_AnyAge_PotentialExceedsCurrentSkill(int age)
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var player = new Player { Position = PlayerPosition.Midfielder, Skill = 4.0, Age = age };

            PlayerService.AssignPotential(player, new Random(seed));

            Assert.True(player.PotentialSkill > player.Skill,
                $"Seed {seed}: potential {player.PotentialSkill} not above skill {player.Skill}");
        }
    }

    [Fact]
    public void AssignPotential_PeakAgeBetween26And30()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var player = new Player { Position = PlayerPosition.Midfielder, Skill = 4.0, Age = 21 };

            PlayerService.AssignPotential(player, new Random(seed));

            Assert.InRange(player.PeakAge, 26, 30);
        }
    }

    [Fact]
    public void AssignPotential_PotentialNeverExceedsMaximumSkill()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var player = new Player { Position = PlayerPosition.Attacker, Skill = 7.9, Age = 18 };

            PlayerService.AssignPotential(player, new Random(seed));

            Assert.True(player.PotentialSkill <= 9.9);
        }
    }

    [Fact]
    public void AssignPotential_SameRoll_YoungerPlayerHasHigherCeiling()
    {
        var young = new Player { Position = PlayerPosition.Attacker, Skill = 4.0, Age = 20 };
        var old   = new Player { Position = PlayerPosition.Attacker, Skill = 4.0, Age = 30 };

        PlayerService.AssignPotential(young, new Random(7));
        PlayerService.AssignPotential(old,   new Random(7));

        Assert.True(young.PotentialSkill > old.PotentialSkill);
    }

    // ── AssignDevelopmentRate ──────────────────────────────────────────────────

    [Fact]
    public void AssignDevelopmentRate_PositiveHeadroom_ProducesPositiveRate()
    {
        var player = new Player { Position = PlayerPosition.Midfielder, Skill = 4.0, PotentialSkill = 6.8, Age = 21, PeakAge = 27 };

        PlayerService.AssignDevelopmentRate(player, new Random(1));

        Assert.True(player.DevelopmentRate > 0);
    }

    [Fact]
    public void AssignDevelopmentRate_NoHeadroom_ProducesZeroRate()
    {
        var player = new Player { Position = PlayerPosition.Midfielder, Skill = 6.8, PotentialSkill = 6.8, Age = 21, PeakAge = 27 };

        PlayerService.AssignDevelopmentRate(player, new Random(1));

        Assert.Equal(0.0, player.DevelopmentRate);
    }

    [Fact]
    public void AssignDevelopmentRate_PaceRoll_VariesAcrossSeeds()
    {
        var rates = new HashSet<double>();
        for (int seed = 0; seed < 30; seed++)
        {
            var player = new Player { Position = PlayerPosition.Midfielder, Skill = 4.0, PotentialSkill = 6.8, Age = 21, PeakAge = 27 };
            PlayerService.AssignDevelopmentRate(player, new Random(seed));
            rates.Add(player.DevelopmentRate);
        }

        Assert.True(rates.Count > 1, "Expected the pace roll to produce more than one distinct rate across seeds.");
    }

    // ── ApplyAppearanceGrowth ──────────────────────────────────────────────────

    [Fact]
    public void ApplyAppearanceGrowth_Starter_GrowsByFullDevelopmentRate()
    {
        var squad = new Player?[29];
        squad[3] = new Player { Position = PlayerPosition.Defender, Skill = 5.0, PotentialSkill = 9.9, DevelopmentRate = 0.02 };

        PlayerService.ApplyAppearanceGrowth(squad);

        Assert.Equal(5.02, squad[3]!.Skill, precision: 5);
        Assert.Equal(0.02, squad[3]!.SkillGainedThisSeason, precision: 5);
    }

    [Fact]
    public void ApplyAppearanceGrowth_UnusedSubstitute_GrowsByHalfRate()
    {
        var squad = new Player?[29];
        squad[12] = new Player { Position = PlayerPosition.Attacker, Skill = 5.0, PotentialSkill = 9.9, DevelopmentRate = 0.02 };

        PlayerService.ApplyAppearanceGrowth(squad);

        Assert.Equal(5.01, squad[12]!.Skill, precision: 5);
    }

    [Fact]
    public void ApplyAppearanceGrowth_Reserve_GrowsByHalfRate()
    {
        var squad = new Player?[29];
        squad[15] = new Player { Position = PlayerPosition.Midfielder, Skill = 5.0, PotentialSkill = 9.9, DevelopmentRate = 0.02 };

        PlayerService.ApplyAppearanceGrowth(squad);

        Assert.Equal(5.01, squad[15]!.Skill, precision: 5);
    }

    [Fact]
    public void ApplyAppearanceGrowth_TransferTargetSlot_IsUntouched()
    {
        var squad = new Player?[29];
        squad[21] = new Player { Position = PlayerPosition.Attacker, Skill = 5.0, PotentialSkill = 9.9, DevelopmentRate = 0.5 };

        PlayerService.ApplyAppearanceGrowth(squad);

        Assert.Equal(5.0, squad[21]!.Skill);
    }

    [Fact]
    public void ApplyAppearanceGrowth_ClampsAtPotential()
    {
        var squad = new Player?[29];
        squad[3] = new Player { Position = PlayerPosition.Defender, Skill = 9.85, PotentialSkill = 9.9, DevelopmentRate = 0.5 };

        PlayerService.ApplyAppearanceGrowth(squad);

        Assert.Equal(9.9, squad[3]!.Skill, precision: 5);
    }

    [Fact]
    public void ApplyAppearanceGrowth_RepeatedAppearances_NeverExceedsSeasonCap()
    {
        var squad = new Player?[29];
        var player = new Player { Position = PlayerPosition.Attacker, Skill = 1.1, PotentialSkill = 9.9, DevelopmentRate = 1.0 };
        squad[9] = player;

        for (int week = 0; week < 50; week++)
            PlayerService.ApplyAppearanceGrowth(squad);

        Assert.Equal(4.0, player.SkillGainedThisSeason, precision: 5);
        Assert.Equal(5.1, player.Skill, precision: 5);
    }

    // ── ApplyEndOfSeasonSkillUpdate ───────────────────────────────────────────

    [Fact]
    public void ApplyEndOfSeasonSkillUpdate_PlayerWithYellowCards_ResetsTallyToZero()
    {
        var squad = new Player?[29];
        squad[1] = new Player { Position = PlayerPosition.Defender, Skill = 5.0, YellowCardsThisSeason = 3 };

        PlayerService.ApplyEndOfSeasonSkillUpdate(squad, new Random(0));

        Assert.Equal(0, squad[1]!.YellowCardsThisSeason);
    }

    [Fact]
    public void ApplyEndOfSeasonSkillUpdate_PlayerWithSkillGainedThisSeason_ResetsToZero()
    {
        var squad = new Player?[29];
        squad[1] = new Player { Position = PlayerPosition.Defender, Skill = 5.0, SkillGainedThisSeason = 3.5 };

        PlayerService.ApplyEndOfSeasonSkillUpdate(squad, new Random(0));

        Assert.Equal(0.0, squad[1]!.SkillGainedThisSeason);
    }

    [Fact]
    public void ApplyEndOfSeasonSkillUpdate_PlayerWithNoYellowCards_StaysAtZero()
    {
        var squad = new Player?[29];
        squad[1] = new Player { Position = PlayerPosition.Defender, Skill = 5.0, YellowCardsThisSeason = 0 };

        PlayerService.ApplyEndOfSeasonSkillUpdate(squad, new Random(0));

        Assert.Equal(0, squad[1]!.YellowCardsThisSeason);
    }

    [Theory]
    [InlineData(1,  0)]
    [InlineData(12, 0)]
    [InlineData(13, 1)]
    [InlineData(20, 8)]
    public void ApplyEndOfSeasonSkillUpdate_InjuryHealsByTwelveWeeksOverBreak(
        int weeksInjured, int expectedRemaining)
    {
        var squad = new Player?[29];
        squad[1] = MakePlayer(age: 25, peakAge: 30);
        squad[1]!.WeeksInjured = weeksInjured;

        PlayerService.ApplyEndOfSeasonSkillUpdate(squad, new Random(0));

        Assert.Equal(expectedRemaining, squad[1]!.WeeksInjured);
    }

    [Fact]
    public void ApplyEndOfSeasonSkillUpdate_PlayerAgesOneYear()
    {
        var squad = new Player?[29];
        squad[1] = MakePlayer(age: 25, peakAge: 30);

        PlayerService.ApplyEndOfSeasonSkillUpdate(squad, new Random(0));

        Assert.Equal(26, squad[1]!.Age);
    }

    [Fact]
    public void ApplyEndOfSeasonSkillUpdate_TransferListedPlayer_AgesAndStaysListed()
    {
        var squad = new Player?[29];
        squad[1] = MakePlayer(age: 25, peakAge: 30);
        MarketService.ListForTransfer(squad[1]!);

        PlayerService.ApplyEndOfSeasonSkillUpdate(squad, new Random(0));

        Assert.Equal(26, squad[1]!.DisplayAge);
        Assert.True(squad[1]!.IsTransferListed);
    }

    [Theory]
    [InlineData(24)]
    [InlineData(28)]
    [InlineData(33)]
    public void ApplyEndOfSeasonSkillUpdate_AnyAge_CeilingNeverDrops(int age)
    {
        var squad = new Player?[29];
        squad[1] = MakePlayer(age: age, peakAge: 28, skill: 6.0, potential: 8.0);

        PlayerService.ApplyEndOfSeasonSkillUpdate(squad, new Random(0));

        Assert.Equal(8.0, squad[1]!.PotentialSkill);
    }

    [Fact]
    public void ApplyEndOfSeasonSkillUpdate_Under30_NoDeclinePenalty()
    {
        // Two different under-30 ages both draw from the same range and
        // carry no yearsPastPeak penalty (0 for anyone <= PeakWindowEndAge),
        // so with the same seed they land on the same result.
        var squadYoung = new Player?[29];
        var squadOlder = new Player?[29];
        squadYoung[1] = MakePlayer(age: 22, peakAge: 26, skill: 5.0);
        squadOlder[1] = MakePlayer(age: 28, peakAge: 26, skill: 5.0);

        PlayerService.ApplyEndOfSeasonSkillUpdate(squadYoung, new Random(5));
        PlayerService.ApplyEndOfSeasonSkillUpdate(squadOlder, new Random(5));

        Assert.Equal(squadYoung[1]!.Skill, squadOlder[1]!.Skill);
    }

    [Fact]
    public void ApplyEndOfSeasonSkillUpdate_Under30_DriftStaysWithinNarrowRange()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            var squad = new Player?[29];
            squad[1] = MakePlayer(age: 24, peakAge: 28, skill: 5.0, potential: 9.9);

            PlayerService.ApplyEndOfSeasonSkillUpdate(squad, new Random(seed));

            double drift = squad[1]!.Skill - 5.0;
            Assert.InRange(drift, -0.5, 0.5);
        }
    }

    [Fact]
    public void ApplyEndOfSeasonSkillUpdate_Age30_DriftCanGoLowerThanUnder30Range()
    {
        // At exactly PeakWindowEndAge (30) the wider pre-existing range
        // (-0.7 to +0.5) still applies — only ages strictly under 30 get the
        // narrower range.
        bool sawBelowNarrowRangeFloor = false;
        for (int seed = 0; seed < 200; seed++)
        {
            var squad = new Player?[29];
            squad[1] = MakePlayer(age: 29, peakAge: 30, skill: 5.0, potential: 9.9);

            PlayerService.ApplyEndOfSeasonSkillUpdate(squad, new Random(seed));

            if (squad[1]!.Skill - 5.0 < -0.5)
            {
                sawBelowNarrowRangeFloor = true;
                break;
            }
        }

        Assert.True(sawBelowNarrowRangeFloor);
    }

    [Fact]
    public void ApplyEndOfSeasonSkillUpdate_PastAge30_EndsLowerThanPeakPlayerWithSameDrift()
    {
        var squadPeak = new Player?[29];
        var squadOld  = new Player?[29];
        squadPeak[1] = MakePlayer(age: 28, peakAge: 28, skill: 5.0);
        squadOld[1]  = MakePlayer(age: 33, peakAge: 28, skill: 5.0);

        PlayerService.ApplyEndOfSeasonSkillUpdate(squadPeak, new Random(5));
        PlayerService.ApplyEndOfSeasonSkillUpdate(squadOld,  new Random(5));

        Assert.True(squadOld[1]!.Skill < squadPeak[1]!.Skill);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Player MakePlayer(int age, int peakAge, double skill = 5.0, double potential = 9.9) => new()
    {
        Position       = PlayerPosition.Defender,
        Skill          = skill,
        PotentialSkill = potential,
        Age            = age,
        PeakAge        = peakAge
    };
}
