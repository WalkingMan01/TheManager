using TheManager.Models;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.Services;

/// <summary>
/// Manages the matchday-by-matchday fixture cycle, determining match type,
/// opponent, and home/away status for each league matchday plus cup matchdays.
///
/// Division One has 20 teams and plays 38 league fixtures per season.
/// Divisions Two–Four each have 24 teams and play 46 fixtures per season.
/// The season calendar is 54 matchdays (<see cref="Constants.SeasonMatchdays"/>);
/// FA Cup rounds fall on <see cref="Constants.FACupMatchdays"/>.
///
/// The original BASIC cycle ran CI=1–59 with cup slots at fixed offsets
/// (League Cup 12,19,…,54; FA Cup 16,23,…,58; CI=59 = end of season).
///
/// Corresponds to lines 422–430, 1701–1724 in FOOT.BAS.
/// </summary>
public static class FixtureSchedulerService
{
    /// <summary>
    /// For Division One (38 league fixtures in 46 non-cup matchdays), the
    /// 1-based non-cup ordinals that are rest days instead of league fixtures.
    /// Spread roughly evenly; deterministic; never matchday 1, never consecutive.
    /// </summary>
    private static readonly int[] DivisionOneRestOrdinals = [6, 12, 18, 24, 30, 36, 42, 46];

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the type, opponent, and home/away flag for the current week.
    /// If no fixture is scheduled for <paramref name="currentWeek"/> the result
    /// has <see cref="MatchType.EndOfSeason"/>.
    ///
    /// BASIC lines 422–430 and 1701–1724:
    ///   Cup weeks are detected by CI value.
    ///   For league weeks V cycles through teams in the division (subroutine 887).
    ///   Home/away is determined by cJ being even (lines 1711–1712).
    /// </summary>
    public static ScheduledMatch GetCurrentMatch(int currentWeek, IReadOnlyList<ScheduledMatch> fixtures)
    {
        return fixtures.FirstOrDefault(m => m.Week == currentWeek)
            ?? new ScheduledMatch { MatchType = MatchType.EndOfSeason, Week = currentWeek };
    }

    /// <summary>
    /// Advances the matchday counter (CI) by one and, when a league fixture was
    /// played, the fixtures-played counter. Cup ties and rest days advance the
    /// calendar but never the league counters, keeping the sacking form window
    /// and the round-robin simulation aligned.
    ///
    /// <paramref name="maxFixtures"/> is the total league fixtures in the season
    /// for the current division (38 for Div 1, 46 for Divs 2–4). Defaults to 38.
    ///
    /// BASIC lines 422–424:
    ///   CI=CI+1; if NC=0 then skip to weekly news; else GOTO 1701.
    /// </summary>
    public static MatchdayAdvanceResult AdvanceMatchday(
        int currentWeek, int fixturesPlayed, int maxFixtures = 38, bool wasLeagueFixture = true)
    {
        int newFixturesPlayed = wasLeagueFixture
            ? Math.Min(maxFixtures, fixturesPlayed + 1)
            : fixturesPlayed;
        return new MatchdayAdvanceResult(currentWeek + 1, newFixturesPlayed, maxFixtures - newFixturesPlayed);
    }

    /// <summary>
    /// Builds the full 54-matchday season calendar for the club: league fixtures
    /// on non-cup matchdays, FA Cup placeholders on <see cref="Constants.FACupMatchdays"/>
    /// (opponent filled in at draw time), and — for Division One, whose 38 league
    /// fixtures don't fill the 46 non-cup slots — 8 evenly spread rest days.
    /// </summary>
    public static List<ScheduledMatch> BuildSeasonCalendar(
        Division division, string clubName, string[] allTeamNames)
    {
        var leagueFixtures = GenerateSeasonFixtures(division, clubName, allTeamNames);
        var cupMatchdays   = new HashSet<int>(Constants.FACupMatchdays);
        bool hasRestDays   = division == Division.One;
        var restOrdinals   = new HashSet<int>(DivisionOneRestOrdinals);

        var calendar        = new List<ScheduledMatch>(Constants.SeasonMatchdays);
        int leagueIndex     = 0;
        int nonCupOrdinal   = 0;

        for (int matchday = 1; matchday <= Constants.SeasonMatchdays; matchday++)
        {
            if (cupMatchdays.Contains(matchday))
            {
                calendar.Add(new ScheduledMatch { Week = matchday, MatchType = MatchType.FACup });
                continue;
            }

            nonCupOrdinal++;

            if ((hasRestDays && restOrdinals.Contains(nonCupOrdinal)) || leagueIndex >= leagueFixtures.Count)
            {
                calendar.Add(new ScheduledMatch { Week = matchday, MatchType = MatchType.NoFixture });
                continue;
            }

            var fixture = leagueFixtures[leagueIndex++];
            fixture.Week = matchday;
            calendar.Add(fixture);
        }

        return calendar;
    }

    /// <summary>
    /// Initialises the opponent pointer for a fresh season or after a club change.
    /// Sets V to the first team in the current division (cM = AP*20 − 19).
    ///
    /// BASIC subroutine 23000 (line 4676): V=cM.
    /// </summary>
    public static int GetDivisionStartIndex(Division division)
        => Constants.DivisionRange(division).Start;

