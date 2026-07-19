using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Sets up a brand-new game or transitions the player to a new club.
///
/// Covers three BASIC code paths:
///   1. New squad generation  — subroutine 1501–1514 (lines 1063–1111)
///   2. New game entry point  — lines 4897–4907 (choose team, generate fixtures)
///   3. Join new club mid-game— subroutines 5532–5558 (lines 4432–4566)
///
/// Also initialises the starting staff (subroutine 5601–5624).
/// </summary>
public static class InitializationService
{
    private const int MoraleMinimum = 60;  // ToDo: Was 30 in original
    private const int MoraleRange   = 20;  // ToDo: Was 20 in original

    // ── New squad generation (subroutine 1501–1514) ───────────────────────────

    /// <summary>
    /// Generates a complete starting squad with randomised attributes for a
    /// freshly chosen club at the given division.
    ///
    /// BASIC subroutine 1502–1514:
    ///   squadSize = 12 + RND(0–4)  (12–16 players)
    ///   me = 30 + RND(0–19)        (morale 30–49)
    ///   For each slot 1..squadSize:
    ///     position  = fixed 1-GK, 2-5 DEF, 6-8 MID, 9-11 ATK, 12+ random
    ///     skill     = |AP-5| + RND(0–3.9)  (higher division = lower base skill)
    ///     age       = 18 + RND(0–17)
    ///     wage      = computed from skill × (51 + RND(1–20)) / ageFactor
    ///     contract  = 20 + RND(0–55) weeks
    ///     temper    = 0–9 clamped random
    /// </summary>
    public static SquadGenerationResult GenerateStartingSquad(Division division, Random rng)
    {
        int divNum = (int)division;
        const int squadSize = 16;

        int teamMorale = Math.Max(2, Math.Min(99, MoraleMinimum + rng.Next(MoraleRange)));

        var squad = new Player?[29];
        for (int slot = 1; slot <= 20; slot++)
            squad[slot] = slot <= squadSize ? GeneratePlayer(slot, divNum, rng) : null;

        double playerWageBill =
            squad.Skip(1).Take(20).Where(p => p is not null).Sum(p => p!.WeeklyWage);

        // Bank balance setup (lines 1104–1110)
        double bankBalance = 150_000 + rng.Next((int)(500_000.0 / divNum));

        return new SquadGenerationResult(squad, teamMorale, playerWageBill, (int)bankBalance);
    }

    /// <summary>
    /// Creates a single player for the given squad slot and division.
    /// Extracted for testability — no GameState dependency.
    ///
    /// BASIC lines 1073–1089.
    /// </summary>
    internal static Player GeneratePlayer(int slot, int divNum, Random rng)
    {
        var player = new Player();

        // Position assignment (line 1074)
        player.Position = slot switch
        {
            1              => PlayerPosition.Goalkeeper,
            >= 2 and <= 5  => PlayerPosition.Defender,
            >= 6 and <= 8  => PlayerPosition.Midfielder,
            >= 9 and <= 11 => PlayerPosition.Attacker,
            _              => (PlayerPosition)(1 + rng.Next(4))
        };

        // Skill: |division-5| + 0.0–3.9  (line 1073: H(Y)=ABS(AP-5)+RND*39/10)
        player.Skill = Math.Abs(divNum - 5) + rng.Next(39) / 10.0;

        // Age 18–35 (line 1076: G(Y)=18+INT(RND*18))
        player.Age = 18 + rng.Next(18);

        // Games played this season (line 1086)
        int gamesRange    = Math.Max(0, (player.Age - 17) * 30 - 30);
        player.GamesPlayed = gamesRange > 0 ? 30 + rng.Next(gamesRange) : 30;

        // Temper 0–9 (lines 1088–1089)
        player.Temper = Math.Max(0, Math.Min(9, -3 + rng.Next(17)));

        player.WeeklyWage    = CalculateWage(player.Skill, player.Age, rng);
        player.ContractWeeks = 20 + rng.Next(56);
        player.Name          = NameGenerationService.GenerateName(rng);

        // Hidden ceiling / peak age (new mechanic — no BASIC equivalent)
        PlayerService.AssignPotential(player, rng);

        PlayerService.RecalculateStatus(player);
        return player;
    }

