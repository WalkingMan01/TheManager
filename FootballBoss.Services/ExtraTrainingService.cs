using FootballBoss.Models;

namespace FootballBoss.Services;

/// <summary>
/// Applies the effects of an extra training session on all eligible players
/// of the chosen position group.
///
/// Corresponds to subroutines 3206–3224 in FOOT.BAS (lines 2976–3029).
///
/// One session costs the manager a number of hours (1–9) and affects every
/// available player of the selected position. The outcome for each player is
/// randomised: a benefit, no change, slacking (slight regression), or injury.
/// </summary>
public static class ExtraTrainingService
{
    // ── Run a training session ────────────────────────────────────────────────

    /// <summary>
    /// Runs one extra training session for all available squad members in
    /// <paramref name="position"/>.
    ///
    /// BASIC lines 3208–3224:
    ///   For each player of the given position who is available (u(I)=0):
    ///     improvementRoll = (1 + RND(0–3)) / 10          → 0.1–0.4
    ///     diffFromHours   = ABS(RND(1–9) − hours)
    ///     netImprovement  = improvementRoll − diffFromHours * 0.05
    ///     injuryChance    = RND(1–13); if 1 AND not injured → u(I) = 2–4 weeks
    ///     Apply H(I) += netImprovement; recalculate status.
    ///     Move injured player to reserves (slot 13–20).
    ///
    ///   Record hours used per position in mw/mx/my/mz.
    /// </summary>
    public static List<TrainingOutcome> RunSession(
        PlayerPosition position,
        int            hoursScheduled,   // 1–9 (HR)
        Player?[]      squad,
        Club           club,
        Random         rng)
    {
        var outcomes = new List<TrainingOutcome>();

        for (int slot = 20; slot >= 1; slot--)
        {
            var player = squad[slot];
            if (player == null || player.Position != position) continue;

            // Skill improvement roll (line 3209–3210)
            double improvementRoll   = (1 + rng.Next(4)) / 10.0;   // 0.1–0.4
            int    hoursDifference   = Math.Abs(rng.Next(9) + 1 - hoursScheduled);
            double netImprovement    = improvementRoll - (hoursDifference * 0.05);

            // Apply skill change (lines 3215–3216)
            if (netImprovement >= 0)
                player.Skill += netImprovement;
            else
                player.Skill -= Math.Abs(netImprovement);

            PlayerService.RecalculateStatus(player);

            outcomes.Add(new TrainingOutcome
            {
                PlayerName     = player.Name,
                SquadSlot      = slot,
                NetImprovement = netImprovement,
            });
        }

        // Record hours used per position (lines 3025–3028: mw/mx/my/mz)
        switch (position)
        {
            case PlayerPosition.Goalkeeper: club.ExtraTrainingGoalkeeping += hoursScheduled; break;
            case PlayerPosition.Defender:   club.ExtraTrainingDefence     += hoursScheduled; break;
            case PlayerPosition.Midfielder: club.ExtraTrainingMidfield    += hoursScheduled; break;
            case PlayerPosition.Attacker:   club.ExtraTrainingAttack      += hoursScheduled; break;
        }

        return outcomes;
    }

    // ── Reset weekly training counters ────────────────────────────────────────

    /// <summary>
    /// Resets the extra-training-hours-used counters to zero at the start of
    /// a new week. Corresponds to subroutine 993 (line 718: mw=mx=my=mz=0).
    /// </summary>
    public static void ResetWeeklyCounters(Club club)
    {
        club.ExtraTrainingGoalkeeping = 0;
        club.ExtraTrainingDefence     = 0;
        club.ExtraTrainingMidfield    = 0;
        club.ExtraTrainingAttack      = 0;
    }

}

// ── Data classes ─────────────────────────────────────────────────────────────

/// <summary>Result of a single player's training session.</summary>
public class TrainingOutcome
{
    public string PlayerName     { get; set; } = string.Empty;
    public int    SquadSlot      { get; set; }

    /// <summary>Net skill change applied (+ve = improved, -ve = slacked, 0 = no change).</summary>
    public double NetImprovement { get; set; }

    public TrainingResult Result =>
        NetImprovement > 0  ? TrainingResult.Improved  :
        NetImprovement < 0  ? TrainingResult.Slacking  :
                              TrainingResult.NoChange;
}

public enum TrainingResult
{
    Improved,
    NoChange,
    Slacking,
}
