using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class RandomEventServiceTests
{
    private static readonly string[] TeamNames = MakeTeamNames();

    // ── EvaluateWeeklyEvents ──────────────────────────────────────────────────

    [Fact]
    public void EvaluateWeeklyEvents_EmptySquad_ReturnsNoEvents()
    {
        var squad  = new Player?[29];
        var events = RandomEventService.EvaluateWeeklyEvents(squad, "OurClub", TeamNames, new Random(0));
        Assert.Empty(events);
    }

    [Fact]
    public void EvaluateWeeklyEvents_NoStarPlayers_NoStarEvents()
    {
        var squad  = new Player?[29];
        squad[1]   = new Player { Name = "Jones", Skill = 5.0, Age = 25 };  // not a star

        var events = RandomEventService.EvaluateWeeklyEvents(squad, "OurClub", TeamNames, new Random(0));

        Assert.DoesNotContain(events, e => e.Type == RandomEventType.InternationalCallUp);
        Assert.DoesNotContain(events, e => e.Type == RandomEventType.ForeignTransferOffer);
    }

    [Fact]
    public void EvaluateWeeklyEvents_ReturnsList()
    {
        var squad = new Player?[29];
        var events = RandomEventService.EvaluateWeeklyEvents(squad, "OurClub", TeamNames, new Random(42));
        Assert.NotNull(events);
    }

    // ── EvaluateWeeklyEvents (star player paths) ──────────────────────────────

    [Fact]
    public void EvaluateWeeklyEvents_StarPlayer_LowRoll_FiresCallUpEvent()
    {
        var squad  = new Player?[29];
        squad[1]   = new Player { Name = "Star", Skill = 9.9, Age = 25, Position = PlayerPosition.Attacker };

        // Find seed where 1+rng.Next(35) < 6 (i.e. rng.Next(35) <= 4)
        int seed = FindSeedWhereFirstNextReturnsAtMost(maxValue: 35, threshold: 4);

        var events = RandomEventService.EvaluateWeeklyEvents(squad, "OurClub", TeamNames, new Random(seed));

        Assert.Contains(events, e => e.Type == RandomEventType.InternationalCallUp);
    }

    [Fact]
    public void EvaluateWeeklyEvents_StarPlayer_CallUpEvent_HasCorrectPlayerName()
    {
        var squad = new Player?[29];
        squad[1]  = new Player { Name = "Ronaldo", Skill = 9.9, Age = 25, Position = PlayerPosition.Attacker };

        int seed   = FindSeedWhereFirstNextReturnsAtMost(maxValue: 35, threshold: 4);
        var events = RandomEventService.EvaluateWeeklyEvents(squad, "OurClub", TeamNames, new Random(seed));

        var callUp = events.First(e => e.Type == RandomEventType.InternationalCallUp);
        Assert.Equal("Ronaldo", callUp.PlayerName);
        Assert.Equal(1, callUp.PlayerSlot);
    }

    [Fact]
    public void EvaluateWeeklyEvents_OlderPlayer_MayAnnounceRetirement()
    {
        var squad = new Player?[29];
        squad[1]  = new Player { Name = "Old", Skill = 5.0, Age = 32, Position = PlayerPosition.Defender };

        // With no star, rng sequence is: Next(35) consumed, then Next(10) for retirement check.
        // Find seed where Next(35) (any) then Next(10)==0.
        int seed = FindSeedForRetirementAnnouncement();

        var events = RandomEventService.EvaluateWeeklyEvents(squad, "OurClub", TeamNames, new Random(seed));

        Assert.Contains(events, e => e.Type == RandomEventType.RetirementAnnouncement);
    }

    // ── ResolveInternationalCallUp ────────────────────────────────────────────

    [Fact]
    public void ResolveInternationalCallUp_Release_ClearsPlayerSlot()
    {
        var squad = SquadWithPlayer(slot: 5, skill: 9.9);
        RandomEventService.ResolveInternationalCallUp(squad, playerSlot: 5, managerReleasesPlayer: true, new Random(0));
        Assert.Null(squad[5]);
    }

    [Fact]
    public void ResolveInternationalCallUp_Refuse_KeepsPlayerInSlot()
    {
        var squad = SquadWithPlayer(slot: 5, skill: 9.9);
        RandomEventService.ResolveInternationalCallUp(squad, playerSlot: 5, managerReleasesPlayer: false, new Random(0));
        Assert.NotNull(squad[5]);
    }

    [Fact]
    public void ResolveInternationalCallUp_Refuse_ReducesSkill()
    {
        var squad      = SquadWithPlayer(slot: 5, skill: 8.0);
        double before  = squad[5]!.Skill;
        RandomEventService.ResolveInternationalCallUp(squad, playerSlot: 5, managerReleasesPlayer: false, new Random(0));
        Assert.True(squad[5]!.Skill < before);
    }

    [Fact]
    public void ResolveInternationalCallUp_NullPlayer_DoesNothing()
    {
        var squad = new Player?[29];
        // Should not throw
        RandomEventService.ResolveInternationalCallUp(squad, playerSlot: 5, managerReleasesPlayer: true, new Random(0));
    }

    [Fact]
    public void ResolveInternationalCallUp_Release_PromotesReserveToVacatedSlot()
    {
        var squad   = new Player?[29];
        var star    = new Player { Name = "Star",    Skill = 9.9, Position = PlayerPosition.Attacker, Age = 25 };
        var reserve = new Player { Name = "Reserve", Skill = 5.0, Position = PlayerPosition.Midfielder, Age = 22 };
        squad[5]  = star;
        squad[13] = reserve;

        RandomEventService.ResolveInternationalCallUp(squad, playerSlot: 5, managerReleasesPlayer: true, new Random(0));

        Assert.Same(reserve, squad[5]);    // reserve promoted into the vacated slot
        Assert.Null(squad[13]);            // reserve slot cleared
    }

    [Fact]
    public void ResolveInternationalCallUp_Release_NoReserve_SlotRemainsNull()
    {
        var squad = SquadWithPlayer(slot: 5, skill: 9.9);
        // No reserves in slots 13-20

        RandomEventService.ResolveInternationalCallUp(squad, playerSlot: 5, managerReleasesPlayer: true, new Random(0));

        Assert.Null(squad[5]);   // slot cleared, nothing promoted
    }

    // ── ResolveForeignTransferOffer ───────────────────────────────────────────

    [Fact]
    public void ResolveForeignTransferOffer_Decline_NoChanges()
    {
        var squad    = SquadWithPlayer(slot: 5, skill: 8.0);
        var finances = new Finances { BankBalance = 100_000 };

        RandomEventService.ResolveForeignTransferOffer(squad, finances, playerSlot: 5, offeredFee: 500_000, managerAccepts: false);

        Assert.NotNull(squad[5]);
        Assert.Equal(100_000, finances.BankBalance);
    }

    [Fact]
    public void ResolveForeignTransferOffer_Accept_UpdatesBankBalance()
    {
        var squad    = SquadWithPlayer(slot: 5, skill: 8.0);
        var finances = new Finances { BankBalance = 100_000 };

        RandomEventService.ResolveForeignTransferOffer(squad, finances, playerSlot: 5, offeredFee: 500_000, managerAccepts: true);

        Assert.Equal(600_000, finances.BankBalance);
    }

    [Fact]
    public void ResolveForeignTransferOffer_Accept_NullsSquadSlot()
    {
        var squad    = SquadWithPlayer(slot: 5, skill: 8.0);
        var finances = new Finances { BankBalance = 0 };

        RandomEventService.ResolveForeignTransferOffer(squad, finances, playerSlot: 5, offeredFee: 500_000, managerAccepts: true);

        Assert.Null(squad[5]);
    }

    [Fact]
    public void ResolveForeignTransferOffer_Accept_NewRecord_ReturnsPlayerName()
    {
        var squad    = SquadWithPlayer(slot: 5, skill: 8.0, name: "Smyth");
        var finances = new Finances { RecordSaleFee = 100_000 };

        var newRecord = RandomEventService.ResolveForeignTransferOffer(
            squad, finances, playerSlot: 5, offeredFee: 500_000, managerAccepts: true);

        Assert.Equal("Smyth", newRecord);
    }

    [Fact]
    public void ResolveForeignTransferOffer_Accept_NoNewRecord_ReturnsNull()
    {
        var squad    = SquadWithPlayer(slot: 5, skill: 8.0);
        var finances = new Finances { RecordSaleFee = 1_000_000 };

        var newRecord = RandomEventService.ResolveForeignTransferOffer(
            squad, finances, playerSlot: 5, offeredFee: 500_000, managerAccepts: true);

        Assert.Null(newRecord);
    }

    // ── ResolveTransferRequest ────────────────────────────────────────────────

    [Fact]
    public void ResolveTransferRequest_Accept_UpdatesBankBalance()
    {
        var squad    = SquadWithPlayer(slot: 5, skill: 6.0);
        var finances = new Finances { BankBalance = 50_000 };

        RandomEventService.ResolveTransferRequest(squad, finances, playerSlot: 5, requestedFee: 200_000, managerAccepts: true, new Random(0));

        Assert.Equal(250_000, finances.BankBalance);
    }

    [Fact]
    public void ResolveTransferRequest_Accept_NullsSquadSlot()
    {
        var squad    = SquadWithPlayer(slot: 5, skill: 6.0);
        var finances = new Finances();

        RandomEventService.ResolveTransferRequest(squad, finances, playerSlot: 5, requestedFee: 100_000, managerAccepts: true, new Random(0));

        Assert.Null(squad[5]);
    }

    [Fact]
    public void ResolveTransferRequest_Refuse_KeepsPlayerAndReducesSkill()
    {
        var squad    = SquadWithPlayer(slot: 5, skill: 8.0);
        double before = squad[5]!.Skill;
        var finances = new Finances();

        RandomEventService.ResolveTransferRequest(squad, finances, playerSlot: 5, requestedFee: 100_000, managerAccepts: false, new Random(0));

        Assert.NotNull(squad[5]);
        Assert.True(squad[5]!.Skill < before);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Player?[] SquadWithPlayer(int slot, double skill, string name = "Jones")
    {
        var squad  = new Player?[29];
        squad[slot] = new Player { Name = name, Skill = skill, Age = 25, Position = PlayerPosition.Attacker };
        return squad;
    }

    private static string[] MakeTeamNames()
    {
        var names = new string[81];
        for (int i = 1; i <= 80; i++) names[i] = $"Team{i:D2}";
        return names;
    }

    /// <summary>Finds a seed where rng.Next(maxValue) returns a value ≤ threshold on the first call.</summary>
    private static int FindSeedWhereFirstNextReturnsAtMost(int maxValue, int threshold)
    {
        for (int seed = 0; seed < 100_000; seed++)
            if (new Random(seed).Next(maxValue) <= threshold)
                return seed;
        throw new InvalidOperationException($"No seed found for Next({maxValue}) ≤ {threshold}");
    }

    /// <summary>
    /// Finds a seed where: rng.Next(35) is consumed (star roll), then rng.Next(10)==0 (retirement fires).
    /// </summary>
    private static int FindSeedForRetirementAnnouncement()
    {
        for (int seed = 0; seed < 100_000; seed++)
        {
            var rng = new Random(seed);
            rng.Next(35);              // consume starEventRoll
            if (rng.Next(10) == 0) return seed;
        }
        throw new InvalidOperationException("No seed found for retirement announcement");
    }
}
