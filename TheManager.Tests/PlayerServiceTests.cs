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
}
