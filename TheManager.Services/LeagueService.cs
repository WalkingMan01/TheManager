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
