using System.Runtime.CompilerServices;
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
    private const int MORALE_MINIMUM = 60;  // ToDo: Was 30 in original
    private const int MORALE_RANGE = 20;    // ToDo: Was 20 in original

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
    public static void GenerateStartingSquad(GameState gameState, Random random)
    {
        var club     = gameState.Club;
        var   finances = gameState.Finances;
        int   divNum   = (int)club.Division;
        int squadSize = 16; // ToDo: Changed to 16               // 12–16 (RZ)
        //int   squadSize = 12 + rng.Next(5);               // 12–16 (RZ)

        club.TeamMorale = MORALE_MINIMUM + random.Next(MORALE_RANGE);               // me

        for (int slot = 1; slot <= 20; slot++)
        {
            if (slot > squadSize)
            {
                gameState.Squad[slot] = null;
                continue;
            }

            var player = new Player();

            // Position assignment — matches BASIC line 1074
            player.Position = slot switch
            {
                1                          => PlayerPosition.Goalkeeper,
                >= 2 and <= 5              => PlayerPosition.Defender,
                >= 6 and <= 8              => PlayerPosition.Midfielder,
                >= 9 and <= 11             => PlayerPosition.Attacker,
                _                          => (PlayerPosition)(1 + random.Next(4))
            };

            // Skill: |division-5| + 0.0–3.9  (line 1073: H(Y)=ABS(AP-5)+RND*39/10)
            player.Skill = Math.Abs(divNum - 5) + random.Next(39) / 10.0;

            // Age 18–35 (line 1076: G(Y)=18+INT(RND*18))
            player.Age = 18 + random.Next(18);

            // Games played this season (line 1086: x(Y)=30+RND*(((G-17)*30)-30))
            int gamesRange    = Math.Max(0, (player.Age - 17) * 30 - 30);
            player.GamesPlayed = gamesRange > 0 ? 30 + random.Next(gamesRange) : 30;

            // Temper 0–9 (lines 1088–1089)
            int rawTemper  = -3 + random.Next(17);
            player.Temper  = Math.Max(0, Math.Min(9, rawTemper));

            // Wage: base = 51–70, multiplied by INT(skill), divided by age factor (lines 1077–1082)
            // V(1,Y)=1+INT(RND*20)+50; V(1,Y)=INT(V(1,Y))*INT(H(Y)); V(1,Y)/=HV; min 50
            int    ageDivisor  = Math.Max(1, player.Age - 27);
            double wageBase    = (1 + random.Next(20) + 50) * (int)player.Skill
                                 + (player.Skill > 9.6 ? 1_000 : 0);
            player.WeeklyWage  = Math.Max(50, (int)(wageBase / ageDivisor));

            // Contract: 20–75 weeks remaining (line 1084: V(2,Y)=20+INT(RND*56))
            player.ContractWeeks = 20 + random.Next(56);

            // Name
            player.Name = NameGenerationService.GenerateName(random);

            PlayerService.RecalculateStatus(player);
            gameState.Squad[slot] = player;
        }

        // Total wage bill (line 1083: NP=INT(NP+V(1,Y)))
        gameState.Finances.PlayerWageBill =
            gameState.Squad.Skip(1).Take(20)
                .Where(p => p is not null)
                .Sum(p => p!.WeeklyWage);

        // Financial setup (lines 1104–1110)
        int   leagueBonus = (int)((150 + random.Next(200)) / (double)divNum);
        int   cupBonus    = (int)((200 + random.Next(300)) / (double)divNum);
        double bankBalance = 150_000 + random.Next((int)(500_000.0 / divNum));

        club.TeamMorale = Math.Max(2, Math.Min(99, club.TeamMorale));

        // Store bonuses in finance object (NW/NV via Finances extension)
        gameState.Finances.BankBalance = (int)bankBalance;
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
    public static void GenerateStartingStaff(GameState gameState, Random rng)
    {
        // Coach and physio always present
        gameState.Coach = StaffService.GenerateCoach(rng);
        gameState.Club.HasCoach = true;

        gameState.Physio = StaffService.GeneratePhysio(rng);
        gameState.Club.HasPhysio = true;

        // 0–3 scouts (line 5580: NN=INT(RND*4); if NN=0 goto 5607)
        int scoutCount = rng.Next(4);
        gameState.Scouts.Clear();
        for (int i = 0; i < scoutCount; i++)
            gameState.Scouts.Add(StaffService.GenerateScout(rng));
        gameState.Club.ScoutCount = scoutCount;

        // 0–4 youth players (subroutine 5623)
        int youthCount = rng.Next(5);
        gameState.YouthTeam.Clear();
        for (int i = 0; i < youthCount; i++)
            gameState.YouthTeam.Add(StaffService.GenerateYouthPlayer(rng));
        gameState.Club.YouthPlayerCount = youthCount;
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
        gameState.Club.Name           = clubName.PadRight(9)[..9];
        gameState.Club.Division       = division;
        gameState.Club.ManagerName    = managerName;
        gameState.Club.PointsPerWin   = 3;
        gameState.Club.TicketPriceInPounds = 5 - (int)division;  // line 5620: nj=1+(4-AP)

        SeasonService.RecalculateDivisionFinancials(gameState.Finances, division);
        gameState.Finances.SharePriceInPence = 2_000 - (int)division * 400;  // line 5442: AK=2000-(AP*400)
        gameState.Finances.SharesOwned       = 100_000;

        // Generate starting squad and staff
        GenerateStartingSquad(gameState, rng);
        TeamData.Seed(gameState);
        GenerateStartingStaff(gameState, rng);

        // Cup draws (BASIC lines 4900–4904)
        var lcBracket = CupService.SetupInitialBracket(gameState.AllTeamNames, rng);
        gameState.LeagueCup.CurrentRoundFixtures =
            [..CupService.DrawRound(lcBracket, gameState.AllTeamNames, rng).Select(ToCupFixture)];

        var faBracket = CupService.SetupInitialBracket(gameState.AllTeamNames, rng);
        gameState.FACup.CurrentRoundFixtures =
            [..CupService.DrawRound(faBracket, gameState.AllTeamNames, rng).Select(ToCupFixture)];

        // Reset counters
        gameState.CurrentWeek              = 1;
        gameState.FixturesPlayed           = 0;
        gameState.MatchesRemainingThisSeason = 38;
        gameState.SeasonsPlayed            = 0;
        gameState.SeasonSlot               = 1;
        gameState.Club.ManagerContractWeeks = 52;   // initial 1-season contract

        FixtureSchedulerService.ResetOpponentPointer(gameState);

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
        gameState.Club.Name     = newClubName.PadRight(9)[..9];
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
        SeasonService.ResetMatchState(gameState);
        SeasonService.RecalculateDivisionFinancials(gameState.Finances, newDivision);

        gameState.Finances.SharePriceInPence = 2_000 - (int)newDivision * 400;
        gameState.Finances.SharesOwned       = 100_000;
        gameState.Finances.LoanOutstanding   = 0;
        gameState.Finances.MortgageOutstanding = 0;

        // Reset season history slot (line 4556: ns=1)
        gameState.SeasonSlot = 1;

        // Generate a new squad for the club
        GenerateStartingSquad(gameState, rng);
        GenerateStartingStaff(gameState, rng);

        FixtureSchedulerService.ResetOpponentPointer(gameState);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CupFixture ToCupFixture(CupFixturePair pair) => new()
    {
        HomeTeam     = pair.HomeTeamName,
        AwayTeam     = pair.AwayTeamName,
        HomeDivision = (Division)Math.Clamp(pair.HomeDivision, 1, 4),
        AwayDivision = (Division)Math.Clamp(pair.AwayDivision, 1, 4)
    };
}
