using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Manages the FA Cup (and League Cup) bracket, round draws, simulated results,
/// and round progression.
///
/// The competition follows the real FA Cup shape rather than the original's flat
/// 64-team bracket: round 1 holds 80 teams (all 48 Division Three/Four clubs plus
/// 32 non-league sides), rounds 1–2 reduce that to 20, and at round 3 all 44
/// Division One/Two clubs enter to make a 64-team field — clean powers of two
/// from there (64 → 32 → 16 → 8 → 4 → 2).
///
/// Corresponds to subroutines 1100, 1237, 1249 in FOOT.BAS (bracket L(), fixture
/// pairs Z(), random-walk draw), extended per docs/specs/fa-cup.md.
/// </summary>
public static class CupService
{
    // ── Constants ─────────────────────────────────────────────────────────────

    /// <summary>Bracket capacity — the 80-team round 1. Extends L(64) in FOOT.BAS.</summary>
    public const int BracketSize = CupCompetition.BracketSize;

    /// <summary>First index of the non-league (cup-only) pool in AllTeamNames.</summary>
    public const int CupTeamPoolStart = 93;

    /// <summary>Number of non-league teams entering round 1.</summary>
    public const int CupTeamPoolSize = 32;

    /// <summary>Round-3 entrants: every Division One and Two club (indices 1–44).</summary>
    private const int TopDivisionTeamCount = 44;

    /// <summary>0-based round index (into Constants.FACupMatchdays) at which Divisions One/Two enter.</summary>
    public const int TopDivisionEntryRoundIndex = 2;

    // ── Round mapping ─────────────────────────────────────────────────────────

    /// <summary>
    /// Maps a 0-based cup-matchday index to its round:
    /// R1, R2, R3, R4, R5, QF, SF, Final.
    /// </summary>
    public static CupRound RoundForIndex(int roundIndex) => roundIndex switch
    {
        0 => CupRound.Round1,
        1 => CupRound.Round2,
        2 => CupRound.Round3,
        3 => CupRound.Round4,
        4 => CupRound.Round5,
        5 => CupRound.QuarterFinal,
        6 => CupRound.SemiFinal,
        7 => CupRound.Final,
        _ => CupRound.NotEntered
    };

    /// <summary>Display name for a round ("Round 3", "Semi Final", …).</summary>
    public static string RoundDisplayName(CupRound round) => round switch
    {
        CupRound.QuarterFinal => "Quarter Final",
        CupRound.SemiFinal    => "Semi Final",
        CupRound.Final        => "Final",
        CupRound.Winner       => "Winner",
        CupRound.NotEntered   => "—",
        _                     => $"Round {(int)round}"
    };

    /// <summary>Semi-finals and the final are played at Wembley (neutral venue).</summary>
    public static bool IsNeutralVenue(CupRound round)
        => round is CupRound.SemiFinal or CupRound.Final;

    // ── Initial bracket (subroutine 1100, reshaped) ───────────────────────────

    /// <summary>
    /// Builds the 80-team round-1 bracket: all Division Three/Four clubs
    /// (indices 45–92) plus all 32 non-league sides (93–124). Division One/Two
    /// clubs are absent — they enter at round 3 via <see cref="MergeTopDivisions"/>.
    /// Returns a 1-based int[81] matching the BASIC L() layout (0 = empty).
    /// </summary>
    public static int[] SetupInitialBracket()
    {
        int[] bracket = new int[BracketSize + 1];
        int slot = 1;

        var (div3Start, _) = Constants.DivisionRange(Division.Three);
        var (_, div4End)   = Constants.DivisionRange(Division.Four);
        for (int teamIndex = div3Start; teamIndex <= div4End; teamIndex++)
            bracket[slot++] = teamIndex;

        for (int i = 0; i < CupTeamPoolSize; i++)
            bracket[slot++] = CupTeamPoolStart + i;

        return bracket;
    }

    /// <summary>
    /// Round-3 entry: adds every Division One and Two club (indices 1–44) to the
    /// bracket alongside the round-2 survivors. Generalises BASIC line 1723
    /// (player-only late entry) to the whole top two divisions.
    /// </summary>
    public static void MergeTopDivisions(int[] bracket)
    {
        var present = new HashSet<int>(bracket.Where(t => t != 0));
        int slot = 1;

        for (int teamIndex = 1; teamIndex <= TopDivisionTeamCount; teamIndex++)
        {
            if (present.Contains(teamIndex)) continue;

            while (slot <= BracketSize && bracket[slot] != 0) slot++;
            if (slot > BracketSize) break;   // bracket full — cannot happen with 20 survivors
            bracket[slot] = teamIndex;
        }
    }

