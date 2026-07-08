using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class MatchEngineServiceTests
{
    // ── ResolveIncident ──────────────────────────────────────────────────────

    [Fact]
    public void ResolveIncident_NoPlayersInLineup_ReturnsNull()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var engine = new MatchEngineService(new Random(seed));
            var squad  = new Player?[29];
            bool substitutionUsed = false;

            var result = engine.ResolveIncident(squad, incidentBeforeMinute81: true, ref substitutionUsed);

            Assert.Null(result);
        }
    }

    [Fact]
    public void ResolveIncident_NoFreeReserveSlot_ReturnsNullAndLeavesSquadUnchanged()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var engine = new MatchEngineService(new Random(seed));
            var squad  = MakeFullSquad();
            bool substitutionUsed = false;

            var result = engine.ResolveIncident(squad, incidentBeforeMinute81: true, ref substitutionUsed);

            Assert.Null(result);
            Assert.False(substitutionUsed);
            for (int slot = 1; slot <= 20; slot++)
            {
                Assert.Equal(0, squad[slot]!.WeeksInjured);
                Assert.Equal(0, squad[slot]!.SuspensionMatchesRemaining);
            }
        }
    }

    [Fact]
    public void ResolveIncident_RedCard_SuspendsPlayerAndClearsTeamSlot()
    {
        bool sawRedCard = false;

        for (int seed = 0; seed < 300; seed++)
        {
            var engine = new MatchEngineService(new Random(seed));
            var squad  = MakeSquadWithOneFreeReserveSlot(out int freeSlot);
            bool substitutionUsed = false;

            var result = engine.ResolveIncident(squad, incidentBeforeMinute81: true, ref substitutionUsed);
            if (result is not { Type: IncidentType.RedCard }) continue;

            sawRedCard = true;
            var sentOff = squad[freeSlot];
            Assert.NotNull(sentOff);
            Assert.Equal(result.PlayerName, sentOff!.Name);
            Assert.Equal(3, sentOff.SuspensionMatchesRemaining);
            Assert.Null(squad[result.PlayerSlot]);
            Assert.False(substitutionUsed);
            AssertNoDuplicatePlayers(squad);
        }

        Assert.True(sawRedCard, "Expected at least one RedCard outcome across the seed range");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResolveIncident_Injury_HandlesSubstituteAvailability(bool substitutionAlreadyUsed)
    {
        bool sawInjury = false;

        for (int seed = 0; seed < 300; seed++)
        {
            var engine      = new MatchEngineService(new Random(seed));
            var squad       = MakeSquadWithOneFreeReserveSlot(out int freeSlot);
            var originalSub = squad[12];
            bool substitutionUsed = substitutionAlreadyUsed;

            var result = engine.ResolveIncident(squad, incidentBeforeMinute81: true, ref substitutionUsed);
            if (result is not { Type: IncidentType.Injury }) continue;

            sawInjury = true;
            Assert.True(substitutionUsed);

            if (substitutionAlreadyUsed)
            {
                // No sub left — the injured player leaves with no replacement.
                Assert.Null(squad[result.PlayerSlot]);
                if (result.PlayerSlot != 12)
                    Assert.Same(originalSub, squad[12]);
            }
            else
            {
                // Substitute comes on in the injured player's place.
                Assert.Null(squad[12]);
                if (result.PlayerSlot != 12)
                    Assert.Same(originalSub, squad[result.PlayerSlot]);
            }

            var injured = squad[freeSlot];
            Assert.NotNull(injured);
            Assert.Equal(result.PlayerName, injured!.Name);
            Assert.Equal(result.WeeksOut, injured.WeeksInjured);
            Assert.True(result.WeeksOut > 0);
            AssertNoDuplicatePlayers(squad);
        }

        Assert.True(sawInjury, "Expected at least one Injury outcome across the seed range");
    }

    // ── ApplyYellowCard ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void ApplyYellowCard_SlotOutsideTeamRange_ReturnsNull(int slot)
    {
        var engine = new MatchEngineService(new Random(0));
        var squad  = MakeFullSquad();

        Assert.Null(engine.ApplyYellowCard(squad, slot));
    }

    [Fact]
    public void ApplyYellowCard_EmptySlot_ReturnsNull()
    {
        var engine = new MatchEngineService(new Random(0));
        var squad  = new Player?[29];

        Assert.Null(engine.ApplyYellowCard(squad, 5));
    }

    [Fact]
    public void ApplyYellowCard_FirstBooking_IncrementsTallyWithoutSuspension()
    {
        var engine = new MatchEngineService(new Random(0));
        var squad  = MakeFullSquad();
        var player = squad[5]!;
        player.YellowCardsThisSeason = 2;

        var outcome = engine.ApplyYellowCard(squad, 5);

        Assert.NotNull(outcome);
        Assert.False(outcome!.SuspensionImposed);
        Assert.Equal(player.Name, outcome.PlayerName);
        Assert.Equal(3, player.YellowCardsThisSeason);
        Assert.Equal(0, player.SuspensionMatchesRemaining);
    }

    [Fact]
    public void ApplyYellowCard_FifthBooking_ImposesSuspensionAndResetsTally()
    {
        var engine = new MatchEngineService(new Random(0));
        var squad  = MakeFullSquad();
        var player = squad[5]!;
        player.YellowCardsThisSeason = 4;

        var outcome = engine.ApplyYellowCard(squad, 5);

        Assert.NotNull(outcome);
        Assert.True(outcome!.SuspensionImposed);
        Assert.Equal(0, player.YellowCardsThisSeason);
        Assert.Equal(1, player.SuspensionMatchesRemaining);
    }

    [Fact]
    public void ApplyYellowCard_FifthBookingWhileAlreadySuspended_KeepsLongerSuspension()
    {
        var engine = new MatchEngineService(new Random(0));
        var squad  = MakeFullSquad();
        var player = squad[5]!;
        player.YellowCardsThisSeason      = 4;
        player.SuspensionMatchesRemaining = 3;

        var outcome = engine.ApplyYellowCard(squad, 5);

        Assert.True(outcome!.SuspensionImposed);
        Assert.Equal(3, player.SuspensionMatchesRemaining);
    }

    // ── RecordOurGoal ─────────────────────────────────────────────────────────

    [Fact]
    public void RecordOurGoal_NoAttackersOnPitch_StillCreditsAPlayer()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var engine = new MatchEngineService(new Random(seed));
            var squad  = MakeFullSquad();   // slots 2–11 are all defenders

            Assert.NotNull(engine.RecordOurGoal(squad));
        }
    }

    [Fact]
    public void RecordOurGoal_OnlyAttackersOnPitch_StillCreditsAPlayer()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var engine = new MatchEngineService(new Random(seed));
            var squad  = new Player?[29];
            for (int slot = 2; slot <= 11; slot++)
            {
                squad[slot] = MakePlayer($"P{slot}");
                squad[slot]!.Position = PlayerPosition.Attacker;
            }

            Assert.NotNull(engine.RecordOurGoal(squad));
        }
    }

    [Fact]
    public void RecordOurGoal_SinglePlayerOnPitch_AlwaysCreditsThatPlayer()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var engine = new MatchEngineService(new Random(seed));
            var squad  = new Player?[29];
            squad[11] = MakePlayer("Lone");
            squad[11]!.Position = PlayerPosition.Attacker;

            Assert.Equal("Lone", engine.RecordOurGoal(squad));
        }
    }

    [Fact]
    public void RecordOurGoal_EmptyLineup_ReturnsNull()
    {
        var engine = new MatchEngineService(new Random(0));
        var squad  = new Player?[29];

        Assert.Null(engine.RecordOurGoal(squad));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Player MakePlayer(string name) => new()
    {
        Name     = name,
        Position = PlayerPosition.Defender,
        Skill    = 5.0
    };

    private static Player?[] MakeFullSquad()
    {
        var squad = new Player?[29];
        for (int slot = 1; slot <= 20; slot++)
            squad[slot] = MakePlayer($"P{slot}");
        return squad;
    }

    private static Player?[] MakeSquadWithOneFreeReserveSlot(out int freeSlot)
    {
        var squad = new Player?[29];
        for (int slot = 1; slot <= 19; slot++)
            squad[slot] = MakePlayer($"P{slot}");
        freeSlot = 20;
        return squad;
    }

    private static void AssertNoDuplicatePlayers(Player?[] squad)
    {
        var seen = new HashSet<Player>();
        for (int slot = 1; slot <= 20; slot++)
        {
            var p = squad[slot];
            if (p == null) continue;
            Assert.True(seen.Add(p), $"Player {p.Name} appears in more than one squad slot");
        }
    }
}
