using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class ScoutReportServiceTests
{
    private static readonly string[] TeamNames = MakeTeamNames();

    // ── RunWeeklyReports ──────────────────────────────────────────────────────

    [Fact]
    public void RunWeeklyReports_NoScouts_ReturnsEmptyFindings()
    {
        var result = ScoutReportService.RunWeeklyReports(
            scouts:            new List<Scout>(),
            currentScoutSlots: new Player?[] { null, null, null },
            allTeamNames:      TeamNames,
            rng:               new Random(0));

        Assert.Empty(result.Findings);
        Assert.Empty(result.SquadSlotUpdates);
    }

    [Fact]
    public void RunWeeklyReports_ScoutWithoutCriteria_ClearsSlot()
    {
        // HasCriteria is computed: requires LookingForPosition != None AND LookingForForm > 0
        var scouts = new List<Scout> { new() { LookingForPosition = PlayerPosition.None, LookingForForm = 0 } };

        var result = ScoutReportService.RunWeeklyReports(
            scouts,
            currentScoutSlots: new Player?[] { null, null, null },
            allTeamNames:      TeamNames,
            rng:               new Random(0));

        // Slot 21 should be set to null (cleared)
        Assert.True(result.SquadSlotUpdates.ContainsKey(21));
        Assert.Null(result.SquadSlotUpdates[21]);
    }

    [Fact]
    public void RunWeeklyReports_SlotAlreadyOccupied_Preserves()
    {
        var existingFind = new Player { Name = "Existing" };
        var scouts = new List<Scout>
        {
            new() { SkillPercent = 99, LookingForForm = 5, LookingForPosition = PlayerPosition.Defender }
        };

        var result = ScoutReportService.RunWeeklyReports(
            scouts,
            currentScoutSlots: new Player?[] { existingFind, null, null },
            allTeamNames:      TeamNames,
            rng:               new Random(0));

        // Slot 21 already occupied — should not be touched
        Assert.False(result.SquadSlotUpdates.ContainsKey(21));
    }

    [Fact]
    public void RunWeeklyReports_IsDeterministicWithSameSeed()
    {
        var scouts = new List<Scout>
        {
            new() { SkillPercent = 80, LookingForForm = 5, LookingForPosition = PlayerPosition.Attacker }
        };
        var slots = new Player?[] { null, null, null };

        var result1 = ScoutReportService.RunWeeklyReports(scouts, slots, TeamNames, new Random(42));
        var result2 = ScoutReportService.RunWeeklyReports(scouts, slots, TeamNames, new Random(42));

        Assert.Equal(result1.Findings.Count, result2.Findings.Count);
        Assert.Equal(result1.SquadSlotUpdates.Count, result2.SquadSlotUpdates.Count);
    }

    [Fact]
    public void RunWeeklyReports_FindingIncludesScoutIndex()
    {
        var scouts = new List<Scout>
        {
            new() { SkillPercent = 99, LookingForForm = 5, LookingForPosition = PlayerPosition.Attacker }
        };
        var slots = new Player?[] { null, null, null };

        var findResult = FindResultWithFindings(scouts, slots);

        Assert.NotNull(findResult);
        Assert.Equal(0, findResult.Findings[0].ScoutIndex);
    }

    [Fact]
    public void RunWeeklyReports_FindingPlayerIsInSlotUpdate()
    {
        var scouts = new List<Scout>
        {
            new() { SkillPercent = 99, LookingForForm = 5, LookingForPosition = PlayerPosition.Defender }
        };
        var slots = new Player?[] { null, null, null };

        var findResult = FindResultWithFindings(scouts, slots);

        Assert.NotNull(findResult);
        var finding = findResult.Findings[0];
        Assert.True(findResult.SquadSlotUpdates.ContainsKey(finding.SquadSlot));
        Assert.Same(finding.Player, findResult.SquadSlotUpdates[finding.SquadSlot]);
    }

    [Fact]
    public void RunWeeklyReports_DiscoveredPlayerHasQuotedAskingPrice()
    {
        var scouts = new List<Scout>
        {
            new() { SkillPercent = 99, LookingForForm = 5, LookingForPosition = PlayerPosition.Attacker }
        };
        var slots = new Player?[] { null, null, null };

        var findResult = FindResultWithFindings(scouts, slots);

        Assert.NotNull(findResult);
        Assert.True(findResult.Findings[0].Player.AskingPrice > 0);
    }

    [Fact]
    public void RunWeeklyReports_SecondScoutSlot_IsProcessed()
    {
        // Two scouts: first has no criteria (clears slot 21), second also has no criteria (clears slot 22)
        var scouts = new List<Scout>
        {
            new() { LookingForPosition = PlayerPosition.None, LookingForForm = 0 },
            new() { LookingForPosition = PlayerPosition.None, LookingForForm = 0 }
        };

        var result = ScoutReportService.RunWeeklyReports(
            scouts,
            currentScoutSlots: new Player?[] { null, null, null },
            allTeamNames:      TeamNames,
            rng:               new Random(0));

        Assert.True(result.SquadSlotUpdates.ContainsKey(21));
        Assert.True(result.SquadSlotUpdates.ContainsKey(22));
    }

    [Fact]
    public void RunWeeklyReports_ThirdScoutSlot_IsProcessed()
    {
        var scouts = new List<Scout>
        {
            new() { LookingForPosition = PlayerPosition.None, LookingForForm = 0 },
            new() { LookingForPosition = PlayerPosition.None, LookingForForm = 0 },
            new() { LookingForPosition = PlayerPosition.None, LookingForForm = 0 }
        };

        var result = ScoutReportService.RunWeeklyReports(
            scouts,
            currentScoutSlots: new Player?[] { null, null, null },
            allTeamNames:      TeamNames,
            rng:               new Random(0));

        Assert.True(result.SquadSlotUpdates.ContainsKey(21));
        Assert.True(result.SquadSlotUpdates.ContainsKey(22));
        Assert.True(result.SquadSlotUpdates.ContainsKey(23));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Sweeps seeds until RunWeeklyReports produces at least one finding.</summary>
    private ScoutRunResult FindResultWithFindings(List<Scout> scouts, Player?[] slots)
    {
        for (int seed = 0; seed < 10_000; seed++)
        {
            var r = ScoutReportService.RunWeeklyReports(scouts, slots, TeamNames, new Random(seed));
            if (r.Findings.Count > 0) return r;
        }
        throw new InvalidOperationException("No seed produced a scout finding in 10,000 attempts");
    }

    private static string[] MakeTeamNames()
    {
        var names = new string[81];
        for (int i = 1; i <= 80; i++) names[i] = $"Team{i:D2}";
        return names;
    }
}
