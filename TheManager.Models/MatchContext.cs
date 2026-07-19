namespace TheManager.Models;

/// <summary>
/// Carry-over state produced by <see cref="GameService"/> after a match and consumed
/// by <see cref="WeeklyTickService"/> in the same turn.
///
/// Replaces the private fields that previously coupled PlayMatch to WeeklyUpdate
/// inside GameService.
/// </summary>
public record MatchContext(
    bool WonLeagueMatch,
    bool WonCupMatch,
    bool LostLastMatch,
    bool WasHomeGame,
    int  OpponentLeaguePosition,
    bool IsCupMatch = false);