    // ── The draw (subroutine 1237) ────────────────────────────────────────────

    /// <summary>
    /// Pairs every team in the bracket into ties in a random order.
    /// With this competition's shape every field is even (80/40/64/32/16/8/4/2);
    /// if an odd team were ever left over it simply stays unpaired and
    /// <see cref="CompleteRound"/> gives it a bye into the next round.
    /// </summary>
    public static List<CupFixture> DrawRound(
        int[]                 bracket,
        IReadOnlyList<string> allTeamNames,
        Random                rng)
    {
        var teams = new List<int>();
        for (int slot = 1; slot <= BracketSize; slot++)
        {
            if (bracket[slot] != 0)
                teams.Add(bracket[slot]);
        }

        Shuffle(teams, rng);

        var fixtures = new List<CupFixture>();
        for (int pairIndex = 0; pairIndex + 1 < teams.Count; pairIndex += 2)
        {
            int homeIndex = teams[pairIndex];
            int awayIndex = teams[pairIndex + 1];

            fixtures.Add(new CupFixture
            {
                HomeTeamIndex = homeIndex,
                AwayTeamIndex = awayIndex,
                HomeTeam      = allTeamNames[homeIndex],
                AwayTeam      = allTeamNames[awayIndex],
                HomeDivision  = (Division)Math.Clamp(GetDivisionForTeamIndex(homeIndex), 1, 4),
                AwayDivision  = (Division)Math.Clamp(GetDivisionForTeamIndex(awayIndex), 1, 4)
            });
        }

        return fixtures;
    }

    // ── Simulating AI ties (lines 1260–1261, replay abstracted) ───────────────

    /// <summary>
    /// Fills in a decisive result for a tie not involving the player. Scores use
    /// the BASIC division-difference formula; a level tie is decided on penalties
    /// (the original's replays are abstracted away — deviation 2 in the spec).
    /// </summary>
    public static void SimulateTie(CupFixture tie, Random rng)
    {
        int homeDiv = GetDivisionForTeamIndex(tie.HomeTeamIndex);
        int awayDiv = GetDivisionForTeamIndex(tie.AwayTeamIndex);
        int divisionDifference = Math.Abs(homeDiv - awayDiv);
        bool homeFavoured      = homeDiv < awayDiv;

        int homeScore = Math.Max(0, rng.Next(7) - 1 - (homeFavoured ? 0 : divisionDifference));
        int awayScore = Math.Max(0, rng.Next(6) - 1 - (homeFavoured ? divisionDifference : 0));

        tie.HomeScore = homeScore;
        tie.AwayScore = awayScore;

        if (homeScore != awayScore)
        {
            tie.Winner = homeScore > awayScore ? tie.HomeTeam : tie.AwayTeam;
            return;
        }

        // Level — decided on penalties. Slight edge to the higher-division side.
        int homeChance   = 50 + (awayDiv - homeDiv) * 10;
        bool homeWinsPens = rng.Next(100) < homeChance;

        int winnerPens = 3 + rng.Next(3);                          // 3–5
        int loserPens  = Math.Max(0, winnerPens - 1 - rng.Next(2)); // 1–2 behind

        tie.WonOnPenalties = true;
        tie.HomePenalties  = homeWinsPens ? winnerPens : loserPens;
        tie.AwayPenalties  = homeWinsPens ? loserPens : winnerPens;
        tie.Winner         = homeWinsPens ? tie.HomeTeam : tie.AwayTeam;
    }

    // ── Round progression ─────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the round with 0-based index <paramref name="roundIndex"/> is drawn
    /// and waiting in <see cref="CupCompetition.CurrentRoundFixtures"/>. Any rounds
    /// missed entirely (legacy saves) are simulated first, keeping the player's
    /// club alive through them. The round-3 top-division merge happens here.
    /// </summary>
    public static void EnsureRoundDrawn(
        CupCompetition cup,
        string[]       allTeamNames,
        string         playerClubName,
        int            roundIndex,
        Random         rng)
    {
        while (cup.RoundHistory.Count < roundIndex)
        {
            DrawIfPending(cup, allTeamNames, rng);
            ForcePlayerWin(cup, playerClubName, rng);
            CompleteRound(cup, rng);
        }

        DrawIfPending(cup, allTeamNames, rng);
    }

