using System.Text.Json;
using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

/// <summary>
/// Ground capacity model (docs/specs/gate-receipts-ground-capacity.md):
/// seeding from the real-ground table, the Division One occupancy bands,
/// the lower-division demand clamp, and save migration.
/// </summary>
public class GroundCapacityTests
{
    // ── Seeding ───────────────────────────────────────────────────────────────

    [Fact]
    public void SetupNewGame_KnownClub_GetsItsRealGround()
    {
        var state = MakeState("Arsenal", Division.One, new Random(1));

        Assert.Equal("Emirates Stadium", state.Club.GroundName);
        Assert.Equal(60_700, state.Club.GroundCapacity);
    }

    [Fact]
    public void SetupNewGame_UnknownClub_GetsJitteredDivisionFallback()
    {
        var state = MakeState("TESTFC", Division.Four, new Random(2));

        Assert.Equal("TESTFC Stadium", state.Club.GroundName);
        Assert.InRange(state.Club.GroundCapacity, 7_200, 8_800);   // 8,000 ±10%
        Assert.Equal(0, state.Club.GroundCapacity % 100);
    }

    [Fact]
    public void SetupNewGame_UnknownClub_IsDeterministicForAGivenSeed()
    {
        var a = MakeState("TESTFC", Division.Two, new Random(3));
        var b = MakeState("TESTFC", Division.Two, new Random(3));

        Assert.Equal(a.Club.GroundCapacity, b.Club.GroundCapacity);
    }

    // ── Ticket price (docs/specs/player-wage-scaling.md) ───────────────────────

    [Theory]
    [InlineData(Division.One,   48.0)]
    [InlineData(Division.Two,   36.0)]
    [InlineData(Division.Three, 24.0)]
    [InlineData(Division.Four,  12.0)]
    public void SetupNewGame_TicketPriceIsScaledByDivision(Division division, double expected)
    {
        var state = MakeState("TESTFC", division, new Random(0));
        Assert.Equal(expected, state.Club.TicketPriceInPounds);
    }

    [Fact]
    public void EveryLeagueClub_HasARealGroundEntry()
    {
        foreach (Division division in new[] { Division.One, Division.Two, Division.Three, Division.Four })
        foreach (string club in TeamData.GetDivisionTeams(division))
        {
            Assert.True(TeamData.TryGetGround(club, out _, out int capacity),
                $"no ground entry for {club}");
            Assert.True(capacity > 0, $"non-positive capacity for {club}");
        }
    }

    // ── Division One occupancy ────────────────────────────────────────────────

    [Theory]
    [InlineData(1,  0.9799, 1.0000)]   // 98.0–100% band (int truncation allows −1 spectator)
    [InlineData(10, 0.9349, 0.9550)]   // 93.5–95.5% pins the −0.5%/place step
    [InlineData(20, 0.8849, 0.9050)]   // 88.5–90.5%
    public void DivisionOne_AttendanceLandsInThePositionsOccupancyBand(
        int position, double minFraction, double maxFraction)
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var (state, capacity) = MakeDivisionOneState(position, seed);
            var result = WeeklyTickService.Process(state, HomeContext(), new Random(seed));

