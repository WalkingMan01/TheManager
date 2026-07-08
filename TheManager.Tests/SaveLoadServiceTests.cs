using System.Text.Json;
using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class SaveLoadServiceTests
{
    // ── Deserialize (legacy-save migration) ───────────────────────────────────

    [Fact]
    public void Deserialize_LegacyPlayerWithoutPeakAge_AssignsPotential()
    {
        var state = MakeStateWithPlayer(new Player
        {
            Position = PlayerPosition.Midfielder,
            Skill    = 5.0,
            Age      = 25
        });
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(42));

        var player = loaded.Squad[1]!;
        Assert.InRange(player.PeakAge, 26, 30);
        Assert.True(player.PotentialSkill > player.Skill);
    }

    [Fact]
    public void Deserialize_LegacyPlayer_SkillIsUnchanged()
    {
        var state = MakeStateWithPlayer(new Player
        {
            Position = PlayerPosition.Midfielder,
            Skill    = 5.0,
            Age      = 25
        });
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(42));

        Assert.Equal(5.0, loaded.Squad[1]!.Skill);
    }

    [Fact]
    public void Deserialize_PlayerWithAssignedPotential_RoundTripsUnchanged()
    {
        var state = MakeStateWithPlayer(new Player
        {
            Position       = PlayerPosition.Defender,
            Skill          = 6.0,
            PotentialSkill = 7.0,
            Age            = 24,
            PeakAge        = 28
        });
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(42));

        var player = loaded.Squad[1]!;
        Assert.Equal(6.0, player.Skill);
        Assert.Equal(7.0, player.PotentialSkill);
        Assert.Equal(28, player.PeakAge);
    }

    [Fact]
    public void Deserialize_EmptySquad_DoesNotThrow()
    {
        var state = new GameState();
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(42));

        Assert.All(loaded.Squad, player => Assert.Null(player));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameState MakeStateWithPlayer(Player player)
    {
        var state = new GameState();
        state.Squad[1] = player;
        return state;
    }
}