    /// <summary>
    /// Finishes the current round: simulates every tie not yet decided (the
    /// player's tie, if any, must already carry its result), appends the round to
    /// <see cref="CupCompetition.RoundHistory"/>, rebuilds the bracket from the
    /// winners (an unpaired team receives a bye), and clears the fixture list.
    /// Returns the completed round's results.
    /// </summary>
    public static List<CupFixture> CompleteRound(CupCompetition cup, Random rng)
    {
        foreach (var tie in cup.CurrentRoundFixtures)
        {
            if (string.IsNullOrEmpty(tie.Winner))
                SimulateTie(tie, rng);
        }

        var results = cup.CurrentRoundFixtures;
        cup.RoundHistory.Add(new CupRoundRecord
        {
            Round   = RoundForIndex(cup.RoundHistory.Count),
            Results = results
        });

        // Winners + any bracket team that was in no tie this round (bye guard).
        var winners = results
            .Select(t => t.Winner.Trim() == t.HomeTeam.Trim() ? t.HomeTeamIndex : t.AwayTeamIndex)
            .ToList();
        var paired = new HashSet<int>(
            results.SelectMany(t => new[] { t.HomeTeamIndex, t.AwayTeamIndex }));
        winners.AddRange(cup.Bracket.Where(t => t != 0 && !paired.Contains(t)));

        Array.Clear(cup.Bracket, 0, cup.Bracket.Length);
        for (int i = 0; i < winners.Count && i < BracketSize; i++)
            cup.Bracket[i + 1] = winners[i];

        cup.CurrentRoundFixtures = new List<CupFixture>();
        return results;
    }

    /// <summary>
    /// Finds the tie involving the player's club in the current round.
    /// Null when the club is not in the round (eliminated or not yet entered).
    /// BASIC line 879.
    /// </summary>
    public static CupFixture? FindPlayerFixture(
        IReadOnlyList<CupFixture> fixtures,
        string playerClubName)
    {
        string trimmed = playerClubName.Trim();
        return fixtures.FirstOrDefault(
            f => f.HomeTeam.Trim().Equals(trimmed, StringComparison.OrdinalIgnoreCase)
              || f.AwayTeam.Trim().Equals(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    // ── Team classification ───────────────────────────────────────────────────

    /// <summary>
    /// Maps a team's AllTeamNames index to a division number.
    /// Div1=[1–20], Div2=[21–44], Div3=[45–68], Div4=[69–92], non-league=5.
    /// </summary>
    public static int GetDivisionForTeamIndex(int teamIndex)
    {
        if (teamIndex < 1)  return 0;
        if (teamIndex <= 20) return 1;
        if (teamIndex <= 44) return 2;
        if (teamIndex <= 68) return 3;
        if (teamIndex <= 92) return 4;
        return 5;   // non-league (cup-only) teams
    }

    /// <summary>True for cup-only non-league teams (indices 93+).</summary>
    public static bool IsNonLeague(int teamIndex) => teamIndex >= CupTeamPoolStart;

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void DrawIfPending(CupCompetition cup, string[] allTeamNames, Random rng)
    {
        if (cup.CurrentRoundFixtures.Count > 0) return;
        if (cup.Bracket.All(t => t == 0)) return;   // competition over / not set up

        if (cup.RoundHistory.Count == TopDivisionEntryRoundIndex)
            MergeTopDivisions(cup.Bracket);

        cup.CurrentRoundFixtures = DrawRound(cup.Bracket, allTeamNames, rng);
    }

    /// <summary>
    /// Legacy-save catch-up only: if the player's club is in the round being
    /// fast-forwarded, hand it a win so a migrated save never silently knocks
    /// the player out.
    /// </summary>
    private static void ForcePlayerWin(CupCompetition cup, string playerClubName, Random rng)
    {
        var tie = FindPlayerFixture(cup.CurrentRoundFixtures, playerClubName);
        if (tie == null) return;

        bool playerIsHome = tie.HomeTeam.Trim().Equals(playerClubName.Trim(), StringComparison.OrdinalIgnoreCase);
        int winScore  = 1 + rng.Next(3);
        int loseScore = Math.Max(0, winScore - 1 - rng.Next(2));

        tie.HomeScore = playerIsHome ? winScore : loseScore;
        tie.AwayScore = playerIsHome ? loseScore : winScore;
        tie.Winner    = playerIsHome ? tie.HomeTeam : tie.AwayTeam;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
