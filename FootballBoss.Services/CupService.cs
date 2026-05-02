using FootballBoss.Models;

namespace FootballBoss.Services;

/// <summary>
/// Manages the FA Cup and League Cup bracket draws, round progression,
/// and result recording.
///
/// The cup system in FOOT.BAS uses two data structures:
///   L(64)    — a 64-slot bracket that holds the team indices currently in the
///              competition. Slots are filled at draw time and cleared on elimination.
///   Z(4,32)  — paired fixtures for each round:
///              Z(J, I)   = home team index
///              Z(J+1, I) = away team index
///              where J=1 for League Cup, J=3 for FA Cup.
///
/// Corresponds to subroutines 1100, 1237, 1249, and related logic in FOOT.BAS.
/// </summary>
public static class CupService
{
    // ── Constants ─────────────────────────────────────────────────────────────

    /// <summary>Total slots in the cup bracket array L(). Corresponds to L(64).</summary>
    public const int BracketSize = 64;

    /// <summary>Number of cup-only teams (indices 81–96 in Y$). Corresponds to RA=81+RND*16.</summary>
    public const int CupTeamPoolStart = 81;
    public const int CupTeamPoolSize  = 16;

    // ── Initial bracket setup (subroutine 1100, lines 815–841) ───────────────

    /// <summary>
    /// Populates the 64-slot cup bracket for the start of a competition.
    ///
    /// BASIC subroutine 1100:
    ///   1. Place 8 cup-specific teams (indices 81–96) into random bracket slots.
    ///   2. Fill the remaining 56 slots sequentially with league teams (indices 41–80).
    ///
    /// Returns the filled bracket as a 1-based int[65] (index 0 unused), matching
    /// the BASIC L(64) array layout.
    /// </summary>
    public static int[] SetupInitialBracket(IReadOnlyList<string> allTeamNames, Random rng)
    {
        int[] bracket        = new int[BracketSize + 1];   // 1-based, L(1..64)
        var   usedCupTeams   = new HashSet<int>();

        // Place 8 randomly chosen cup-only teams in random bracket positions
        for (int cupTeamCount = 1; cupTeamCount <= 8; cupTeamCount++)
        {
            int cupTeamIndex;
            do
            {
                cupTeamIndex = CupTeamPoolStart + rng.Next(CupTeamPoolSize);
            }
            while (usedCupTeams.Contains(cupTeamIndex));

            usedCupTeams.Add(cupTeamIndex);

            int bracketSlot;
            do
            {
                bracketSlot = 1 + rng.Next(BracketSize);
            }
            while (bracket[bracketSlot] != 0);

            bracket[bracketSlot] = cupTeamIndex;
        }

        // Fill remaining slots with league teams (indices 41–80, lower two divisions)
        int leagueTeamIndex = 41;
        for (int bracketSlot = 1; bracketSlot <= BracketSize && leagueTeamIndex <= 80; bracketSlot++)
        {
            if (bracket[bracketSlot] == 0)
            {
                bracket[bracketSlot] = leagueTeamIndex;
                leagueTeamIndex++;
            }
        }

        return bracket;
    }

    // ── Build round fixtures (subroutine 1237, lines 936–959) ────────────────

    /// <summary>
    /// Pairs up the remaining teams in the bracket into fixtures for the next round.
    /// Shuffles filled bracket slots randomly then assigns pairs to the Z fixture array.
    ///
    /// BASIC subroutine 1237:
    ///   Walk through L(F) in a random order; each consecutive pair forms a fixture.
    ///   Z(J, K) = first team, Z(J+1, K) = second team (J=1 LC, J=3 FA).
    ///
    /// Returns a list of <see cref="CupFixturePair"/> representing the draw.
    /// </summary>
    public static List<CupFixturePair> DrawRound(
        int[]                 bracket,
        IReadOnlyList<string> allTeamNames,
        Random                rng)
    {
        // Collect all filled slots and shuffle them
        var filledSlots = new List<int>();
        for (int slot = 1; slot <= BracketSize; slot++)
        {
            if (bracket[slot] != 0)
                filledSlots.Add(bracket[slot]);
        }

        Shuffle(filledSlots, rng);

        var fixtures = new List<CupFixturePair>();
        for (int pairIndex = 0; pairIndex + 1 < filledSlots.Count; pairIndex += 2)
        {
            int homeTeamIndex = filledSlots[pairIndex];
            int awayTeamIndex = filledSlots[pairIndex + 1];

            fixtures.Add(new CupFixturePair
            {
                HomeTeamIndex = homeTeamIndex,
                AwayTeamIndex = awayTeamIndex,
                HomeTeamName  = allTeamNames[homeTeamIndex],
                AwayTeamName  = allTeamNames[awayTeamIndex],
                HomeDivision  = GetDivisionForTeamIndex(homeTeamIndex),
                AwayDivision  = GetDivisionForTeamIndex(awayTeamIndex)
            });
        }

        return fixtures;
    }

