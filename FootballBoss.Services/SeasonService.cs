using FootballBoss.Models;

namespace FootballBoss.Services;

/// <summary>
/// Handles end-of-season processing: promotion/relegation, prize money,
/// manager rating, share price adjustment, youth player aging, and season
/// history rolling.
///
/// Corresponds to subroutines 2401–2417, 2412, 2421–2443 in FOOT.BAS.
/// </summary>
public static class SeasonService
{
    // ── Manager rating (lines 2631–2634) ─────────────────────────────────────

    /// <summary>
    /// Calculates the end-of-season manager performance rating (0–100%).
    ///
    /// BASIC lines 2631–2634:
    ///   managerRating = INT(84 - (leaguePos*4) + (leagueCupRound/2)
    ///                       + (faCupRound/2) + (europeanRound/2))
    ///                 + INT(remainingPlayers / 2.5)
    ///   bankBonus = INT(bankBalance / ((999999/division) / 4)), capped at 4
    ///   managerRating += bankBonus
    /// </summary>
    public static int CalculateManagerRating(
        int      finalLeaguePosition,
        int      leagueCupRoundReached,
        int      faCupRoundReached,
        int      europeanRound,
        int      squadPlayersRemaining,
        double   bankBalance,
        Division division)
    {
        int baseRating = (int)(
            84
            - (finalLeaguePosition * 4)
            + (leagueCupRoundReached / 2.0)
            + (faCupRoundReached     / 2.0)
            + (europeanRound         / 2.0))
            + (int)(squadPlayersRemaining / 2.5);

        double bankRatingThreshold = (999_999.0 / (int)division) / 4;
        int    bankBonus           = Math.Min(4, (int)(bankBalance / bankRatingThreshold));

        return (int)(baseRating + bankBonus);
    }

    // ── League prize money (lines 2403) ──────────────────────────────────────

    /// <summary>
    /// Awards prize money for finishing in the top 3 of the league.
    ///
    /// BASIC lines 2403–2404:
    ///   prize = INT(50000 / division) / leaguePosition
    ///   Only awarded when finalLeaguePosition ≤ 3.
    /// </summary>
    public static void AwardLeaguePrizeMoney(
        Finances finances,
        int      finalLeaguePosition,
        Division division)
    {
        if (finalLeaguePosition > 3) return;

        double prizeAmount    = (int)(50_000.0 / (int)division) / finalLeaguePosition;
        finances.BankBalance += (int)prizeAmount;
    }

    // ── Share price adjustment (lines 2401–2402) ─────────────────────────────

    /// <summary>
    /// Adjusts the share price based on the change in league position compared
    /// to the previous season.
    ///
    /// BASIC lines 2401–2402:
    ///   If position improved (lower number): sharePriceIndex += (prevPos-currentPos)*20
    ///   If position worsened: sharePriceIndex -= (currentPos-prevPos)*20
    ///   Then converge toward 1.0: sharePrice += ((1-sharePrice)/2) * (1+SGN(0.95-sharePrice))
    /// </summary>
    public static void AdjustSharePrice(
        Finances finances,
        int      currentLeaguePosition,
        int      previousLeaguePosition)
    {
        if (currentLeaguePosition < previousLeaguePosition)
            finances.SharePriceInPence += (previousLeaguePosition - currentLeaguePosition) * 20;
        else if (currentLeaguePosition > previousLeaguePosition)
            finances.SharePriceInPence -= (currentLeaguePosition - previousLeaguePosition) * 20;

        // Gravity toward 100p (line 2402: AK += ((1-AK)/2)*(1+SGN(0.95-AK)))
        double convergenceFactor = finances.SharePriceInPence < 95 ? 1 : -1;
        finances.SharePriceInPence +=
            (int)(((100 - finances.SharePriceInPence) / 2.0) * (1 + convergenceFactor));
    }

    // ── Promotion / relegation (lines 2416, 2421–2428) ───────────────────────

    /// <summary>
    /// Determines the player's new division based on final league position.
    ///
    /// BASIC line 2416:
    ///   If leaguePos > 17 and division &lt; 4: relegated (division + 1)
    ///   If leaguePos &lt; 4 and division > 1:  promoted (division - 1)
    ///   Otherwise: no change
    /// </summary>
    public static Division DetermineNewDivision(
        int      finalLeaguePosition,
        Division currentDivision)
    {
        int divisionNumber = (int)currentDivision;

        if (finalLeaguePosition > 17 && divisionNumber < 4)
            return (Division)(divisionNumber + 1);

        if (finalLeaguePosition < 4 && divisionNumber > 1)
            return (Division)(divisionNumber - 1);

        return currentDivision;
    }

