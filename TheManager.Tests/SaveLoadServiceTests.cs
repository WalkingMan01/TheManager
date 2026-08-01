using System.Text.Json;
using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.Tests;

public class SaveLoadServiceTests
{
    // ── Deserialize (legacy cup migration) ────────────────────────────────────

    [Fact]
    public void Deserialize_LegacySaveWith120TeamNames_ExpandsTo128AndFillsNonLeagueNames()
    {
        var state = new GameState();
        TeamData.Seed(state);
        state.AllTeamNames = state.AllTeamNames.Take(120).ToArray();   // pre-FA-Cup pool
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(42));

        Assert.Equal(128, loaded.AllTeamNames.Length);
        for (int i = 93; i <= 124; i++)
            Assert.False(string.IsNullOrWhiteSpace(loaded.AllTeamNames[i]),
                $"expected a non-league name at index {i}");
    }

    [Fact]
    public void Deserialize_LegacySaveWithLeagueOnlyFixtures_SitsOutTheCupThisSeason()
    {
        var state = new GameState();
        TeamData.Seed(state);
        state.FACup.CurrentRound = CupRound.Round1;
        state.Fixtures = Enumerable.Range(1, 46)
            .Select(w => new ScheduledMatch { Week = w, MatchType = MatchType.League, OpponentName = "X" })
            .ToList();
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(42));

        Assert.Equal(CupRound.NotEntered, loaded.FACup.CurrentRound);
    }

    [Fact]
    public void Deserialize_SaveWithCupCalendar_KeepsCupState()
    {
        var state = new GameState();
        TeamData.Seed(state);
        state.FACup.CurrentRound = CupRound.Round2;
        state.Fixtures =
        [
            new ScheduledMatch { Week = 1,  MatchType = MatchType.League, OpponentName = "X" },
            new ScheduledMatch { Week = 12, MatchType = MatchType.FACup }
        ];
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(42));

        Assert.Equal(CupRound.Round2, loaded.FACup.CurrentRound);
    }

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
        Assert.True(player.DevelopmentRate > 0);
    }

    [Fact]
    public void Deserialize_PlayerWithPotentialButNoDevelopmentRate_BackfillsRate()
    {
        // A player saved after the potential mechanic shipped but before
        // DevelopmentRate existed: PeakAge/PotentialSkill are already valid,
        // so the PeakAge==0 migration branch won't fire — only the
        // DevelopmentRate backfill should run.
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
        Assert.Equal(28, player.PeakAge);
        Assert.Equal(7.0, player.PotentialSkill);
        Assert.True(player.DevelopmentRate > 0);
    }

    [Fact]
    public void Deserialize_PlayerWithDevelopmentRateAlready_RoundTripsUnchanged()
    {
        var state = MakeStateWithPlayer(new Player
        {
            Position        = PlayerPosition.Defender,
            Skill           = 6.0,
            PotentialSkill  = 7.0,
            Age             = 24,
            PeakAge         = 28,
            DevelopmentRate = 0.015
        });
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(42));

        Assert.Equal(0.015, loaded.Squad[1]!.DevelopmentRate);
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
