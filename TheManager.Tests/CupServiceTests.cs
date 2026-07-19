using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class CupServiceTests
{
    private static string[] MakeTeamNames()
    {
        var state = new GameState();
        TeamData.Seed(state);
        return state.AllTeamNames;
    }

    // ── Bracket composition ───────────────────────────────────────────────────

    [Fact]
    public void SetupInitialBracket_Holds80Teams()
    {
        var bracket = CupService.SetupInitialBracket();
        Assert.Equal(80, bracket.Count(t => t != 0));
    }

    [Fact]
    public void SetupInitialBracket_ContainsAllDivisionThreeAndFourTeams()
    {
        var bracket = CupService.SetupInitialBracket();
        var teams   = new HashSet<int>(bracket.Where(t => t != 0));

        for (int teamIndex = 45; teamIndex <= 92; teamIndex++)
            Assert.Contains(teamIndex, teams);
    }

    [Fact]
    public void SetupInitialBracket_ContainsAll32NonLeagueTeams()
    {
        var bracket = CupService.SetupInitialBracket();
        var teams   = new HashSet<int>(bracket.Where(t => t != 0));

        for (int teamIndex = 93; teamIndex <= 124; teamIndex++)
            Assert.Contains(teamIndex, teams);
    }

    [Fact]
    public void SetupInitialBracket_ExcludesTopTwoDivisions()
    {
        var bracket = CupService.SetupInitialBracket();
        Assert.DoesNotContain(bracket.Where(t => t != 0), t => t <= 44);
    }

    // ── The draw ──────────────────────────────────────────────────────────────

    [Fact]
    public void DrawRound_FullBracket_Produces40Ties()
    {
        var names   = MakeTeamNames();
        var bracket = CupService.SetupInitialBracket();

        var fixtures = CupService.DrawRound(bracket, names, new Random(1));

        Assert.Equal(40, fixtures.Count);
    }

    [Fact]
    public void DrawRound_EveryTeamAppearsExactlyOnce()
    {
        var names   = MakeTeamNames();
        var bracket = CupService.SetupInitialBracket();

        var fixtures = CupService.DrawRound(bracket, names, new Random(1));

        var seen = fixtures.SelectMany(f => new[] { f.HomeTeamIndex, f.AwayTeamIndex }).ToList();
        Assert.Equal(80, seen.Count);
        Assert.Equal(80, seen.Distinct().Count());
    }

    // ── Round progression: 80 → 40 → 20 → merge → 64 → … → winner ────────────

    [Fact]
    public void FullCompetition_FieldSizesFollowTheRealFACupShape()
    {
        var names = MakeTeamNames();
        var rng   = new Random(7);
        var cup   = new CupCompetition { Type = CupType.FACup, Bracket = CupService.SetupInitialBracket() };
        cup.CurrentRoundFixtures = CupService.DrawRound(cup.Bracket, names, rng);

        int[] expectedTiesPerRound = [40, 20, 32, 16, 8, 4, 2, 1];

        for (int roundIndex = 0; roundIndex < 8; roundIndex++)
        {
            CupService.EnsureRoundDrawn(cup, names, "Nobody FC", roundIndex, rng);
            Assert.Equal(expectedTiesPerRound[roundIndex], cup.CurrentRoundFixtures.Count);
            CupService.CompleteRound(cup, rng);
        }

        Assert.Equal(8, cup.RoundHistory.Count);
        Assert.Equal(1, cup.Bracket.Count(t => t != 0));   // one winner remains
    }

    [Fact]
    public void MergeTopDivisions_AtRound3_Creates64TeamField()
    {
        var names = MakeTeamNames();
        var rng   = new Random(3);
        var cup   = new CupCompetition { Type = CupType.FACup, Bracket = CupService.SetupInitialBracket() };
        cup.CurrentRoundFixtures = CupService.DrawRound(cup.Bracket, names, rng);

        CupService.CompleteRound(cup, rng);                       // R1: 80 → 40
        CupService.EnsureRoundDrawn(cup, names, "Nobody FC", 1, rng);
        CupService.CompleteRound(cup, rng);                       // R2: 40 → 20
        Assert.Equal(20, cup.Bracket.Count(t => t != 0));

        CupService.EnsureRoundDrawn(cup, names, "Nobody FC", 2, rng);

        Assert.Equal(32, cup.CurrentRoundFixtures.Count);         // 64 teams
        var r3Teams = cup.CurrentRoundFixtures
            .SelectMany(f => new[] { f.HomeTeamIndex, f.AwayTeamIndex })
            .ToHashSet();
        for (int teamIndex = 1; teamIndex <= 44; teamIndex++)
            Assert.Contains(teamIndex, r3Teams);                  // all Div 1/2 clubs entered
    }

    [Fact]
    public void CompleteRound_RecordsResultsInRoundHistory()
    {
        var names = MakeTeamNames();
        var rng   = new Random(11);
        var cup   = new CupCompetition { Type = CupType.FACup, Bracket = CupService.SetupInitialBracket() };
        cup.CurrentRoundFixtures = CupService.DrawRound(cup.Bracket, names, rng);

        var results = CupService.CompleteRound(cup, rng);

        Assert.Single(cup.RoundHistory);
        Assert.Equal(CupRound.Round1, cup.RoundHistory[0].Round);
        Assert.Equal(40, cup.RoundHistory[0].Results.Count);
        Assert.All(results, tie => Assert.False(string.IsNullOrEmpty(tie.Winner)));
        Assert.All(results, tie => Assert.True(tie.HomeScore.HasValue && tie.AwayScore.HasValue));
    }

    [Fact]
    public void CompleteRound_BracketMatchesLastRoundWinners()
    {
        var names = MakeTeamNames();
        var rng   = new Random(13);
        var cup   = new CupCompetition { Type = CupType.FACup, Bracket = CupService.SetupInitialBracket() };
        cup.CurrentRoundFixtures = CupService.DrawRound(cup.Bracket, names, rng);

        var results = CupService.CompleteRound(cup, rng);

        var winners = results
            .Select(t => t.Winner.Trim() == t.HomeTeam.Trim() ? t.HomeTeamIndex : t.AwayTeamIndex)
            .ToHashSet();
        var bracketTeams = cup.Bracket.Where(t => t != 0).ToHashSet();

        Assert.Equal(winners, bracketTeams);
    }

    [Fact]
    public void CompleteRound_UnpairedTeam_GetsAByeIntoNextRound()
    {
        var names = MakeTeamNames();
        var rng   = new Random(17);
        var cup   = new CupCompetition { Type = CupType.FACup };

        // A 5-team bracket: two ties are drawn, one team is left unpaired.
        cup.Bracket = new int[CupService.BracketSize + 1];
        for (int i = 0; i < 5; i++) cup.Bracket[i + 1] = 45 + i;
        cup.CurrentRoundFixtures = CupService.DrawRound(cup.Bracket, names, rng);
        Assert.Equal(2, cup.CurrentRoundFixtures.Count);

        CupService.CompleteRound(cup, rng);

        Assert.Equal(3, cup.Bracket.Count(t => t != 0));   // 2 winners + 1 bye
    }

    [Fact]
    public void SimulateTie_LevelScores_AreDecidedOnPenalties()
    {
        var names = MakeTeamNames();
        var rng   = new Random(0);

        // Run enough ties that some end level after 90 minutes.
        bool sawPenalties = false;
        for (int i = 0; i < 200 && !sawPenalties; i++)
        {
            var tie = new CupFixture
            {
                HomeTeamIndex = 45, AwayTeamIndex = 46,
                HomeTeam = names[45], AwayTeam = names[46]
            };
            CupService.SimulateTie(tie, rng);

            Assert.False(string.IsNullOrEmpty(tie.Winner));
            if (tie.WonOnPenalties)
            {
                sawPenalties = true;
                Assert.Equal(tie.HomeScore, tie.AwayScore);
                Assert.NotEqual(tie.HomePenalties, tie.AwayPenalties);
            }
        }

        Assert.True(sawPenalties, "expected at least one tie to go to penalties");
    }

    // ── Round mapping ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, CupRound.Round1)]
    [InlineData(1, CupRound.Round2)]
    [InlineData(2, CupRound.Round3)]
    [InlineData(3, CupRound.Round4)]
    [InlineData(4, CupRound.Round5)]
    [InlineData(5, CupRound.QuarterFinal)]
    [InlineData(6, CupRound.SemiFinal)]
    [InlineData(7, CupRound.Final)]
    public void RoundForIndex_MapsAllEightRounds(int index, CupRound expected)
    {
        Assert.Equal(expected, CupService.RoundForIndex(index));
    }

    [Theory]
    [InlineData(CupRound.QuarterFinal, false)]
    [InlineData(CupRound.SemiFinal,    true)]
    [InlineData(CupRound.Final,        true)]
    public void IsNeutralVenue_OnlySemiFinalAndFinalAreAtWembley(CupRound round, bool expected)
    {
        Assert.Equal(expected, CupService.IsNeutralVenue(round));
    }
}
