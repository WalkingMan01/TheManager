using TheManager.Models;
using MatchType = TheManager.Models.MatchType;

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
    // Cup weeks are fixed offsets within the 59-week season cycle.
    // ToDo: Restore when cup logic is implemented.
    private static readonly HashSet<int> LeagueCupWeeks = []; // { 12, 19, 26, 33, 40, 47, 54 }
    private static readonly HashSet<int> FACupWeeks     = []; // { 16, 23, 30, 37, 44, 51, 58 }

    // ── Public API ────────────────────────────────────────────────────────────

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
        //int week = gameState.CurrentWeek;

        //if (week > Models.Constants.WeeksInSeason)
        //    return new ScheduledMatch { MatchType = MatchType.EndOfSeason, Week = week };

        //string opponentName = AdvanceOpponentPointer(gameState);
        //bool   isHomeGame   = gameState.MatchesRemainingThisSeason % 2 == 0;

        //return new ScheduledMatch
        //{
        //    MatchType         = MatchType.League,
        //    Week              = week,
        //    OpponentName      = opponentName,
        //    OpponentTeamIndex = gameState.CurrentOpponentIndex,
        //    IsHomeGame        = isHomeGame
        //};
        if (gameState.CurrentWeek > Constants.WeeksInSeason)
            return new ScheduledMatch { MatchType = MatchType.EndOfSeason, Week = gameState.CurrentWeek };

        return gameState.Fixtures.FirstOrDefault(m => m.Week == gameState.CurrentWeek)
            ?? new ScheduledMatch { MatchType = MatchType.EndOfSeason, Week = gameState.CurrentWeek };
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
        gameState.FixturesPlayed             = Math.Min(38, gameState.FixturesPlayed + 1);
        gameState.MatchesRemainingThisSeason = 38 - gameState.FixturesPlayed;
    }

    /// <summary>
    /// Initialises the opponent pointer for a fresh season or after a club change.
    /// Sets V to the first team in the current division (cM = AP*20 − 19).
    ///
    /// BASIC subroutine 23000 (line 4676): V=cM.
    /// </summary>
    public static void ResetOpponentPointer(GameState gameState)
    {
        gameState.CurrentOpponentIndex = DivisionRange(gameState.Club.Division).Start;
    }

    /// <summary>
    /// Generates the full 38-fixture season schedule using the circle-method
    /// round-robin algorithm and stores the result in <see cref="GameState.Fixtures"/>.
    ///
    /// The 20 division teams are treated as a fixed team (index 19 within the
    /// division) plus 19 rotating teams. Each call to <see cref="GetRoundPairings"/>
    /// returns the 10 fixtures for one league round. Rounds 0–18 form the first
    /// half; rounds 19–37 reverse home/away to give the second half.
    /// </summary>
    public static void GetSeasonFixtures(GameState gameState)
    {
        var (divStart, _) = DivisionRange(gameState.Club.Division);
        string ourTrimmed = gameState.Club.Name.Trim();

        // Build the ordered team list for this division (indices 0..19 within division)
        var divTeams = new string[20];
        int playerIdx = 0;
        for (int i = 0; i < 20; i++)
        {
            divTeams[i] = gameState.AllTeamNames[divStart + i];
            if (divTeams[i].Trim().Equals(ourTrimmed, StringComparison.OrdinalIgnoreCase))
                playerIdx = i;
        }

        var fixtures = new List<ScheduledMatch>(Constants.WeeksInSeason);

        for (int round = 0; round < Constants.WeeksInSeason; round++)
        {
            var pairs = GetRoundPairings(round);

            // Find the pair that contains the player
            var pair = Array.Find(pairs, p => p.homeIdx == playerIdx || p.awayIdx == playerIdx);

            bool   isHome      = pair.homeIdx == playerIdx;
            int    oppDivIdx   = isHome ? pair.awayIdx : pair.homeIdx;
            string opponentName = divTeams[oppDivIdx];

            fixtures.Add(new ScheduledMatch
            {
                MatchType         = MatchType.League,
                Week              = round + 1,
                OpponentName      = opponentName,
                OpponentTeamIndex = divStart + oppDivIdx,
                IsHomeGame        = isHome
            });
        }

        gameState.Fixtures = fixtures;
    }

    /// <summary>
    /// Returns the 10 home/away pairings (as division-relative indices 0..19)
    /// for a given league round using the circle-method round-robin algorithm.
    ///
    /// Team at index 19 is the fixed pivot. For rounds 0–18 (first half) it is
    /// home; for rounds 19–37 (second half) home/away is reversed so every team
    /// gets each opponent once at home and once away over 38 rounds.
    /// </summary>
    public static (int homeIdx, int awayIdx)[] GetRoundPairings(int leagueRound)
    {
        int  r          = leagueRound % 19;
        bool secondHalf = leagueRound >= 19;

        var pairs = new (int homeIdx, int awayIdx)[10];

        // Fixed team (index 19) is home in the first half, away in the second
        pairs[0] = secondHalf ? (r, 19) : (19, r);

        for (int k = 1; k <= 9; k++)
        {
            int a = ((r - k) % 19 + 19) % 19;
            int b = (r + k) % 19;
            pairs[k] = secondHalf ? (b, a) : (a, b);
        }

        return pairs;
    }

    /// <summary>
    /// Returns true when the current week is a cup week that the player's club
    /// has a fixture in (i.e. they have not been eliminated).
    ///
    /// BASIC lines 1701–1708: checks CI against cup week ranges and tests
    /// whether CT (LC) or CR (FA) is non-zero.
    /// </summary>
    //public static bool HasCupFixtureThisWeek(GameState gameState)
    //{
    //    int week = gameState.CurrentWeek;

    //    if (LeagueCupWeeks.Contains(week))
    //        return gameState.LeagueCup.CurrentRound != CupRound.NotEntered;

    //    if (FACupWeeks.Contains(week))
    //        return gameState.FACup.CurrentRound != CupRound.NotEntered;

    //    return false;
    //}

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Normalises the opponent pointer (wraps at division boundary, skips own club)
    /// and returns the opponent name at the current index.
    ///
    /// BASIC subroutine 887 (lines 658–661):
    ///   V=V+1; IF V>cn THEN V=cM; IF Y$(V)=Z$ THEN 887.
    /// </summary>
    private static string AdvanceOpponentPointer(GameState gameState)
    {
        var (start, end) = DivisionRange(gameState.Club.Division);

        if (gameState.CurrentOpponentIndex > end)
            gameState.CurrentOpponentIndex = start;

        if (string.Equals(gameState.AllTeamNames[gameState.CurrentOpponentIndex], gameState.Club.Name, StringComparison.CurrentCultureIgnoreCase))
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
    //private static bool DetermineHomeAway(int matchesRemaining)
    //    => matchesRemaining % 2 == 0;

    /// <summary>Returns the inclusive [Start, End] team-index range for <paramref name="division"/>.</summary>
    private static (int Start, int End) DivisionRange(Division division)
    {
        int end = (int)division * 20;
        return (end - 19, end);
    }
}
