using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Manages the week-by-week fixture cycle, determining match type, opponent,
/// and home/away status for each of the 38 league weeks plus cup weeks.
///
/// The BASIC cycle runs from CI=1 to CI=59:
///   CI = 12, 19, 26, 33, 40, 47, 54 → League Cup fixture weeks
///   CI = 16, 23, 30, 37, 44, 51, 58 → FA Cup fixture weeks
///   CI = 59                          → end-of-season trigger
///   All other values                 → league match
///
/// Home/away alternates based on matches remaining (cJ):
///   Even cJ (38, 36, 34 …) → home  (BK%=1)
///   Odd  cJ                → away  (BK%=2)
///
/// Corresponds to lines 422–430, 1701–1724 in FOOT.BAS.
/// </summary>
public static class FixtureSchedulerService
{
    // Cup weeks are fixed offsets within the 59-week season cycle
    private static readonly HashSet<int> LeagueCupWeeks = new() { 12, 19, 26, 33, 40, 47, 54 };
    private static readonly HashSet<int> FACupWeeks     = new() { 16, 23, 30, 37, 44, 51, 58 };

    // ── Determine next scheduled match ───────────────────────────────────────

    /// <summary>
    /// Returns the type, opponent, and home/away flag for the current week.
    ///
    /// BASIC lines 422–430 and 1701–1724:
    ///   Cup weeks are detected by CI value.
    ///   For league weeks V cycles through teams in the division (subroutine 887).
    ///   Home/away is determined by cJ being even (lines 1711–1712).
    /// </summary>
    public static ScheduledMatch GetCurrentMatch(GameState gameState)
    {
        int week = gameState.CurrentWeek;

        if (week >= 59)
            return new ScheduledMatch { MatchType = MatchType.EndOfSeason, Week = week };

        // ── Cup weeks ─────────────────────────────────────────────────────────
        if (LeagueCupWeeks.Contains(week))
            return BuildCupMatch(gameState, MatchType.LeagueCup, week);

        if (FACupWeeks.Contains(week))
            return BuildCupMatch(gameState, MatchType.FACup, week);

        // ── League week ───────────────────────────────────────────────────────
        return BuildLeagueMatch(gameState, week);
    }

    /// <summary>
    /// Advances CI by one week and decrements the matches-remaining counter.
    /// Call this after a match result has been processed.
    ///
    /// BASIC lines 422–424:
    ///   CI=CI+1; if NC=0 then skip to weekly news; else GOTO 1701.
    /// </summary>
    public static void AdvanceWeek(GameState gameState)
    {
        gameState.CurrentWeek++;
        gameState.FixturesPlayed = Math.Min(38, gameState.FixturesPlayed + 1);
        gameState.MatchesRemainingThisSeason = 38 - (gameState.FixturesPlayed - 1);
    }

    /// <summary>
    /// Initialises the opponent pointer for a fresh season or after a club change.
    /// Sets V to the first team in the current division (cM = AP*20 − 19).
    ///
    /// BASIC subroutine 23000 (line 4676): V=cM.
    /// </summary>
    public static void ResetOpponentPointer(GameState gameState)
    {
        gameState.CurrentOpponentIndex = DivisionStart(gameState.Club.Division);
    }

    // ── Cup match override (subroutine 152 / 416) ─────────────────────────────

    /// <summary>
    /// Returns true when the current week is a cup week that the player's club
    /// has a fixture in (i.e. they have not been eliminated).
    ///
    /// BASIC lines 1701–1708: checks CI against cup week ranges and tests
    /// whether CT (LC) or CR (FA) is non-zero.
    /// </summary>
    public static bool HasCupFixtureThisWeek(GameState gameState)
    {
        int week = gameState.CurrentWeek;

        if (LeagueCupWeeks.Contains(week))
            return gameState.LeagueCup.CurrentRound != CupRound.NotEntered;

        if (FACupWeeks.Contains(week))
            return gameState.FACup.CurrentRound != CupRound.NotEntered;

        return false;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static ScheduledMatch BuildLeagueMatch(GameState gameState, int week)
    {
        string opponentName  = AdvanceOpponentPointer(gameState);
        bool   isHomeGame    = DetermineHomeAway(gameState.MatchesRemainingThisSeason);

        return new ScheduledMatch
        {
            MatchType             = MatchType.League,
            Week                  = week,
            OpponentName          = opponentName,
            OpponentTeamIndex     = gameState.CurrentOpponentIndex,
            IsHomeGame            = isHomeGame
        };
    }

    private static ScheduledMatch BuildCupMatch(GameState gameState, MatchType cupType, int week)
    {
        // Opponent for cup comes from the cup draw, not from the opponent pointer.
        // Caller retrieves it from CupCompetition.CurrentRoundFixtures.
        return new ScheduledMatch
        {
            MatchType  = cupType,
            Week       = week,
            IsHomeGame = false   // determined by the draw (home/away in fixture log)
        };
    }

    /// <summary>
    /// Advances the opponent pointer past our own club name, cycling through
    /// all teams in the current division.
    ///
    /// BASIC subroutine 887 (lines 658–661):
    ///   V=V+1; IF V>cn THEN V=cM; IF Y$(V)=Z$ THEN 887.
    /// </summary>
    private static string AdvanceOpponentPointer(GameState gameState)
    {
        int start = DivisionStart(gameState.Club.Division);
        int end   = DivisionEnd(gameState.Club.Division);

        gameState.CurrentOpponentIndex++;
        if (gameState.CurrentOpponentIndex > end)
            gameState.CurrentOpponentIndex = start;

        // Skip our own team
        int guard = 0;
        while (gameState.AllTeamNames[gameState.CurrentOpponentIndex].Trim()
               == gameState.Club.Name.Trim()
               && guard++ < 20)
        {
            gameState.CurrentOpponentIndex++;
            if (gameState.CurrentOpponentIndex > end)
                gameState.CurrentOpponentIndex = start;
        }

        return gameState.AllTeamNames[gameState.CurrentOpponentIndex];
    }

    /// <summary>
    /// Home when matches remaining is even (38, 36, 34 …).
    /// BASIC lines 1711–1712: FOR I=38 TO 2 STEP -2; IF cJ=I THEN BK%=1.
    /// </summary>
    private static bool DetermineHomeAway(int matchesRemaining)
        => matchesRemaining % 2 == 0;

    private static int DivisionStart(Division division) => (int)division * 20 - 19;
    private static int DivisionEnd  (Division division) => (int)division * 20;
}

// ── Data classes ─────────────────────────────────────────────────────────────

public enum MatchType
{
    League,
    LeagueCup,
    FACup,
    EuropeanFirstLeg,
    EuropeanSecondLeg,
    EuropeanFriendly,
    Replay,
    EndOfSeason
}

/// <summary>The match scheduled for the current week.</summary>
public class ScheduledMatch
{
    public MatchType MatchType           { get; set; }
    public int       Week                { get; set; }
    public string    OpponentName        { get; set; } = string.Empty;
    public int       OpponentTeamIndex   { get; set; }
    public bool      IsHomeGame          { get; set; }
}