    /// <summary>
    /// Generates the full season schedule using the circle-method round-robin
    /// algorithm, then reorders the fixtures to alternate home and away games
    /// as evenly as possible (H/A/H/A/…), starting at home.
    ///
    /// Division One produces 38 fixtures (20 teams).
    /// Divisions Two–Four produce 46 fixtures each (24 teams).
    ///
    /// The circle-method guarantees each opponent is faced exactly once home
    /// and once away, so a perfect H/A alternation is always achievable.
    /// The reordering only changes the week each match is played, not who
    /// plays who or the H/A assignment per opponent.
    /// </summary>
    public static List<ScheduledMatch> GenerateSeasonFixtures(Division division, string clubName, string[] allTeamNames)
    {
        var (divStart, _) = Constants.DivisionRange(division);
        int teamCount      = Constants.TeamCount(division);
        int fixturesInSeason = Constants.FixturesInSeason(division);
        string ourTrimmed  = clubName.Trim();

        var divTeams = new string[teamCount];
        int playerIdx = 0;
        for (int i = 0; i < teamCount; i++)
        {
            divTeams[i] = allTeamNames[divStart + i];
            if (divTeams[i].Trim().Equals(ourTrimmed, StringComparison.OrdinalIgnoreCase))
                playerIdx = i;
        }

        // Build fixtures in circle-method round order — this fixes who plays who
        // and the H/A assignment per opponent, and guarantees no two same-round-half
        // fixtures share an opponent.
        var roundOrder = new List<ScheduledMatch>(fixturesInSeason);

        for (int round = 0; round < fixturesInSeason; round++)
        {
            var  pairs  = GetRoundPairings(round, teamCount);
            var  pair   = Array.Find(pairs, p => p.homeIdx == playerIdx || p.awayIdx == playerIdx);
            bool isHome = pair.homeIdx == playerIdx;
            int  oi     = isHome ? pair.awayIdx : pair.homeIdx;

            roundOrder.Add(new ScheduledMatch
            {
                MatchType         = MatchType.League,
                OpponentName      = divTeams[oi],
                OpponentTeamIndex = divStart + oi,
                IsHomeGame        = isHome
            });
        }

        return InterleaveHomeAndAway(roundOrder);
    }

    /// <summary>
    /// Reorders a set of fixtures (already fixed as to who plays who and where)
    /// into a week-by-week sequence that alternates home and away as strictly
    /// as possible, starting at home.
    ///
    /// Each opponent's away fixture is offset by a fixed shift (half the list,
    /// rounded) relative to that opponent's home position. This guarantees no
    /// opponent ever appears on consecutive weeks.
    /// </summary>
    private static List<ScheduledMatch> InterleaveHomeAndAway(List<ScheduledMatch> fixtures)
    {
        var home = fixtures.Where(f => f.IsHomeGame).ToList();
        var awayByOpponent = fixtures.Where(f => !f.IsHomeGame)
            .ToDictionary(f => f.OpponentName);

        int shift  = home.Count / 2;
        var result = new ScheduledMatch[fixtures.Count];

        for (int h = 0; h < home.Count; h++)
        {
            var homeMatch = home[h];
            var awayMatch = awayByOpponent[homeMatch.OpponentName];
            int a = (h + shift) % home.Count;

            homeMatch.Week = 2 * h + 1;
            awayMatch.Week = 2 * a + 2;

            result[homeMatch.Week - 1] = homeMatch;
            result[awayMatch.Week - 1] = awayMatch;
        }

        return result.ToList();
    }

    /// <summary>
    /// Returns the circle-method round in which <paramref name="playerIdx"/>
    /// is paired against <paramref name="opponentIdx"/>.
    ///
    /// <paramref name="teamCount"/> must match the division size (20 or 24).
    /// Defaults to 20 (Division One) for backward compatibility.
    ///
    /// Used to pass the correct round to <see cref="LeagueService.SimulateOtherFixtures"/>
    /// so its skip logic aligns with the match actually played rather than the
    /// interleaved fixture-count index.
    /// </summary>
    public static int FindLeagueRound(int playerIdx, int opponentIdx, int teamCount = 20)
    {
        int fixturesInSeason = (teamCount - 1) * 2;
        for (int round = 0; round < fixturesInSeason; round++)
        {
            foreach (var (h, a) in GetRoundPairings(round, teamCount))
                if ((h == playerIdx && a == opponentIdx) || (h == opponentIdx && a == playerIdx))
                    return round;
        }
        return 0;
    }

    /// <summary>
    /// Returns the home/away pairings (as division-relative indices 0..teamCount-1)
    /// for a given league round using the circle-method round-robin algorithm.
    ///
    /// <paramref name="teamCount"/> must be even (20 for Div 1, 24 for Divs 2–4).
    /// Defaults to 20 for backward compatibility.
    ///
    /// The team at index <c>teamCount-1</c> is the fixed pivot. For the first
    /// half of rounds it is home; for the second half home/away is reversed so
    /// every team gets each opponent once at home and once away.
    /// </summary>
    public static (int homeIdx, int awayIdx)[] GetRoundPairings(int leagueRound, int teamCount = 20)
    {
        int  pivot      = teamCount - 1;
        int  halfCount  = teamCount / 2;
        int  r          = leagueRound % pivot;
        bool secondHalf = leagueRound >= pivot;

        var pairs = new (int homeIdx, int awayIdx)[halfCount];

        // Fixed team (pivot) is home in the first half, away in the second
        pairs[0] = secondHalf ? (r, pivot) : (pivot, r);

        for (int k = 1; k < halfCount; k++)
        {
            int a = ((r - k) % pivot + pivot) % pivot;
            int b = (r + k) % pivot;
            pairs[k] = secondHalf ? (b, a) : (a, b);
        }

        return pairs;
    }

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
        var (start, end) = Constants.DivisionRange(gameState.Club.Division);

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
}

// ── Result types ──────────────────────────────────────────────────────────────

/// <summary>Output of <see cref="FixtureSchedulerService.AdvanceMatchday"/>.</summary>
public record MatchdayAdvanceResult(int CurrentWeek, int FixturesPlayed, int MatchesRemainingThisSeason);
