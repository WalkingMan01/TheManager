using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Estimates the opposing team's strength ratings and morale before a match.
///
/// Corresponds to subroutines 413–419 and 416 in FOOT.BAS (lines 432–458).
/// The result is used to populate the <see cref="MatchSetupInput"/> that feeds
/// into <see cref="MatchEngine.SetupMatch"/>.
/// </summary>
public static class OpponentRatingService
{
    /// <summary>
    /// Estimates all four positional ratings, morale, and temper for the opponent.
    ///
    /// BASIC subroutine 416 (lines 439–443):
    ///   Find opponent's league position (mn) by searching T$().
    ///
    /// BASIC subroutine 413 (lines 432–458):
    ///   topThreeBonus (IB) = 1 if opponent in top 3 AND difficulty is normal/hard (JT&lt;1)
    ///   difficultyAdjustment (cx):
    ///     League match: cx = division − difficultyLevel       (line 418)
    ///     Cup match:    cx = cupRound  − difficultyLevel       (line 415)
    ///   Each rating = (6 − cx) + RND(0–3) + topThreeBonus     (line 419)
    ///   League morale: mm = 100 − (5 × leaguePos) + RND(1–20) (line 450)
    ///   Cup morale:    mm = 75 + RND(0–23)                    (subroutine 410)
    ///   Opponent temper (pv): 15 + RND(0–74)                  (line 455)
    /// </summary>
    public static OpponentRatings Estimate(
        string      opponentName,
        LeagueTable currentLeague,
        Division    ourDivision,
        int         difficultyLevel,    // JT: 1=hard, 0=normal, -1=easy
        int         cupRound,           // cs: relevant cup round (0 for league match)
        bool        isCupMatch,         // true when CV=1, cq=1, or cp=1
        Random      rng)
    {
        // ── Find opponent's league position (subroutine 416) ──────────────────
        int opponentLeaguePosition = FindLeaguePosition(currentLeague, opponentName);

        // ── Top-3 bonus (line 433) ────────────────────────────────────────────
        // IB = 1 if opponent is in top 3 AND difficulty is normal or hard (JT < 1)
        bool opponentIsTopThree  = opponentLeaguePosition <= 3 && difficultyLevel < 1;
        int  topThreeBonus       = opponentIsTopThree ? 1 : 0;

        // ── Difficulty adjustment (lines 415, 418) ────────────────────────────
        int difficultyAdjustment = isCupMatch
            ? cupRound - difficultyLevel        // cx = cs − JT
            : (int)ourDivision - difficultyLevel; // cx = AP − JT

        // ── Four positional ratings (line 419) ───────────────────────────────
        int ratingBase         = 6 - difficultyAdjustment;
        int goalkeeperRating   = ratingBase + rng.Next(4) + topThreeBonus;  // EI1
        int defenceRating      = ratingBase + rng.Next(4) + topThreeBonus;  // ej
        int midRating          = ratingBase + rng.Next(4) + topThreeBonus;  // ek
        int attackRating       = ratingBase + rng.Next(4) + topThreeBonus;  // el

        // ── Morale (lines 410, 450–453) ───────────────────────────────────────
        int morale;
        if (isCupMatch)
        {
            // Subroutine 410: cup opponents always have high morale
            morale = 75 + rng.Next(24);
        }
        else
        {
            morale = 100 - (int)(5 * opponentLeaguePosition) + 1 + rng.Next(20);
            morale = Math.Min(99, morale);
        }

        // ── Temper and formation (lines 454–456) ─────────────────────────────
        int opponentTemper   = 15 + rng.Next(75);                    // pv
        int formationRoll    = 1 + rng.Next(2);
        int opponentFormation = formationRoll == 2 ? 433 : 442;      // mv

        return new OpponentRatings
        {
            GoalkeeperRating = Math.Max(1, goalkeeperRating),
            DefenceRating    = Math.Max(1, defenceRating),
            MidRating        = Math.Max(1, midRating),
            AttackRating     = Math.Max(1, attackRating),
            Morale           = morale,
            Temper           = opponentTemper,
            LeaguePosition   = opponentLeaguePosition,
            FormationCode    = opponentFormation
        };
    }

    /// <summary>
    /// Builds a <see cref="MatchSetupInput"/> by combining our
    /// <see cref="TeamRatings"/> with the estimated <see cref="OpponentRatings"/>.
    /// </summary>
    public static MatchSetupInput BuildMatchInput(
        TeamRatings    ourRatings,
        OpponentRatings opponentRatings,
        Club           ourClub,
        bool           isHomeGame,
        bool           lostLastMatch,
        int            lineupChanges,
        int            previousLegOurScore   = 0,
        int            previousLegTheirScore = 0)
    {
        return new MatchSetupInput
        {
            OurGoalkeeperSkill       = ourRatings.GoalkeeperRating,
            OurDefence               = ourRatings.DefenceRating,
            OurMid                   = ourRatings.MidRating,
            OurAttack                = ourRatings.AttackRating,
            OpponentGoalkeeperSkill  = opponentRatings.GoalkeeperRating,
            OpponentDefence          = opponentRatings.DefenceRating,
            OpponentMid              = opponentRatings.MidRating,
            OpponentAttack           = opponentRatings.AttackRating,
            IsHomeGame               = isHomeGame,
            LostLastMatch            = lostLastMatch,
            LineupChanges            = lineupChanges,
            OurMorale                = ourClub.TeamMorale,
            OpponentMorale           = opponentRatings.Morale,
            OurTemper                = ourRatings.TeamTemper,
            OpponentTemper           = opponentRatings.Temper,
            Division                 = (int)ourClub.Division,
            PreviousLegOurScore      = previousLegOurScore,
            PreviousLegTheirScore    = previousLegTheirScore
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static int FindLeaguePosition(LeagueTable league, string teamName)
    {
        string trimmed = teamName.Trim();
        for (int position = 0; position < league.Entries.Count; position++)
        {
            if (league.Entries[position].TeamName.Trim() == trimmed)
                return position + 1;
        }
        return 2;   // default mn=2 in BASIC subroutine 416
    }
}

/// <summary>Estimated strength profile of the opposing team.</summary>
public class OpponentRatings
{
    public int GoalkeeperRating { get; set; }   // EI1
    public int DefenceRating    { get; set; }   // ej
    public int MidRating        { get; set; }   // ek
    public int AttackRating     { get; set; }   // el
    public int Morale           { get; set; }   // mm
    public int Temper           { get; set; }   // pv
    public int LeaguePosition   { get; set; }   // mn
    public int FormationCode    { get; set; }   // mv  (e.g. 442 or 433)
}
