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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameState MakeGameState(Random rng)
    {
        var state = new GameState();
        InitializationService.SetupNewGame(state, "TESTFC", Division.Four, "Manager", rng);
        state.CurrentLeague = LeagueService.InitialiseTable(state.Club.Division, state.AllTeamNames);
        return state;
    }

    private static MatchContext MakeContext() => new(
        WonLeagueMatch: false,
        WonCupMatch: false,
        LostLastMatch: false,
        WasHomeGame: false,
        OpponentLeaguePosition: 1);
}
