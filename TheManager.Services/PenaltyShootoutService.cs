using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Simulates a penalty shootout for the player's drawn cup ties, pre-computing
/// every kick so the UI can reveal them one at a time (the same approach
/// <see cref="MatchEngineService"/> uses for goal events).
///
/// Rules: best of five, alternating kicks; ends early once a side cannot be
/// caught; level after five goes to sudden death (one kick each per round).
/// Our kicking order is attackers first, goalkeeper last (squad slots 11 → 2,
/// then 1), cycling once every on-pitch player has taken one.
///
/// No FOOT.BAS equivalent — the original replayed drawn cup ties instead.
/// </summary>
public static class PenaltyShootoutService
{
    /// <summary>Base scoring chance in percent; the taker's skill is added on top.</summary>
    private const double BaseScoreChancePercent = 70.0;

    private const int KicksPerSide = 5;

    /// <summary>Safety cap on sudden-death rounds (the RNG makes this unreachable in practice).</summary>
    private const int MaxSuddenDeathRounds = 50;

    /// <summary>
    /// Runs a full shootout. <paramref name="squad"/> supplies our takers: the
    /// players still on the pitch at full time (slots 1–11), ordered attackers
    /// first and goalkeeper last. Opponent kicks use
    /// <see cref="OpponentRatings.AttackRating"/> in place of skill and are
    /// labelled "&lt;club&gt; — kick n" (the opposition has no named players).
    /// </summary>
    public static PenaltyShootoutResult Run(
        Player?[]       squad,
        string          opponentName,
        OpponentRatings opponent,
        Random          rng)
    {
        var takers = GetOurTakers(squad);
        var result = new PenaltyShootoutResult();

        bool weKickFirst   = rng.Next(2) == 0;
        int  ourScore      = 0;
        int  theirScore    = 0;
        int  ourKicks      = 0;
        int  theirKicks    = 0;
        string oppTrimmed  = opponentName.Trim();

        // ── Best of five ──────────────────────────────────────────────────────
        bool decided = false;
        for (int kickNumber = 0; kickNumber < KicksPerSide * 2 && !decided; kickNumber++)
        {
            bool ourKick = (kickNumber % 2 == 0) == weKickFirst;

            TakeKick(ourKick, suddenDeath: false);

            // Early end: the side behind can no longer catch up.
            int ourRemaining   = KicksPerSide - ourKicks;
            int theirRemaining = KicksPerSide - theirKicks;
            decided = ourScore > theirScore + theirRemaining
                   || theirScore > ourScore + ourRemaining;
        }

        // ── Sudden death ──────────────────────────────────────────────────────
        if (!decided && ourScore == theirScore)
        {
            result.WentToSuddenDeath = true;
            for (int round = 0; round < MaxSuddenDeathRounds; round++)
            {
                bool firstIsOurs = weKickFirst;
                TakeKick(firstIsOurs,  suddenDeath: true);
                TakeKick(!firstIsOurs, suddenDeath: true);

                if (ourScore != theirScore) break;
            }
        }

        result.OurScore   = ourScore;
        result.TheirScore = theirScore;
        result.WeWon      = ourScore > theirScore;
        return result;

        void TakeKick(bool ourKick, bool suddenDeath)
        {
            string takerName;
            double skill;

            if (ourKick)
            {
                var taker = takers[ourKicks % takers.Count];
                takerName = taker.Name.Trim();
                skill     = taker.Skill;
                ourKicks++;
            }
            else
            {
                theirKicks++;
                takerName = $"{oppTrimmed} — kick {theirKicks}";
                skill     = opponent.AttackRating;
            }

            // 70% + skill rating, in percentage points (e.g. skill 5.0 → 75%).
            bool scored = rng.NextDouble() * 100 < BaseScoreChancePercent + skill;
            var outcome = scored ? PenaltyOutcome.Scored : RollMissKind(rng);

            if (scored)
            {
                if (ourKick) ourScore++; else theirScore++;
            }

            result.Kicks.Add(new PenaltyKick
            {
                TakerName         = takerName,
                IsOurKick         = ourKick,
                Outcome           = outcome,
                OurRunningScore   = ourScore,
                TheirRunningScore = theirScore,
                IsSuddenDeath     = suddenDeath
            });
        }
    }

    /// <summary>
    /// Our kicking order: the players still on the pitch (slots 1–11), attackers
    /// first, goalkeeper last — squad slots 11 → 2, then 1. Sent-off and injured
    /// players have already been swapped out of these slots by the match engine.
    /// </summary>
    public static List<Player> GetOurTakers(Player?[] squad)
    {
        var takers = new List<Player>();

        for (int slot = 11; slot >= 2; slot--)
        {
            var player = squad[slot];
            if (player != null && player.Position != PlayerPosition.None)
                takers.Add(player);
        }

        var goalkeeper = squad[1];
        if (goalkeeper != null && goalkeeper.Position != PlayerPosition.None)
            takers.Add(goalkeeper);

        // A squad can never field zero players in practice; guard for tests.
        if (takers.Count == 0)
            takers.Add(new Player { Name = "Unknown", Skill = 5.0, Position = PlayerPosition.Attacker });

        return takers;
    }

    private static PenaltyOutcome RollMissKind(Random rng) => rng.Next(3) switch
    {
        0 => PenaltyOutcome.Saved,
        1 => PenaltyOutcome.Wide,
        _ => PenaltyOutcome.OverBar
    };
}
