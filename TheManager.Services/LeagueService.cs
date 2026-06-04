using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Updates league standings after a result and re-sorts the table.
///
/// Corresponds to subroutines 3951, 3956, 3957 in FOOT.BAS.
/// </summary>
public static class LeagueService
{
    // ── Record a result (subroutine 3951 + 3956) ─────────────────────────────

    /// <summary>
    /// Applies a match result to both teams in the league table.
    ///
    /// BASIC subroutine 3951 (lines 3261–3274):
    ///   Find home team by name, update P/W/D/F, then swap scores (subroutine 3956)
    ///   and repeat for away team.
    /// </summary>
    public static void RecordResult(
        LeagueTable table,
        string homeTeam, int homeScore,
        string awayTeam, int awayScore)
    {
        UpdateEntry(table, homeTeam,
            won:   homeScore > awayScore  ? 1 : 0,
            drawn: homeScore == awayScore ? 1 : 0,
            goalsFor: homeScore, goalsAgainst: awayScore);

        UpdateEntry(table, awayTeam,
            won:   awayScore > homeScore  ? 1 : 0,
            drawn: awayScore == homeScore ? 1 : 0,
            goalsFor: awayScore, goalsAgainst: homeScore);
    }

    private static void UpdateEntry(
        LeagueTable table,
        string teamName,
        int won, int drawn,
        int goalsFor, int goalsAgainst)
    {
        var entry = table.Entries.FirstOrDefault(e => e.TeamName == teamName);
        if (entry == null) return;

        entry.Played++;
        entry.Won          += won;
        entry.Drawn        += drawn;
        entry.GoalsFor     += goalsFor;
        entry.GoalsAgainst += goalsAgainst;
    }

    // ── Sort the table (subroutine 3957, lines 3281–3310) ────────────────────

    /// <summary>
    /// Sorts the league table in-place using the same insertion-sort as
    /// FOOT.BAS subroutine 3957:
    ///   Primary:   points (descending)
    ///   Secondary: goal difference (descending)
    ///   Tertiary:  goals for (descending)
    /// </summary>
    public static void Sort(LeagueTable table, int pointsPerWin)
    {
        var entries = table.Entries;

        for (int sortIndex = 1; sortIndex < entries.Count; sortIndex++)
        {
            var  currentEntry      = entries[sortIndex];
            int  currentPoints     = currentEntry.Points(pointsPerWin);
            int  currentGoalDiff   = currentEntry.GoalDifference;
            int  currentGoalsFor   = currentEntry.GoalsFor;

            int insertAt = sortIndex - 1;

            while (insertAt >= 0)
            {
                var above          = entries[insertAt];
                int abovePoints    = above.Points(pointsPerWin);
                int aboveGoalDiff  = above.GoalDifference;

                // line 3958: above team has more points → stop here
                if (abovePoints > currentPoints) break;

                // line 3959: equal points, above has better goal difference → stop
                if (abovePoints == currentPoints && aboveGoalDiff > currentGoalDiff) break;

                // line 3960: equal points and GD, above has more goals for → stop
                if (abovePoints == currentPoints
                    && aboveGoalDiff  == currentGoalDiff
                    && above.GoalsFor  > currentGoalsFor) break;

                // Shift entry down (line 3961)
                entries[insertAt + 1] = entries[insertAt];
                insertAt--;
            }

            entries[insertAt + 1] = currentEntry;
        }
    }

    // ── Postponed fixture resolution (subroutine 3906) ───────────────────────

    /// <summary>
    /// Plays out all postponed fixtures with random scores and updates the table.
    ///
    /// BASIC subroutine 3906 (lines 3244–3252):
    ///   homeScore = max(0, RND*9 − 5),  awayScore = max(0, RND*8 − 5)
    /// </summary>
    public static void PlayPostponedFixtures(
        LeagueTable                  table,
        IReadOnlyList<PostponedFixture> postponedFixtures,
        IReadOnlyList<string>           allTeamNames,
        int                          pointsPerWin,
        Random                       rng)
    {
        foreach (var fixture in postponedFixtures)
        {
            string homeTeam = allTeamNames[fixture.HomeTeamIndex];
            string awayTeam = allTeamNames[fixture.AwayTeamIndex];

            int homeScore = Math.Max(0, rng.Next(9) - 5);
            int awayScore = Math.Max(0, rng.Next(8) - 5);

            RecordResult(table, homeTeam, homeScore, awayTeam, awayScore);
        }

        Sort(table, pointsPerWin);
    }

    // ── Simulate other fixtures (lines 3412–3429) ────────────────────────────