    /// <summary>
    /// Calculates the weekly wage for a player given their skill and age.
    /// Extracted for testability — pure function aside from the RNG call.
    ///
    /// BASIC lines 1077–1082:
    ///   V(1,Y) = (1 + RND*20 + 50) * INT(H(Y)) [+ 1000 if star]
    ///   V(1,Y) = INT(V(1,Y) / HV)   where HV = MAX(1, age-27)
    ///   V(1,Y) = MAX(50, V(1,Y))
    /// </summary>
    internal static double CalculateWage(double skill, int age, Random rng)
    {
        int    ageDivisor = Math.Max(1, age - 27);
        double wageBase   = (1 + rng.Next(20) + 50) * (int)skill
                            + (skill > 9.6 ? 1_000 : 0);
        return Math.Max(50, (int)(wageBase / ageDivisor));
    }

    // ── Starting staff (subroutines 5601–5624) ────────────────────────────────

    /// <summary>
    /// Generates a new club's starting staff: always a coach and physio, plus
    /// a random number of scouts (0–3) and youth players (0–4).
    ///
    /// BASIC subroutine 5602 (line 4574):
    ///   NL=1, NM=1, generate coach and physio names/wages/skill
    ///   NN = RND(0–3) scouts
    ///   NO = RND(0–4) youth players
    /// </summary>
    public static StartingStaff GenerateStartingStaff(Random rng)
    {
        var coach  = StaffService.GenerateCoach(rng);
        var physio = StaffService.GeneratePhysio(rng);

        // 0–3 scouts (line 5580: NN=INT(RND*4); if NN=0 goto 5607)
        var scouts = Enumerable.Range(0, rng.Next(4))
            .Select(_ => StaffService.GenerateScout(rng))
            .ToList();

        // 0–4 youth players (subroutine 5623)
        var youthPlayers = Enumerable.Range(0, rng.Next(5))
            .Select(_ => StaffService.GenerateYouthPlayer(rng))
            .ToList();

        return new StartingStaff(coach, physio, scouts, youthPlayers);
    }

    // ── New game setup ────────────────────────────────────────────────────────

    /// <summary>
    /// Performs the complete new-game initialisation sequence:
    ///   1. Set club identity and division.
    ///   2. Generate squad.
    ///   3. Generate staff.
    ///   4. Generate the initial cup draws (subroutines 1100, 1237).
    ///   5. Reset the fixture pointer.
    ///
    /// Corresponds to BASIC lines 4897–4907.
    /// </summary>
    public static void SetupNewGame(
        GameState gameState,
        string    clubName,
        Division  division,
        string    managerName,        
        Random    rng)
    {
        gameState.Club.Name           = clubName;
        gameState.Club.Division       = division;
        gameState.Club.ManagerName    = managerName;
        gameState.Club.PointsPerWin   = 3;
        gameState.Club.TicketPriceInPounds = 5 - (int)division;  // line 5620: nj=1+(4-AP)

        SeasonService.RecalculateDivisionFinancials(gameState.Finances, division);
        gameState.Finances.SharePriceInPence = 2_000 - (int)division * 400;  // line 5442: AK=2000-(AP*400)
        gameState.Finances.SharesOwned       = 100_000;

        // Generate starting squad and staff
        var squadResult = GenerateStartingSquad(division, rng);
        gameState.Squad                    = squadResult.Squad;
        gameState.Club.TeamMorale          = squadResult.TeamMorale;
        gameState.Finances.PlayerWageBill  = squadResult.PlayerWageBill;
        gameState.Finances.BankBalance     = squadResult.BankBalance;

        TeamData.Seed(gameState);

        var staff = GenerateStartingStaff(rng);
        gameState.Coach               = staff.Coach;
        gameState.Physio              = staff.Physio;
        gameState.Scouts              = staff.Scouts;
        gameState.YouthTeam           = staff.YouthPlayers;
        gameState.Club.HasCoach       = true;
        gameState.Club.HasPhysio      = true;
        gameState.Club.ScoutCount     = staff.Scouts.Count;
        gameState.Club.YouthPlayerCount = staff.YouthPlayers.Count;

        // Cup brackets and round-1 draws (BASIC lines 4900–4904)
        gameState.LeagueCup.Bracket = CupService.SetupInitialBracket();
        gameState.LeagueCup.RoundHistory.Clear();
        gameState.LeagueCup.CurrentRoundFixtures =
            CupService.DrawRound(gameState.LeagueCup.Bracket, gameState.AllTeamNames, rng);

        gameState.FACup.Bracket = CupService.SetupInitialBracket();
        gameState.FACup.RoundHistory.Clear();
        gameState.FACup.CurrentRoundFixtures =
            CupService.DrawRound(gameState.FACup.Bracket, gameState.AllTeamNames, rng);

        // Reset counters
        gameState.CurrentWeek              = 1;
        gameState.FixturesPlayed           = 0;
        gameState.MatchesRemainingThisSeason = Constants.FixturesInSeason(gameState.Club.Division);
        gameState.SeasonsPlayed            = 0;
        gameState.SeasonSlot               = 1;
        gameState.Club.ManagerContractWeeks = 52;   // initial 1-season contract

        gameState.CurrentOpponentIndex = FixtureSchedulerService.GetDivisionStartIndex(gameState.Club.Division);

        // Team ratings
        PlayerService.CalculateTeamRatings(gameState.Squad);
    }

