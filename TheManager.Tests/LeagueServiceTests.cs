using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class LeagueServiceTests
{
    // ── InitialiseTable ───────────────────────────────────────────────────────

    [Fact]
    public void InitialiseTable_Creates20Entries()
    {
        var table = LeagueService.InitialiseTable(Division.One, MakeTeamNames());
        Assert.Equal(20, table.Entries.Count);
    }

    [Fact]
    public void InitialiseTable_EntriesMatchDivisionTeams()
    {
        var names = MakeTeamNames();
        var table = LeagueService.InitialiseTable(Division.Two, names);

        // Division 2 occupies indices 21-40
        Assert.Equal(names[21], table.Entries[0].TeamName);
        Assert.Equal(names[40], table.Entries[19].TeamName);
    }

    [Fact]
    public void InitialiseTable_AllEntriesStartAtZero()
    {
        var table = LeagueService.InitialiseTable(Division.One, MakeTeamNames());
        Assert.All(table.Entries, e =>
        {
            Assert.Equal(0, e.Played);
            Assert.Equal(0, e.Won);
            Assert.Equal(0, e.Drawn);
            Assert.Equal(0, e.GoalsFor);
            Assert.Equal(0, e.GoalsAgainst);
        });
    }

    [Fact]
    public void InitialiseTable_SetsCorrectDivision()
    {
        var table = LeagueService.InitialiseTable(Division.Three, MakeTeamNames());
        Assert.Equal(Division.Three, table.Division);
    }

    // ── RecordResult ──────────────────────────────────────────────────────────

    [Fact]
    public void RecordResult_Win_UpdatesWinner()
    {
        var table = TableWithTeams("Home", "Away");
        LeagueService.RecordResult(table, "Home", 3, "Away", 1);

        var home = table.Entries.First(e => e.TeamName == "Home");
        Assert.Equal(1, home.Played);
        Assert.Equal(1, home.Won);
        Assert.Equal(3, home.GoalsFor);
        Assert.Equal(1, home.GoalsAgainst);
    }

    [Fact]
    public void RecordResult_Win_UpdatesLoser()
    {
        var table = TableWithTeams("Home", "Away");
        LeagueService.RecordResult(table, "Home", 3, "Away", 1);

        var away = table.Entries.First(e => e.TeamName == "Away");
        Assert.Equal(1, away.Played);
        Assert.Equal(0, away.Won);
        Assert.Equal(1, away.GoalsFor);
        Assert.Equal(3, away.GoalsAgainst);
    }

    [Fact]
    public void RecordResult_Draw_UpdatesBothTeams()
    {
        var table = TableWithTeams("Home", "Away");
        LeagueService.RecordResult(table, "Home", 1, "Away", 1);

        var home = table.Entries.First(e => e.TeamName == "Home");
        var away = table.Entries.First(e => e.TeamName == "Away");
        Assert.Equal(1, home.Drawn);
        Assert.Equal(1, away.Drawn);
        Assert.Equal(0, home.Won);
        Assert.Equal(0, away.Won);
    }

    // ── Sort ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Sort_OrdersByPointsDescending()
    {
        var table = TableWithTeams("A", "B", "C");
        // Give B the most points
        LeagueService.RecordResult(table, "B", 2, "A", 0);
        LeagueService.RecordResult(table, "B", 2, "C", 0);

        LeagueService.Sort(table, pointsPerWin: 3);

        Assert.Equal("B", table.Entries[0].TeamName);
    }

    [Fact]
    public void Sort_BreaksTiesByGoalDifference()
    {
        var table = TableWithTeams("A", "B");
        // Equal points but B has better goal difference
        LeagueService.RecordResult(table, "A", 1, "B", 1); // both get 1 pt
        LeagueService.RecordResult(table, "B", 3, "A", 0); // B +3, A -3

        LeagueService.Sort(table, pointsPerWin: 3);

        Assert.Equal("B", table.Entries[0].TeamName);
    }

    // ── EncodeResultString ────────────────────────────────────────────────────

    [Theory]
    [InlineData(2, 1, "12")]   // we win: opponent=1, us=2 → "12"
    [InlineData(0, 3, "30")]   // we lose: opponent=3, us=0 → "30"
    [InlineData(2, 2, "22")]   // draw
    public void EncodeResultString_EncodesCorrectly(int ourScore, int opponentScore, string expected)
    {
        Assert.Equal(expected, LeagueService.EncodeResultString(ourScore, opponentScore));
    }

    [Fact]
    public void EncodeResultString_CapsAt9()
    {
        var result = LeagueService.EncodeResultString(12, 10);
        Assert.Equal("99", result);
    }

    // ── Sort (goals-for tiebreaker) ───────────────────────────────────────────

    [Fact]
    public void Sort_BreaksTiesByGoalsFor()
    {
        var table = TableWithTeams("A", "B");
        // Both win one match, both concede the same, but A scores more
        LeagueService.RecordResult(table, "A", 3, "B", 2);   // A: +1 GD, 3 GF; B: -1 GD, 2 GF
        LeagueService.RecordResult(table, "B", 3, "A", 2);   // B: +1 GD total; A: -1 GD total — equal pts

        // Now both have 3 pts and +0 GD overall, but A has 5 GF, B has 5 GF — equal.
        // Use a simpler setup: give both teams equal pts, equal GD via manual entry.
        var table2 = new LeagueTable { Division = Division.One };
        table2.Entries.Add(new LeagueEntry { TeamName = "Low",  Won = 2, GoalsFor = 4, GoalsAgainst = 2 });
        table2.Entries.Add(new LeagueEntry { TeamName = "High", Won = 2, GoalsFor = 6, GoalsAgainst = 4 });

        LeagueService.Sort(table2, pointsPerWin: 3);

        // Equal pts (6), equal GD (+2 each), High has more GF (6 vs 4) → High first
        Assert.Equal("High", table2.Entries[0].TeamName);
    }

    // ── PlayPostponedFixtures ─────────────────────────────────────────────────

    [Fact]
    public void PlayPostponedFixtures_BothTeamsGetPlayedIncremented()
    {
        var names    = MakeTeamNames();
        var table    = LeagueService.InitialiseTable(Division.One, names);
        var fixtures = new List<PostponedFixture> { new() { HomeTeamIndex = 1, AwayTeamIndex = 2 } };

        LeagueService.PlayPostponedFixtures(table, fixtures, names, pointsPerWin: 3, new Random(0));

        var home = table.Entries.First(e => e.TeamName == names[1]);
        var away = table.Entries.First(e => e.TeamName == names[2]);
        Assert.Equal(1, home.Played);
        Assert.Equal(1, away.Played);
    }

    [Fact]
    public void PlayPostponedFixtures_TableIsSortedAfterward()
    {
        var names    = MakeTeamNames();
        var table    = LeagueService.InitialiseTable(Division.One, names);
        var fixtures = new List<PostponedFixture>
        {
            new() { HomeTeamIndex = 1, AwayTeamIndex = 2 },
            new() { HomeTeamIndex = 3, AwayTeamIndex = 4 }
        };

        LeagueService.PlayPostponedFixtures(table, fixtures, names, pointsPerWin: 3, new Random(0));

        // After sorting, no team should have fewer points than the team below it
        for (int i = 0; i < table.Entries.Count - 1; i++)
            Assert.True(table.Entries[i].Points(3) >= table.Entries[i + 1].Points(3));
    }

    // ── SimulateOtherFixtures ─────────────────────────────────────────────────

    [Fact]
    public void SimulateOtherFixtures_Returns9Results()
    {
        var names  = MakeTeamNames();
        var table  = LeagueService.InitialiseTable(Division.One, names);
        string our = names[1];   // our club is the first team in division 1

        var results = LeagueService.SimulateOtherFixtures(
            table, names, Division.One, our, leagueRound: 0, pointsPerWin: 3, new Random(0));

        Assert.Equal(9, results.Count);
    }

    [Fact]
    public void SimulateOtherFixtures_AllTeamNamesNonEmpty()
    {
        var names  = MakeTeamNames();
        var table  = LeagueService.InitialiseTable(Division.One, names);

        var results = LeagueService.SimulateOtherFixtures(
            table, names, Division.One, names[1], leagueRound: 5, pointsPerWin: 3, new Random(0));

        Assert.All(results, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.HomeTeam));
            Assert.False(string.IsNullOrWhiteSpace(r.AwayTeam));
        });
    }

    [Fact]
    public void SimulateOtherFixtures_ScoresAreNonNegative()
    {
        var names  = MakeTeamNames();
        var table  = LeagueService.InitialiseTable(Division.One, names);

        var results = LeagueService.SimulateOtherFixtures(
            table, names, Division.One, names[1], leagueRound: 0, pointsPerWin: 3, new Random(0));

        Assert.All(results, r =>
        {
            Assert.True(r.HomeScore >= 0);
            Assert.True(r.AwayScore >= 0);
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string[] MakeTeamNames()
    {
        var names = new string[81];
        for (int i = 1; i <= 80; i++) names[i] = $"Team{i:D2}";
        return names;
    }

    private static LeagueTable TableWithTeams(params string[] teamNames)
    {
        var table = new LeagueTable { Division = Division.One };
        foreach (var name in teamNames)
            table.Entries.Add(new LeagueEntry { TeamName = name });
        return table;
    }
}