    /// <summary>
    /// Swaps the bottom 3 teams of an upper division with the top 3 of the
    /// lower division in the all-teams name array, simulating promotion and
    /// relegation for the AI-controlled divisions.
    ///
    /// BASIC subroutines 2421–2428 process divisions 2 and 4 (i.e. the boundary
    /// between Div1/Div2 and Div3/Div4).
    ///
    /// Team indices: Div N occupies Y$(((N-1)*20)+1) to Y$(N*20).
    ///   Bottom 3 of upper div = indices (N*20)-2, (N*20)-1, (N*20)
    ///   Top 3 of lower div    = indices ((N)*20)+1, ((N)*20)+2, ((N)*20)+3
    /// </summary>
    public static void SwapPromotedRelegatedTeams(
        string[] allTeamNames,
        int      upperDivisionNumber)
    {
        // Bottom of upper division (last 3 entries)
        int upperBottom1 = upperDivisionNumber * 20 - 2;
        int upperBottom2 = upperDivisionNumber * 20 - 1;
        int upperBottom3 = upperDivisionNumber * 20;

        // Top of lower division (first 3 entries)
        int lowerTop1 = upperDivisionNumber * 20 + 1;
        int lowerTop2 = upperDivisionNumber * 20 + 2;
        int lowerTop3 = upperDivisionNumber * 20 + 3;

        // Swap (line 2428)
        (allTeamNames[upperBottom1], allTeamNames[lowerTop1]) =
            (allTeamNames[lowerTop1], allTeamNames[upperBottom1]);

        (allTeamNames[upperBottom2], allTeamNames[lowerTop2]) =
            (allTeamNames[lowerTop2], allTeamNames[upperBottom2]);

        (allTeamNames[upperBottom3], allTeamNames[lowerTop3]) =
            (allTeamNames[lowerTop3], allTeamNames[upperBottom3]);
    }

    // ── Youth player aging (subroutine 2441, lines 1782–1786) ────────────────

    /// <summary>
    /// Ages youth players by one year and removes those who have reached 19
    /// without being promoted to the first team.
    ///
    /// BASIC subroutine 2441:
    ///   For each youth player (I=6–12): Y(5,I) += (Y(4,I)>0)
    ///   If Y(5,I)=19: release the player (clear slot, NO--)
    /// </summary>
    public static void AgeYouthPlayers(
        List<YouthPlayer> youthTeam,
        ref int           youthPlayerCount)
    {
        for (int index = youthTeam.Count - 1; index >= 0; index--)
        {
            var youthPlayer = youthTeam[index];

            // Only age players who have an assigned position (Y(4,I)>0)
            if (youthPlayer.Position != PlayerPosition.None)
                youthPlayer.Age++;

            if (youthPlayer.Age >= 19)
            {
                youthTeam.RemoveAt(index);
                youthPlayerCount = Math.Max(0, youthPlayerCount - 1);
            }
        }
    }

    // ── Season history rolling (subroutine 2443, lines 1788–1795) ────────────

    /// <summary>
    /// Adds the current season's record to the history list, rolling off the
    /// oldest entry when the list exceeds 10 seasons.
    ///
    /// BASIC subroutine 2443:
    ///   If ns=11: shift K(2..10) → K(1..9), clear K(10).
    /// </summary>
    public static void RecordSeasonAndRoll(
        List<SeasonRecord> history,
        SeasonRecord       completedSeason)
    {
        history.Add(completedSeason);

        if (history.Count > 10)
            history.RemoveAt(0);
    }

    // ── Full end-of-season reset (subroutines 22000, 23000) ──────────────────

    /// <summary>
    /// Resets all match-state variables to their start-of-season defaults.
    /// Called after promotion/relegation has been applied.
    ///
    /// BASIC subroutine 22000 (line 4658):
    ///   Resets OJ, OK, CT, CR, MT, MS, CI, cJ, BK%, CV, cp, cq, gz, be, dt, etc.
    /// </summary>
    public static void ResetMatchState(GameState gameState)
    {
        gameState.CurrentWeek                  = 1;
        gameState.FixturesPlayed               = 0;
        gameState.CurrentMatch                 = null;
        gameState.InEuropeanFriendlyTour       = false;

        gameState.LeagueCup.CurrentRound       = CupRound.Round1;
        gameState.LeagueCup.RoundTracker       = 3 - ((int)gameState.Club.Division > 2 ? 0 : 1);
        gameState.FACup.CurrentRound           = CupRound.Round1;
        gameState.FACup.RoundTracker           = gameState.LeagueCup.RoundTracker;

        if (gameState.European != null)
            gameState.European.RoundState      = 0;
    }