    // ── Join new club (subroutines 5532–5558) ─────────────────────────────────

    /// <summary>
    /// Transitions the player to manage a different club mid-game (either after
    /// being offered a job by a rival club or after being sacked).
    ///
    /// BASIC subroutine 5555 (lines 4544–4562):
    ///   Swap Z$ and J$(IB) club names.
    ///   Reset NI ground improvement based on division.
    ///   Reset PA/PB/PC/PD/PE/PF/PG records.
    ///   Reset cup state, season history slot, K() array.
    ///   Jump to either 5538 (same division — reuse fixture list) or
    ///   5533 (different division — rebuild T$ from Y$).
    /// </summary>
    public static void JoinNewClub(
        GameState gameState,
        string    newClubName,
        Division  newDivision,
        int       startingLeaguePosition,
        Random    rng)
    {
        // Swap club identity (line 5544–5545)
        gameState.Club.Name     = newClubName;
        gameState.Club.Division = newDivision;
        gameState.Club.LeaguePosition = startingLeaguePosition;

        // Ground improvement cost is zero for lower divisions (line 5546)
        if ((int)newDivision > 2)
            gameState.Club.GroundImprovementCost = 0;

        // Reset all-time records (subroutine 5554, line 4533)
        gameState.Finances.RecordSigningFee     = 0;
        gameState.Finances.RecordSaleFee        = 0;
        gameState.RecordSigningName             = string.Empty;
        gameState.RecordSaleName                = string.Empty;
        gameState.HighestGoalscorerName         = string.Empty;
        gameState.MostAppearancesName           = string.Empty;
        gameState.HighestPaidPlayerName         = string.Empty;

        // Shift previous clubs list (O$(8) → O$(7) → O$(6), line 4538–4541)
        gameState.PreviousClubs[2] = gameState.PreviousClubs[1];
        gameState.PreviousClubs[1] = gameState.PreviousClubs[0];
        gameState.PreviousClubs[0] = gameState.Club.Name;

        // Reset cup state, season history, financial ceilings (subroutine 5544)
        SeasonService.ResetMatchState(gameState.LeagueCup, gameState.FACup, gameState.European, newDivision);
        gameState.CurrentWeek            = 1;
        gameState.FixturesPlayed         = 0;
        gameState.CurrentMatch           = null;
        gameState.InEuropeanFriendlyTour = false;
        SeasonService.RecalculateDivisionFinancials(gameState.Finances, newDivision);

        gameState.Finances.SharePriceInPence = 2_000 - (int)newDivision * 400;
        gameState.Finances.SharesOwned       = 100_000;
        gameState.Finances.LoanOutstanding   = 0;
        gameState.Finances.MortgageOutstanding = 0;

        // Reset season history slot (line 4556: ns=1)
        gameState.SeasonSlot = 1;

        // Generate a new squad and staff for the club
        var squadResult = GenerateStartingSquad(newDivision, rng);
        gameState.Squad                    = squadResult.Squad;
        gameState.Club.TeamMorale          = squadResult.TeamMorale;
        gameState.Finances.PlayerWageBill  = squadResult.PlayerWageBill;
        gameState.Finances.BankBalance     = squadResult.BankBalance;

        var staff = GenerateStartingStaff(rng);
        gameState.Coach               = staff.Coach;
        gameState.Physio              = staff.Physio;
        gameState.Scouts              = staff.Scouts;
        gameState.YouthTeam           = staff.YouthPlayers;
        gameState.Club.HasCoach       = true;
        gameState.Club.HasPhysio      = true;
        gameState.Club.ScoutCount     = staff.Scouts.Count;
        gameState.Club.YouthPlayerCount = staff.YouthPlayers.Count;

        gameState.CurrentOpponentIndex = FixtureSchedulerService.GetDivisionStartIndex(gameState.Club.Division);
    }

