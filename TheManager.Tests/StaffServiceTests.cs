using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class StaffServiceTests
{
    // ── HireCoach ─────────────────────────────────────────────────────────────

    [Fact]
    public void HireCoach_WhenAlreadyHasCoach_ReturnsNull()
    {
        var club = new Club { HasCoach = true };
        Assert.Null(StaffService.HireCoach(club, new Random(0)));
    }

    [Fact]
    public void HireCoach_WhenAvailable_ReturnsCoach()
    {
        var club = new Club { HasCoach = false };
        var coach = StaffService.HireCoach(club, new Random(0));
        Assert.NotNull(coach);
    }

    [Fact]
    public void HireCoach_SetsHasCoachFlag()
    {
        var club = new Club { HasCoach = false };
        StaffService.HireCoach(club, new Random(0));
        Assert.True(club.HasCoach);
    }

    // ── SackCoach ─────────────────────────────────────────────────────────────

    [Fact]
    public void SackCoach_ClearsHasCoachFlag()
    {
        var club = new Club { HasCoach = true };
        StaffService.SackCoach(club);
        Assert.False(club.HasCoach);
    }

    // ── HirePhysio ────────────────────────────────────────────────────────────

    [Fact]
    public void HirePhysio_WhenAlreadyHasPhysio_ReturnsNull()
    {
        var club = new Club { HasPhysio = true };
        Assert.Null(StaffService.HirePhysio(club, new Random(0)));
    }

    [Fact]
    public void HirePhysio_WhenAvailable_SetsFlag()
    {
        var club = new Club { HasPhysio = false };
        StaffService.HirePhysio(club, new Random(0));
        Assert.True(club.HasPhysio);
    }

    // ── HireScout / SackScout ─────────────────────────────────────────────────

    [Fact]
    public void HireScout_WhenAtCapacity_ReturnsNull()
    {
        var club   = new Club { ScoutCount = 3 };
        var scouts = new List<Scout>();
        Assert.Null(StaffService.HireScout(club, scouts, new Random(0)));
    }

    [Fact]
    public void HireScout_AddsToListAndIncrementsCount()
    {
        var club   = new Club { ScoutCount = 1 };
        var scouts = new List<Scout> { new() };
        StaffService.HireScout(club, scouts, new Random(0));
        Assert.Equal(2, scouts.Count);
        Assert.Equal(2, club.ScoutCount);
    }

    [Fact]
    public void SackScout_RemovesFromListAndDecrementsCount()
    {
        var club   = new Club { ScoutCount = 2 };
        var scouts = new List<Scout> { new() { Name = "A" }, new() { Name = "B" } };
        StaffService.SackScout(club, scouts, index: 0);
        Assert.Single(scouts);
        Assert.Equal(1, club.ScoutCount);
    }

    [Fact]
    public void SackScout_InvalidIndex_ReturnsFalse()
    {
        var club   = new Club { ScoutCount = 1 };
        var scouts = new List<Scout> { new() };
        Assert.False(StaffService.SackScout(club, scouts, index: 5));
    }

    // ── HireYouthPlayer / SackYouthPlayer ─────────────────────────────────────

    [Fact]
    public void HireYouthPlayer_AtCapacity_ReturnsNull()
    {
        var club = new Club { YouthPlayerCount = 7 };
        Assert.Null(StaffService.HireYouthPlayer(club, new List<YouthPlayer>(), new Random(0)));
    }

    [Fact]
    public void HireYouthPlayer_WhenAccepted_AddsToTeamAndReturnsPlayer()
    {
        // GenerateYouthPlayer consumes several rng values before the accept/decline roll.
        // Sweep seeds to find one where the prospect accepts (1+rng.Next(3) != 3).
        var club  = new Club { YouthPlayerCount = 0 };
        var team  = new List<YouthPlayer>();
        int seed  = FindSeedWhereYouthAccepts();

        var result = StaffService.HireYouthPlayer(club, team, new Random(seed));

        Assert.NotNull(result);
        Assert.Single(team);
        Assert.Equal(1, club.YouthPlayerCount);
    }

    [Fact]
    public void SackYouthPlayer_RemovesPlayerAndDecrementsCount()
    {
        var club  = new Club { YouthPlayerCount = 2 };
        var youth = new List<YouthPlayer> { new() { Name = "A" }, new() { Name = "B" } };
        StaffService.SackYouthPlayer(club, youth, index: 0);
        Assert.Single(youth);
        Assert.Equal(1, club.YouthPlayerCount);
    }

    // ── PromoteYouthPlayer ────────────────────────────────────────────────────

    [Fact]
    public void PromoteYouthPlayer_IneligiblePlayer_ReturnsNull()
    {
        var squad = new Player?[29];
        var youth = new List<YouthPlayer> { new() { SkillPercent = 10 } };  // not eligible
        var club  = new Club { Division = Division.Two };

        Assert.Null(StaffService.PromoteYouthPlayer(squad, youth, club, youthIndex: 0, new Random(0)));
    }

    [Fact]
    public void PromoteYouthPlayer_NoFreeReserveSlot_ReturnsNull()
    {
        var squad = new Player?[29];
        // Fill all reserve slots (13-20)
        for (int i = 13; i <= 20; i++) squad[i] = new Player { Name = "Filler" };

        var youth = new List<YouthPlayer> { EligibleYouthPlayer() };
        var club  = new Club { Division = Division.Two };

        Assert.Null(StaffService.PromoteYouthPlayer(squad, youth, club, youthIndex: 0, new Random(0)));
    }

    [Fact]
    public void PromoteYouthPlayer_Success_PlacesPlayerInSquad()
    {
        var squad = new Player?[29];
        var youth = new List<YouthPlayer> { EligibleYouthPlayer() };
        var club  = new Club { Division = Division.Two };

        var result = StaffService.PromoteYouthPlayer(squad, youth, club, youthIndex: 0, new Random(0));

        Assert.NotNull(result);
        Assert.NotNull(squad[result.Value.slot]);
    }

    [Fact]
    public void PromoteYouthPlayer_Success_RemovesFromYouthTeam()
    {
        var squad = new Player?[29];
        var youth = new List<YouthPlayer> { EligibleYouthPlayer() };
        var club  = new Club { Division = Division.Two };

        StaffService.PromoteYouthPlayer(squad, youth, club, youthIndex: 0, new Random(0));

        Assert.Empty(youth);
    }

    [Fact]
    public void PromoteYouthPlayer_PotentialSkillExceedsPromotedSkill()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var squad = new Player?[29];
            var youth = new List<YouthPlayer> { EligibleYouthPlayer() };
            var club  = new Club { Division = Division.Two };

            var result = StaffService.PromoteYouthPlayer(squad, youth, club, youthIndex: 0, new Random(seed));

            Assert.NotNull(result);
            Assert.True(result.Value.player.PotentialSkill > result.Value.player.Skill,
                $"Seed {seed}: potential {result.Value.player.PotentialSkill} not above skill {result.Value.player.Skill}");
        }
    }

    [Fact]
    public void PromoteYouthPlayer_HighYouthPotential_CarriesOverToSeniorCeiling()
    {
        var squad = new Player?[29];
        var youthPlayer = EligibleYouthPlayer();
        youthPlayer.PotentialSkillPercent = 97;
        var youth = new List<YouthPlayer> { youthPlayer };
        var club  = new Club { Division = Division.Two };

        var result = StaffService.PromoteYouthPlayer(squad, youth, club, youthIndex: 0, new Random(0));

        Assert.NotNull(result);
        Assert.Equal(9.7, result.Value.player.PotentialSkill);
    }

    [Fact]
    public void PromoteYouthPlayer_PeakAgeBetween26And30()
    {
        var squad = new Player?[29];
        var youth = new List<YouthPlayer> { EligibleYouthPlayer() };
        var club  = new Club { Division = Division.Two };

        var result = StaffService.PromoteYouthPlayer(squad, youth, club, youthIndex: 0, new Random(0));

        Assert.NotNull(result);
        Assert.InRange(result.Value.player.PeakAge, 26, 30);
    }

    // ── TotalStaffWageBill ────────────────────────────────────────────────────

    [Fact]
    public void TotalStaffWageBill_SumsAllWages()
    {
        var coach  = new Coach  { WeeklySalary = 200 };
        var physio = new Physio { WeeklySalary = 150 };
        var scouts = new List<Scout>       { new() { WeeklySalary = 100 }, new() { WeeklySalary = 100 } };
        var youth  = new List<YouthPlayer> { new() { WeeklySalary = 50  } };

        double total = StaffService.TotalStaffWageBill(coach, physio, scouts, youth);

        Assert.Equal(600, total);
    }

    [Fact]
    public void TotalStaffWageBill_NoStaff_ReturnsZero()
    {
        double total = StaffService.TotalStaffWageBill(
            null, null,
            new List<Scout>(), new List<YouthPlayer>());

        Assert.Equal(0, total);
    }

    // ── CheckRandomResignation ────────────────────────────────────────────────

    [Fact]
    public void CheckRandomResignation_NoStaff_ReturnsNoneEvenOnFiringRoll()
    {
        // Find a seed where 1+rng.Next(99) == 8
        var club = new Club { HasCoach = false, HasPhysio = false, ScoutCount = 0 };
        int firingSeed = FindSeedForRoll(7, 99);

        var result = StaffService.CheckRandomResignation(
            club, coach: null, physio: null, scouts: new List<Scout>(), new Random(firingSeed));

        Assert.Equal(ResignationType.None, result.Type);
    }

    [Fact]
    public void CheckRandomResignation_CoachResigns_ClearsCoachFlag()
    {
        var club  = new Club { HasCoach = true, HasPhysio = false, ScoutCount = 0 };
        var coach = new Coach { Name = "Hill" };
        int seed  = FindSeedForStaffResignation(staffSlot: 1);

        var result = StaffService.CheckRandomResignation(
            club, coach, physio: null, scouts: new List<Scout>(), new Random(seed));

        Assert.Equal(ResignationType.Coach, result.Type);
        Assert.False(club.HasCoach);
        Assert.Equal("Hill", result.ResignerName);
    }

    [Fact]
    public void CheckRandomResignation_PhysioResigns_ClearsPhysioFlag()
    {
        var club   = new Club { HasCoach = false, HasPhysio = true, ScoutCount = 0 };
        var physio = new Physio { Name = "Adams" };
        int seed   = FindSeedForStaffResignation(staffSlot: 2);

        var result = StaffService.CheckRandomResignation(
            club, coach: null, physio, scouts: new List<Scout>(), new Random(seed));

        Assert.Equal(ResignationType.Physio, result.Type);
        Assert.False(club.HasPhysio);
        Assert.Equal("Adams", result.ResignerName);
    }

    [Fact]
    public void CheckRandomResignation_ScoutResigns_RemovesFromList()
    {
        var scouts = new List<Scout> { new() { Name = "Peters" } };
        var club   = new Club { HasCoach = false, HasPhysio = false, ScoutCount = 1 };
        int seed   = FindSeedForStaffResignation(staffSlot: 3);

        var result = StaffService.CheckRandomResignation(
            club, coach: null, physio: null, scouts, new Random(seed));

        Assert.Equal(ResignationType.Scout, result.Type);
        Assert.Empty(scouts);
        Assert.Equal(0, club.ScoutCount);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static YouthPlayer EligibleYouthPlayer() => new()
    {
        Name         = "Youth",
        Position     = PlayerPosition.Defender,
        SkillPercent = 60,   // IsEligibleForPromotion requires SkillPercent >= 50
        Age          = 17
    };

    /// <summary>Finds a Random seed where rng.Next(maxValue) returns targetValue on the first call.</summary>
    private static int FindSeedForRoll(int targetValue, int maxValue)
    {
        for (int seed = 0; seed < 10_000; seed++)
            if (new Random(seed).Next(maxValue) == targetValue)
                return seed;
        throw new InvalidOperationException($"Could not find seed for roll {targetValue}/{maxValue}");
    }

    /// <summary>Finds a seed where: first Next(99)==7 (roll=8) AND second Next(5)+1==staffSlot.</summary>
    private static int FindSeedForStaffResignation(int staffSlot)
    {
        for (int seed = 0; seed < 100_000; seed++)
        {
            var rng = new Random(seed);
            if (rng.Next(99) == 7 && rng.Next(5) + 1 == staffSlot)
                return seed;
        }
        throw new InvalidOperationException($"Could not find seed for staff slot {staffSlot}");
    }

    /// <summary>Finds a seed where HireYouthPlayer's accept/decline roll accepts (!=3).</summary>
    private static int FindSeedWhereYouthAccepts()
    {
        for (int seed = 0; seed < 10_000; seed++)
        {
            var club  = new Club { YouthPlayerCount = 0 };
            var team  = new List<YouthPlayer>();
            var result = StaffService.HireYouthPlayer(club, team, new Random(seed));
            if (result is not null) return seed;
        }
        throw new InvalidOperationException("Could not find seed where youth player accepts");
    }
}