    /// <summary>
    /// Recalculates the division-specific financial ceiling and starting
    /// overdraft position for the new season.
    ///
    /// BASIC subroutine 23000 (line 4673):
    ///   overdraftMaximum = INT(310000 / division) - (division * 10000)
    ///   overdraftAvailable = overdraftMaximum - loanOutstanding
    /// </summary>
    public static void RecalculateDivisionFinancials(Finances finances, Division division)
    {
        double newOverdraftCeiling       = (int)(310_000.0 / (int)division) - ((int)division * 10_000);
        finances.OverdraftMaximum        = newOverdraftCeiling;
        finances.OverdraftAvailable      = Math.Max(0, newOverdraftCeiling - finances.LoanOutstanding);
    }

    // ── Full season wrap-up ───────────────────────────────────────────────────

    /// <summary>
    /// Convenience method that runs all end-of-season steps in the correct order,
    /// matching the sequence in FOOT.BAS subroutine 2401 / 2412 / 2416–2417.
    /// </summary>
    public static SeasonSummary WrapUpSeason(
        GameState gameState,
        Random    rng)
    {
        var club     = gameState.Club;
        var finances = gameState.Finances;

        // 1. Manager rating
        int squadCount   = gameState.Squad.Count(p => p?.Position != PlayerPosition.None);
        int managerRating = CalculateManagerRating(
            finalLeaguePosition:    club.LeaguePosition,
            leagueCupRoundReached:  (int)gameState.LeagueCup.CurrentRound,
            faCupRoundReached:      (int)gameState.FACup.CurrentRound,
            europeanRound:          gameState.European?.RoundState ?? 0,
            squadPlayersRemaining:  squadCount,
            bankBalance:            finances.BankBalance,
            division:               club.Division);

        club.ManagerRating = managerRating;

        // 2. Prize money for top-3 finish
        AwardLeaguePrizeMoney(finances, club.LeaguePosition, club.Division);

        // 3. Share price adjustment
        AdjustSharePrice(finances, club.LeaguePosition, previousLeaguePosition: gameState.SeasonHistory.LastOrDefault()?.FinalLeaguePosition ?? club.LeaguePosition);

        // 4. Record season before applying promotion/relegation
        var completedRecord = new SeasonRecord
        {
            SeasonNumber          = gameState.SeasonsPlayed + 1,
            FinalLeaguePosition   = club.LeaguePosition,
            LeagueCupRoundReached = (int)gameState.LeagueCup.CurrentRound,
            FACupRoundReached     = (int)gameState.FACup.CurrentRound,
            Division              = club.Division,
            WonLeagueCup          = gameState.LeagueCup.CurrentRound == CupRound.Winner,
            WonFACup              = gameState.FACup.CurrentRound    == CupRound.Winner,
            WonEuropean           = gameState.European?.RoundState  == 9
        };
        RecordSeasonAndRoll(gameState.SeasonHistory, completedRecord);

        // 5. Promotion / relegation for the player's club
        Division newDivision = DetermineNewDivision(club.LeaguePosition, club.Division);
        club.Division        = newDivision;

        // 6. Swap AI teams between divisions (Div1/2 and Div3/4 boundaries)
        SwapPromotedRelegatedTeams(gameState.AllTeamNames, upperDivisionNumber: 1);
        SwapPromotedRelegatedTeams(gameState.AllTeamNames, upperDivisionNumber: 3);

        // 7. Age youth players
        int youthCount = club.YouthPlayerCount;
        AgeYouthPlayers(gameState.YouthTeam, ref youthCount);
        club.YouthPlayerCount = youthCount;

        // 8. Apply weekly skill drift for end-of-season conditioning
        PlayerService.ApplyWeeklySkillDrift(gameState.Squad, rng);

        // 9. Reset match state for new season
        ResetMatchState(gameState);
        RecalculateDivisionFinancials(finances, newDivision);

        gameState.SeasonsPlayed++;

        return new SeasonSummary
        {
            ManagerRating     = managerRating,
            NewDivision       = newDivision,
            WasPromoted       = newDivision < completedRecord.Division,
            WasRelegated      = newDivision > completedRecord.Division,
            CompletedRecord   = completedRecord
        };
    }
}

// ── Data classes ─────────────────────────────────────────────────────────────

/// <summary>Summary returned after <see cref="SeasonService.WrapUpSeason"/>.</summary>
public class SeasonSummary
{
    public int          ManagerRating   { get; set; }
    public Division     NewDivision     { get; set; }
    public bool         WasPromoted     { get; set; }
    public bool         WasRelegated    { get; set; }
    public SeasonRecord CompletedRecord { get; set; } = new();
}
