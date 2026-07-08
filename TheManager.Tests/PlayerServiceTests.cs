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

    // ── ApplyPostMatchSkillChanges ────────────────────────────────────────────

    [Fact]
    public void ApplyPostMatchSkillChanges_Win_SkillNeverExceedsPotential()
    {
        var squad = new Player?[29];
        squad[9] = new Player { Position = PlayerPosition.Attacker, Skill = 5.0, PotentialSkill = 5.0, Age = 24 };

        PlayerService.ApplyPostMatchSkillChanges(squad, won: true, lost: false, cleanSheet: false);

        Assert.Equal(5.0, squad[9]!.Skill);
    }

    [Fact]
    public void ApplyPostMatchSkillChanges_Win_SkillIncreasesBelowPotential()
    {
        var squad = new Player?[29];
        squad[9] = new Player { Position = PlayerPosition.Attacker, Skill = 5.0, PotentialSkill = 9.9, Age = 24 };

        PlayerService.ApplyPostMatchSkillChanges(squad, won: true, lost: false, cleanSheet: false);

        Assert.Equal(5.05, squad[9]!.Skill, precision: 5);
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
    public void ApplyEndOfSeasonSkillUpdate_PlayerWithNoYellowCards_StaysAtZero()
    {
        var squad = new Player?[29];
        squad[1] = new Player { Position = PlayerPosition.Defender, Skill = 5.0, YellowCardsThisSeason = 0 };

        PlayerService.ApplyEndOfSeasonSkillUpdate(squad, new Random(0));

        Assert.Equal(0, squad[1]!.YellowCardsThisSeason);
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
    public void ApplyEndOfSeasonSkillUpdate_WithinPeakWindow_NoDeclinePenalty()
    {
        var squadYoung = new Player?[29];
        var squadPeak  = new Player?[29];
        squadYoung[1] = MakePlayer(age: 22, peakAge: 26, skill: 5.0);
        squadPeak[1]  = MakePlayer(age: 29, peakAge: 26, skill: 5.0);

        PlayerService.ApplyEndOfSeasonSkillUpdate(squadYoung, new Random(5));
        PlayerService.ApplyEndOfSeasonSkillUpdate(squadPeak,  new Random(5));

        Assert.Equal(squadYoung[1]!.Skill, squadPeak[1]!.Skill);
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
