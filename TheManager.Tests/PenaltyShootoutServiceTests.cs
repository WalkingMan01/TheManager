using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class PenaltyShootoutServiceTests
{
    private static Player?[] MakeSquad()
    {
        var squad = new Player?[29];
        squad[1] = new Player { Name = "KEEPER", Position = PlayerPosition.Goalkeeper, Skill = 5.0, Age = 25 };
        for (int slot = 2; slot <= 5; slot++)
            squad[slot] = new Player { Name = $"DEF{slot}", Position = PlayerPosition.Defender, Skill = 5.0, Age = 25 };
        for (int slot = 6; slot <= 8; slot++)
            squad[slot] = new Player { Name = $"MID{slot}", Position = PlayerPosition.Midfielder, Skill = 5.0, Age = 25 };
        for (int slot = 9; slot <= 11; slot++)
            squad[slot] = new Player { Name = $"ATK{slot}", Position = PlayerPosition.Attacker, Skill = 5.0, Age = 25 };
        return squad;
    }

    private static OpponentRatings MakeOpponent() => new()
    {
        GoalkeeperRating = 5, DefenceRating = 5, MidRating = 5, AttackRating = 5,
        Morale = 80, Temper = 40, LeaguePosition = 10, FormationCode = 442
    };

    // ── Kicking order ─────────────────────────────────────────────────────────

    [Fact]
    public void GetOurTakers_AttackersFirstGoalkeeperLast()
    {
        var takers = PenaltyShootoutService.GetOurTakers(MakeSquad());

        Assert.Equal(11, takers.Count);
        Assert.Equal("ATK11",  takers[0].Name);   // slot 11 first
        Assert.Equal("DEF2",   takers[9].Name);   // slot 2 second-to-last
        Assert.Equal("KEEPER", takers[10].Name);  // goalkeeper last
    }

    [Fact]
    public void GetOurTakers_SkipsEmptySlots()
    {
        var squad = MakeSquad();
        squad[10] = null;   // sent off / swapped out

        var takers = PenaltyShootoutService.GetOurTakers(squad);

        Assert.Equal(10, takers.Count);
        Assert.DoesNotContain(takers, t => t.Name == "ATK10");
    }

    // ── Shootout rules (invariants across many seeds) ─────────────────────────

    [Fact]
    public void Run_AlwaysProducesAWinner()
    {
        var squad    = MakeSquad();
        var opponent = MakeOpponent();

        for (int seed = 0; seed < 200; seed++)
        {
            var result = PenaltyShootoutService.Run(squad, "Rivals", opponent, new Random(seed));
            Assert.NotEqual(result.OurScore, result.TheirScore);
            Assert.Equal(result.OurScore > result.TheirScore, result.WeWon);
        }
    }

    [Fact]
    public void Run_BestOfFive_NeverExceedsFiveKicksPerSideBeforeSuddenDeath()
    {
        var squad    = MakeSquad();
        var opponent = MakeOpponent();

        for (int seed = 0; seed < 200; seed++)
        {
            var result = PenaltyShootoutService.Run(squad, "Rivals", opponent, new Random(seed));

            var regulation = result.Kicks.Where(k => !k.IsSuddenDeath).ToList();
            Assert.True(regulation.Count(k => k.IsOurKick)  <= 5);
            Assert.True(regulation.Count(k => !k.IsOurKick) <= 5);
        }
    }

    [Fact]
    public void Run_KicksAlternateSides()
    {
        var squad    = MakeSquad();
        var opponent = MakeOpponent();

        for (int seed = 0; seed < 50; seed++)
        {
            var result = PenaltyShootoutService.Run(squad, "Rivals", opponent, new Random(seed));

            for (int i = 1; i < result.Kicks.Count; i++)
                Assert.NotEqual(result.Kicks[i - 1].IsOurKick, result.Kicks[i].IsOurKick);
        }
    }

    [Fact]
    public void Run_SuddenDeath_OnlyEnteredWhenLevelAfterFiveEach()
    {
        var squad    = MakeSquad();
        var opponent = MakeOpponent();
        bool sawSuddenDeath = false;

        for (int seed = 0; seed < 500; seed++)
        {
            var result = PenaltyShootoutService.Run(squad, "Rivals", opponent, new Random(seed));
            if (!result.WentToSuddenDeath) continue;

            sawSuddenDeath = true;

            // Level at the moment regulation ended.
            var lastRegulation = result.Kicks.Last(k => !k.IsSuddenDeath);
            Assert.Equal(lastRegulation.OurRunningScore, lastRegulation.TheirRunningScore);

            // Sudden death ends the moment a round is split — the final two
            // kicks are one per side with exactly one scoring.
            var lastTwo = result.Kicks.TakeLast(2).ToList();
            Assert.True(lastTwo[0].IsSuddenDeath && lastTwo[1].IsSuddenDeath);
            Assert.NotEqual(lastTwo[0].IsOurKick, lastTwo[1].IsOurKick);
            Assert.Equal(1, lastTwo.Count(k => k.Scored));
        }

        Assert.True(sawSuddenDeath, "expected at least one shootout to reach sudden death");
    }

    [Fact]
    public void Run_TakersCycle_WhenSuddenDeathOutlastsTheSquad()
    {
        var squad    = MakeSquad();
        var opponent = MakeOpponent();

        for (int seed = 0; seed < 2000; seed++)
        {
            var result = PenaltyShootoutService.Run(squad, "Rivals", opponent, new Random(seed));
            var ourKicks = result.Kicks.Where(k => k.IsOurKick).ToList();
            if (ourKicks.Count <= 11) continue;

            // The 12th kick cycles back to the first taker (slot 11).
            Assert.Equal("ATK11", ourKicks[11].TakerName);
            return;
        }

        Assert.Fail("expected at least one shootout to cycle past 11 kicks");
    }

    [Fact]
    public void Run_ScoringChance_Is70PercentPlusSkill()
    {
        // A team of 9.9-skill players should convert close to 79.9% of kicks.
        var squad = MakeSquad();
        foreach (var p in squad.Where(p => p != null))
            p!.Skill = 9.9;

        var opponent = MakeOpponent();
        int ourKicks = 0, ourGoals = 0;

        for (int seed = 0; seed < 2000; seed++)
        {
            var result = PenaltyShootoutService.Run(squad, "Rivals", opponent, new Random(seed));
            ourKicks += result.Kicks.Count(k => k.IsOurKick);
            ourGoals += result.Kicks.Count(k => k.IsOurKick && k.Scored);
        }

        double rate = (double)ourGoals / ourKicks;
        Assert.InRange(rate, 0.77, 0.83);
    }
}