    // ── Join new club mid-season (post-sacking) ───────────────────────────────

    /// <summary>
    /// Transitions the manager to a new club mid-season after being sacked.
    ///
    /// Identical to <see cref="JoinNewClub"/> except the season clock
    /// (<c>CurrentWeek</c>, <c>FixturesPlayed</c>, <c>MatchesRemainingThisSeason</c>)
    /// is preserved so the current season continues. The caller is responsible for
    /// regenerating <c>Fixtures</c> and <c>CurrentLeague</c> for the new division.
    /// </summary>
    public static void JoinNewClubMidSeason(
        GameState gameState,
        string    newClubName,
        Division  newDivision,
        int       startingLeaguePosition,
        Random    rng)
    {
        // Capture old club name before overwriting
        string oldClubName = gameState.Club.Name;

        // Swap club identity
        gameState.Club.Name          = newClubName;
        gameState.Club.Division      = newDivision;
        gameState.Club.LeaguePosition = startingLeaguePosition;

        if ((int)newDivision > 2)
            gameState.Club.GroundImprovementCost = 0;

        // Reset all-time records
        gameState.Finances.RecordSigningFee    = 0;
        gameState.Finances.RecordSaleFee       = 0;
        gameState.RecordSigningName            = string.Empty;
        gameState.RecordSaleName               = string.Empty;
        gameState.HighestGoalscorerName        = string.Empty;
        gameState.MostAppearancesName          = string.Empty;
        gameState.HighestPaidPlayerName        = string.Empty;

        // Shift previous clubs list
        gameState.PreviousClubs[2] = gameState.PreviousClubs[1];
        gameState.PreviousClubs[1] = gameState.PreviousClubs[0];
        gameState.PreviousClubs[0] = oldClubName;

        // Reset cup state and financial ceilings (season clock preserved)
        SeasonService.ResetMatchState(gameState.LeagueCup, gameState.FACup, gameState.European, newDivision);
        gameState.CurrentMatch           = null;
        gameState.InEuropeanFriendlyTour = false;
        SeasonService.RecalculateDivisionFinancials(gameState.Finances, newDivision);

        gameState.Finances.SharePriceInPence    = 2_000 - (int)newDivision * 400;
        gameState.Finances.SharesOwned          = 100_000;
        gameState.Finances.LoanOutstanding      = 0;
        gameState.Finances.MortgageOutstanding  = 0;

        // Clear transfer slots
        for (int slot = 21; slot <= 26; slot++) gameState.Squad[slot] = null;
        gameState.TransferMarket.IncomingOffers.Clear();

        // Generate a new squad and staff
        var squadResult = GenerateStartingSquad(newDivision, rng);
        gameState.Squad                   = squadResult.Squad;
        gameState.Club.TeamMorale         = squadResult.TeamMorale;
        gameState.Finances.PlayerWageBill = squadResult.PlayerWageBill;
        gameState.Finances.BankBalance    = squadResult.BankBalance;

        var staff = GenerateStartingStaff(rng);
        gameState.Coach                  = staff.Coach;
        gameState.Physio                 = staff.Physio;
        gameState.Scouts                 = staff.Scouts;
        gameState.YouthTeam              = staff.YouthPlayers;
        gameState.Club.HasCoach          = true;
        gameState.Club.HasPhysio         = true;
        gameState.Club.ScoutCount        = staff.Scouts.Count;
        gameState.Club.YouthPlayerCount  = staff.YouthPlayers.Count;

        gameState.CurrentOpponentIndex = FixtureSchedulerService.GetDivisionStartIndex(newDivision);
    }

}

// ── Result types ──────────────────────────────────────────────────────────────

/// <summary>Output of <see cref="InitializationService.GenerateStartingSquad"/>.</summary>
public record SquadGenerationResult(
    Player?[] Squad,
    int       TeamMorale,
    double    PlayerWageBill,
    double    BankBalance);

/// <summary>Output of <see cref="InitializationService.GenerateStartingStaff"/>.</summary>
public record StartingStaff(
    Coach             Coach,
    Physio            Physio,
    List<Scout>       Scouts,
    List<YouthPlayer> YouthPlayers);