            Assert.InRange(result.Attendance, capacity * minFraction, capacity * maxFraction);
            Assert.True(result.Attendance <= capacity);
        }
    }

    [Fact]
    public void DivisionOne_DifferentSeeds_GiveDifferentAttendances()
    {
        var attendances = new HashSet<double>();
        for (int seed = 0; seed < 10; seed++)
        {
            var (state, _) = MakeDivisionOneState(position: 5, seed);
            attendances.Add(WeeklyTickService.Process(state, HomeContext(), new Random(seed)).Attendance);
        }

        Assert.True(attendances.Count > 1, "occupancy band produced identical draws for every seed");
    }

    [Fact]
    public void DivisionOne_HomeCupTieAtTheTop_FillsTheGround()
    {
        // 1st place band is 98–100%; the +2% cup bump always caps at 100%.
        var (state, capacity) = MakeDivisionOneState(position: 1, seed: 7);
        var result = WeeklyTickService.Process(state, HomeContext(isCupMatch: true), new Random(7));

        Assert.Equal(capacity, result.Attendance);
    }

    // ── Divisions Two–Four: occupancy bands with a 60% floor ──────────────────

    [Theory]
    [InlineData(1,  0.9799, 1.0000)]   // top of the league: 98–100%, same as Division One
    [InlineData(12, 0.7981, 0.8183)]   // mid-table: ceiling 100 − 38 × 11/23 ≈ 81.8%
    [InlineData(24, 0.5999, 0.6201)]   // bottom: 60–62% — never below 60% of capacity
    public void LowerDivision_AttendanceLandsInThePositionsOccupancyBand(
        int position, double minFraction, double maxFraction)
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var (state, capacity) = MakeStateAtPosition("Barnsley", Division.Three, position, seed);
            var result = WeeklyTickService.Process(state, HomeContext(), new Random(seed));

            Assert.InRange(result.Attendance, capacity * minFraction, capacity * maxFraction);
            Assert.True(result.Attendance <= capacity);
        }
    }

    [Fact]
    public void LowerDivision_BottomOfTheLeague_NeverDrawsBelow60PercentOfCapacity()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var (state, capacity) = MakeStateAtPosition("York City", Division.Four, position: 24, seed);
            var result = WeeklyTickService.Process(state, HomeContext(), new Random(seed));

            Assert.True(result.Attendance >= capacity * 0.599,
                $"seed {seed}: {result.Attendance} is below 60% of {capacity}");
        }
    }

    [Fact]
    public void LowerDivision_HomeCupTie_BumpsOccupancyByTwoPoints()
    {
        // Bottom-of-Division-Three band is 60–62%; the cup bump lifts it to 62–64%.
        var (state, capacity) = MakeStateAtPosition("Barnsley", Division.Three, position: 24, seed: 6);
        var result = WeeklyTickService.Process(state, HomeContext(isCupMatch: true), new Random(6));

        Assert.InRange(result.Attendance, capacity * 0.6199, capacity * 0.6401);
    }

    [Fact]
    public void HomeGame_GateEqualsAttendanceTimesTicketPrice()
    {
        var (state, _) = MakeStateAtPosition("Barnsley", Division.Three, position: 5, seed: 8);

        var result = WeeklyTickService.Process(state, HomeContext(), new Random(8));

        Assert.Equal(result.Attendance * state.Club.TicketPriceInPounds, result.GateMoney);
        Assert.Equal(result.GateMoney, result.FinanceReport.GateMoney);
    }

    // ── Immutability ──────────────────────────────────────────────────────────

    [Fact]
    public void PurchasingGroundImprovement_LeavesCapacityUnchanged()
    {
        var state = MakeState("Arsenal", Division.One, new Random(9));
        state.Club.GroundImprovementCost = GroundImprovementService.CalculateCost(Division.One);
        state.Finances.BankBalance = 5_000_000;
        int before = state.Club.GroundCapacity;

        var purchase = GroundImprovementService.PurchaseImprovement(state.Club, state.Finances);

        Assert.True(purchase.Success);
        Assert.Equal(before, state.Club.GroundCapacity);
    }

    // ── Save migration ────────────────────────────────────────────────────────

    [Fact]
    public void Deserialize_PreCapacitySave_SeedsKnownClubFromTheRealTable()
    {
        var state = new GameState();
        TeamData.Seed(state);
        state.Club.Name     = "Liverpool";
        state.Club.Division = Division.One;
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(10));

        Assert.Equal("Anfield", loaded.Club.GroundName);
        Assert.Equal(61_300, loaded.Club.GroundCapacity);
    }

    [Fact]
    public void Deserialize_PreCapacitySaveWithUnknownClub_SeedsDeterministicFallback()
    {
        var state = new GameState();
        TeamData.Seed(state);
        state.Club.Name     = "TESTFC";
        state.Club.Division = Division.Three;
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(11));

        Assert.Equal("TESTFC Stadium", loaded.Club.GroundName);
        Assert.Equal(Constants.FallbackGroundCapacity(Division.Three), loaded.Club.GroundCapacity);
    }

    [Fact]
    public void Deserialize_ModernSave_RoundTripsGroundUnchanged()
    {
        var state = new GameState();
        TeamData.Seed(state);
        state.Club.GroundName     = "Custom Park";
        state.Club.GroundCapacity = 12_345;
        string json = JsonSerializer.Serialize(state, SaveLoadService.SerializerOptions);

        var loaded = SaveLoadService.Deserialize(json, new Random(12));

        Assert.Equal("Custom Park", loaded.Club.GroundName);
        Assert.Equal(12_345, loaded.Club.GroundCapacity);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameState MakeState(string clubName, Division division, Random rng)
    {
        var state = new GameState();
        InitializationService.SetupNewGame(state, clubName, division, "Manager", rng);
        state.CurrentLeague = LeagueService.InitialiseTable(state.Club.Division, state.AllTeamNames);
        return state;
    }

    /// <summary>State for a known club, moved to the given league position.</summary>
    private static (GameState State, int Capacity) MakeStateAtPosition(
        string clubName, Division division, int position, int seed)
    {
        var state = MakeState(clubName, division, new Random(seed));

        var entries = state.CurrentLeague.Entries;
        int index   = entries.FindIndex(e => e.TeamName.Trim() == clubName);
        var entry   = entries[index];
        entries.RemoveAt(index);
        entries.Insert(position - 1, entry);

        return (state, state.Club.GroundCapacity);
    }

    private static (GameState State, int Capacity) MakeDivisionOneState(int position, int seed)
        => MakeStateAtPosition("Arsenal", Division.One, position, seed);

    private static MatchContext HomeContext(bool isCupMatch = false) => new(
        WonLeagueMatch: false,
        WonCupMatch: false,
        LostLastMatch: false,
        WasHomeGame: true,
        OpponentLeaguePosition: 1,
        IsCupMatch: isCupMatch);
}