    /// <summary>
    /// Simulates the nine other league fixtures on the same match day, records
    /// all results, and re-sorts the table.
    ///
    /// BASIC lines 3412–3429: pairs up the remaining 18 division teams (excluding
    /// the player's club and their opponent this week). Scores are weighted by
    /// current league position (subroutines 3408 + 3423).
    /// </summary>
    /// <summary>
    /// Simulates the nine other league fixtures on the same match day using the
    /// round-robin schedule, records all results, and re-sorts the table.
    ///
    /// BASIC lines 3412–3429. Pairs are derived from the circle-method schedule
    /// (see <see cref="FixtureSchedulerService.GetRoundPairings"/>) so every team
    /// faces every other team exactly once per half-season. The player's own pair
    /// is identified by division index and skipped.
    /// </summary>
    public static List<OtherFixtureResult> SimulateOtherFixtures(
        LeagueTable table,
        string[]    allTeamNames,
        Division    division,
        string      ourClub,
        int         leagueRound,
        int         pointsPerWin,
        Random      rng)
    {
        int    divStart   = (int)division * 20 - 19;
        string ourTrimmed = ourClub.Trim();

        var divTeams = new string[20];
        int playerIdx = 0;
        for (int i = 0; i < 20; i++)
        {
            divTeams[i] = allTeamNames[divStart + i];
            if (divTeams[i].Trim() == ourTrimmed)
                playerIdx = i;
        }

        var pairs   = FixtureSchedulerService.GetRoundPairings(leagueRound);
        var results = new List<OtherFixtureResult>(9);

        foreach (var (homeIdx, awayIdx) in pairs)
        {
            if (homeIdx == playerIdx || awayIdx == playerIdx)
                continue;

            string home    = divTeams[homeIdx];
            string away    = divTeams[awayIdx];
            int    homePos = LeaguePosition(table, home);  // KI in BASIC 3422
            int    awayPos = LeaguePosition(table, away);  // KH in BASIC 3423

            // BASIC 3423: R = 1 + INT(RND*8) + (KI<KH)*2 − 5, clamped to 0
            int homeScore = Math.Max(0, 1 + rng.Next(8) + (homePos < awayPos ? 2 : 0) - 5);
            int awayScore = Math.Max(0, 1 + rng.Next(7) + (awayPos < homePos ? 2 : 0) - 5);

            RecordResult(table, home, homeScore, away, awayScore);
            results.Add(new OtherFixtureResult
            {
                HomeTeam  = home.Trim(),
                HomeScore = homeScore,
                AwayTeam  = away.Trim(),
                AwayScore = awayScore
            });
        }

        Sort(table, pointsPerWin);
        return results;
    }

    private static int LeaguePosition(LeagueTable table, string teamName)
    {
        string trimmed = teamName.Trim();
        for (int i = 0; i < table.Entries.Count; i++)
            if (table.Entries[i].TeamName.Trim() == trimmed)
                return i + 1;
        return 10;
    }

    // ── Mid-season division swap ──────────────────────────────────────────────

    /// <summary>
    /// Replaces the team names in an existing league table with those from a
    /// different division, preserving all stats (played, won, drawn, GF, GA).
    ///
    /// Used when the manager joins a new club mid-season: the standings carry
    /// over so the table is not blank, but the clubs shown are from the new
    /// division.
    /// </summary>
    public static void SwapDivisionTeams(LeagueTable table, Division newDivision, string[] allTeamNames)
    {
        int divStart = (int)newDivision * 20 - 19;
        table.Division = newDivision;

        for (int i = 0; i < table.Entries.Count && i < 20; i++)
            table.Entries[i].TeamName = allTeamNames[divStart + i];
    }

    // ── League table initialisation ───────────────────────────────────────────

    /// <summary>
    /// Creates and returns a fresh league table for the given division,
    /// populated with the 20 team names from <paramref name="allTeamNames"/>.
    ///
    /// Called at new-game setup and at the start of each new season.
    /// </summary>
    public static LeagueTable InitialiseTable(Division division, string[] allTeamNames)
    {
        int divStart = (int)division * 20 - 19;
        var table    = new LeagueTable { Division = division };
        for (int i = 0; i < 20; i++)
            table.Entries.Add(new LeagueEntry { TeamName = allTeamNames[divStart + i] });
        return table;
    }

    // ── Weekly result string (line 3070) ─────────────────────────────────────

    /// <summary>
    /// Encodes a match result as the 2-char string stored in W$(id).
    /// Format: opponent-score digit first, then our-score digit (ASCII 48–57).
    /// BASIC line 3070: W$(id) = RIGHT$(STR$(S),1) + RIGHT$(STR$(R),1) when home.
    /// </summary>
    public static string EncodeResultString(int ourScore, int opponentScore)
    {
        char opponentDigit = (char)('0' + Math.Min(9, opponentScore));
        char ourDigit      = (char)('0' + Math.Min(9, ourScore));
        return $"{opponentDigit}{ourDigit}";
    }
}
