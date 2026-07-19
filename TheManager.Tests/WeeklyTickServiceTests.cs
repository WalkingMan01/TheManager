using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class WeeklyTickServiceTests
{
    // ── Injury / suspension countdown ────────────────────────────────────────

    [Fact]
    public void Process_PlayerWithWeeksInjured_DecrementsByOne()
    {
        var state  = MakeGameState(new Random(1));
        var player = state.Squad[1]!;
        player.WeeksInjured = 4;

        WeeklyTickService.Process(state, MakeContext(), new Random(1));

        Assert.Equal(3, player.WeeksInjured);
    }

    [Fact]
    public void Process_PlayerWithZeroWeeksInjured_StaysAtZero()
    {
        var state  = MakeGameState(new Random(2));
        var player = state.Squad[1]!;
        player.WeeksInjured = 0;

        WeeklyTickService.Process(state, MakeContext(), new Random(2));

        Assert.Equal(0, player.WeeksInjured);
    }

    [Fact]
    public void Process_PlayerWithSuspensionMatchesRemaining_DecrementsByOne()
    {
        var state  = MakeGameState(new Random(3));
        var player = state.Squad[1]!;
        player.SuspensionMatchesRemaining = 2;

        WeeklyTickService.Process(state, MakeContext(), new Random(3));

        Assert.Equal(1, player.SuspensionMatchesRemaining);
    }

    [Fact]
    public void Process_PlayerWithZeroSuspensionMatchesRemaining_StaysAtZero()
    {
        var state  = MakeGameState(new Random(4));
        var player = state.Squad[1]!;
        player.SuspensionMatchesRemaining = 0;

        WeeklyTickService.Process(state, MakeContext(), new Random(4));

        Assert.Equal(0, player.SuspensionMatchesRemaining);
    }

    // ── Appearances ───────────────────────────────────────────────────────────

    [Fact]
    public void Process_Matchday_CreditsStartersWithAGamePlayed()
    {
        var state  = MakeGameState(new Random(20));
        int before = state.Squad[1]!.GamesPlayed;

        WeeklyTickService.Process(state, MakeContext(), new Random(20));

        Assert.Equal(before + 1, state.Squad[1]!.GamesPlayed);
    }

    [Fact]
    public void Process_RestDay_DoesNotCreditAnAppearance()
    {
        var state  = MakeGameState(new Random(21));
        int before = state.Squad[1]!.GamesPlayed;

        WeeklyTickService.Process(state, MakeContext() with { MatchPlayed = false }, new Random(21));

        Assert.Equal(before, state.Squad[1]!.GamesPlayed);
    }

    // ── Finance report ────────────────────────────────────────────────────────

    [Fact]
    public void Process_HomeGame_ReportsGateReceiptsAsAttendanceTimesTicketPrice()
    {
        var state  = MakeGameState(new Random(10));
        var result = WeeklyTickService.Process(state, MakeContext(wasHomeGame: true), new Random(10));

        Assert.NotNull(result.FinanceReport);
        Assert.True(result.Attendance > 0);
        Assert.Equal(result.Attendance * state.Club.TicketPriceInPounds, result.GateMoney);
        Assert.Equal(result.GateMoney, result.FinanceReport.GateMoney);
        Assert.Equal(result.GateMoney, state.Finances.LastMatchGateMoney);
        Assert.True(result.FinanceReport.PlayerWageBill > 0);
    }

    [Fact]
    public void Process_AwayGame_ReportsNoGateMoney()
    {
        var state  = MakeGameState(new Random(11));
        var result = WeeklyTickService.Process(state, MakeContext(wasHomeGame: false), new Random(11));

        Assert.Equal(0, result.Attendance);
        Assert.Equal(0, result.FinanceReport.GateMoney);
    }

    [Fact]
    public void Process_HomeCupTie_DrawsBiggerCrowdThanTheSameLeagueMatch()
    {
        // Identical states and identical rng seeds: the only difference is the
        // cup flag, so the cup crowd must be the boosted one (spec: fa-cup.md).
        var leagueState = MakeGameState(new Random(12));
        var cupState    = MakeGameState(new Random(12));

        var league = WeeklyTickService.Process(
            leagueState, MakeContext(wasHomeGame: true), new Random(99));
        var cup    = WeeklyTickService.Process(
            cupState, MakeContext(wasHomeGame: true, isCupMatch: true), new Random(99));

        Assert.True(cup.Attendance > league.Attendance);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameState MakeGameState(Random rng)
    {
        var state = new GameState();
        InitializationService.SetupNewGame(state, "TESTFC", Division.Four, "Manager", rng);
        state.CurrentLeague = LeagueService.InitialiseTable(state.Club.Division, state.AllTeamNames);
        return state;
    }

    private static MatchContext MakeContext(bool wasHomeGame = false, bool isCupMatch = false) => new(
        WonLeagueMatch: false,
        WonCupMatch: false,
        LostLastMatch: false,
        WasHomeGame: wasHomeGame,
        OpponentLeaguePosition: 1,
        IsCupMatch: isCupMatch);
}