    // ── Simulate non-player fixtures (lines 1223–1226) ───────────────────────

    /// <summary>
    /// Generates random scores for a cup fixture that does not involve the
    /// player's team and records the winner back into the bracket.
    ///
    /// BASIC lines 1260–1261:
    ///   homeScore = max(0, random based on division difference)
    ///   awayScore = max(0, random based on division difference)
    ///   Replay if drawn (marked with 'R'); winner goes into L(F).
    ///
    /// Returns the result with the winning team index.
    /// </summary>
    public static CupMatchResult SimulateFixture(
        CupFixturePair fixture,
        int[]          bracket,
        int            nextAvailableBracketSlot,
        Random         rng)
    {
        int divisionDifference = Math.Abs(fixture.HomeDivision - fixture.AwayDivision);
        bool homeFavoured      = fixture.HomeDivision < fixture.AwayDivision;

        int homeScore = Math.Max(0, rng.Next(7) - 1 - (homeFavoured ? 0 : divisionDifference));
        int awayScore = Math.Max(0, rng.Next(6) - 1 - (homeFavoured ? divisionDifference : 0));

        bool isReplay   = homeScore == awayScore;
        int winnerIndex = homeScore > awayScore ? fixture.HomeTeamIndex : fixture.AwayTeamIndex;

        // Line 1226: put winner into the bracket
        if (!isReplay)
            bracket[nextAvailableBracketSlot] = winnerIndex;

        return new CupMatchResult
        {
            HomeScore    = homeScore,
            AwayScore    = awayScore,
            IsReplay     = isReplay,
            WinnerIndex  = isReplay ? 0 : winnerIndex
        };
    }

    // ── Advance to next round (lines 1228–1249) ───────────────────────────────

    /// <summary>
    /// Advances the competition to the next round by:
    ///   1. Incrementing the round counter.
    ///   2. Rebuilding the bracket from the current winners.
    ///   3. If this is round 3 in a 64-team competition, redistributing the
    ///      bracket slots sequentially (line 1231–1234).
    ///
    /// Returns the new round number (9 = final won).
    /// </summary>
    public static int AdvanceRound(
        CupCompetition competition,
        int[]          bracket,
        Random         rng)
    {
        int newRound = (int)competition.CurrentRound + 1;
        competition.CurrentRound = (CupRound)Math.Min(newRound, (int)CupRound.Winner);

        if (newRound == (int)CupRound.Winner)
        {
            // Clear bracket — competition over (line 1249)
            Array.Clear(bracket, 0, bracket.Length);
        }
        else if (newRound == 3)
        {
            // Round 3: redistribute all winners into sequential bracket slots (lines 1231–1234)
            RedistributeBracketSequentially(bracket);
        }

        return newRound;
    }

    // ── Check if the player's team is in a specific fixture ───────────────────

    /// <summary>
    /// Searches the fixture list to find the tie involving the player's club.
    /// Returns null if the club is not in this round (already eliminated).
    ///
    /// BASIC line 879: OE = ABS(Y$(Z(J,I))=Z$ OR Y$(Z(J+1,I))=Z$)
    /// </summary>
    public static CupFixturePair? FindPlayerFixture(
        IReadOnlyList<CupFixturePair> fixtures,
        string playerClubName)
    {
        return fixtures.FirstOrDefault(
            f => f.HomeTeamName.Trim() == playerClubName.Trim()
              || f.AwayTeamName.Trim() == playerClubName.Trim());
    }

    // ── Record the player's cup result (lines 255–258, 250–254) ──────────────

    /// <summary>
    /// Records the result of the player's cup tie:
    ///   - Puts the winning team back into the bracket.
    ///   - Updates the cup round counter (CT/CR in BASIC).
    ///   - If a draw, flags for replay.
    ///
    /// Returns the outcome from the player's perspective.
    /// </summary>
    public static CupTieOutcome RecordPlayerResult(
        CupCompetition competition,
        int[]          bracket,
        string         playerClubName,
        string         opponentName,
        int            playerScore,
        int            opponentScore,
        bool           playerIsHome,
        Random         rng)
    {
        if (playerScore == opponentScore)
            return CupTieOutcome.Replay;

        bool playerWon = playerScore > opponentScore;

        // Find a free bracket slot for the winner (line 1226: L(F)=winner)
        int bracketSlot = FindFreeBracketSlot(bracket, rng);
        if (bracketSlot > 0)
            bracket[bracketSlot] = playerWon ? FindTeamInBracket(bracket, playerClubName) : -1;

        if (playerWon)
        {
            competition.RoundTracker++;
            return CupTieOutcome.PlayerWon;
        }

        return CupTieOutcome.PlayerEliminated;
    }

    // ── Season fixture log helpers (lines 954–957) ────────────────────────────

    /// <summary>
    /// Builds the fixture log string stored in A$(cupIndex, round).
    ///
    /// BASIC format: "H" or "A" + opponent name (9 chars) + "  " + divisionChar
    /// where divisionChar = CHR$(INT((teamIndex+19)/20) + 48), adjusted for 53.
    /// </summary>
    public static string BuildFixtureLogEntry(
        bool   isHome,
        string opponentName,
        int    opponentDivision)
    {
        char homeAwayFlag    = isHome ? 'H' : 'A';
        string paddedName    = opponentName.Length > 9
            ? opponentName[..9]
            : opponentName.PadRight(9);
        char divisionChar    = (char)('0' + opponentDivision);

        return $"{homeAwayFlag}{paddedName}  {divisionChar}";
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static void RedistributeBracketSequentially(int[] bracket)
    {
        var winners = bracket.Skip(1).Where(t => t != 0).ToList();
        Array.Clear(bracket, 1, BracketSize);
        for (int i = 0; i < winners.Count; i++)
            bracket[i + 1] = winners[i];
    }

    /// <summary>
    /// Maps a team's Y$ index to the division it belongs to.
    /// Div1=1–20, Div2=21–40, Div3=41–60, Div4=61–80, Cup-only=81+.
    /// Corresponds to INT((teamIndex+19)/20) used throughout FOOT.BAS.
    /// </summary>
    public static int GetDivisionForTeamIndex(int teamIndex)
    {
        if (teamIndex < 1)  return 0;
        if (teamIndex > 80) return 5;   // cup-only teams treated as division 5
        return (teamIndex + 19) / 20;
    }

    private static int FindFreeBracketSlot(int[] bracket, Random rng)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            int slot = 1 + rng.Next(BracketSize);
            if (bracket[slot] == 0) return slot;
        }
        return 0;
    }

    private static int FindTeamInBracket(int[] bracket, string teamName)
    {
        // Returns a placeholder — caller responsible for resolving team index
        return -1;
    }
}

// ── Data classes ─────────────────────────────────────────────────────────────

/// <summary>A drawn cup tie between two teams.</summary>
public class CupFixturePair
{
    public int    HomeTeamIndex { get; set; }
    public int    AwayTeamIndex { get; set; }
    public string HomeTeamName  { get; set; } = string.Empty;
    public string AwayTeamName  { get; set; } = string.Empty;
    public int    HomeDivision  { get; set; }
    public int    AwayDivision  { get; set; }
}

/// <summary>Outcome of a simulated cup match.</summary>
public class CupMatchResult
{
    public int  HomeScore   { get; set; }
    public int  AwayScore   { get; set; }
    public bool IsReplay    { get; set; }
    public int  WinnerIndex { get; set; }   // 0 if replay
}

public enum CupTieOutcome
{
    PlayerWon,
    PlayerEliminated,
    Replay
}
