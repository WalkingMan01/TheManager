using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Manages individual player skill, status recalculation, team ratings,
/// and end-of-week squad aging.
///
/// Corresponds to subroutines 332, 523–527, 751, and related logic in FOOT.BAS.
/// </summary>
public static class PlayerService
{
    // ── Status / skill normalisation (lines 523–527) ─────────────────────────

    /// <summary>
    /// Recalculates a player's <see cref="PlayerStatus"/> from their current
    /// <see cref="Player.Skill"/> value and clamps Skill to [1.1, 9.9].
    ///
    /// BASIC lines 523–527:
    ///   J(Y) = 32 + (H>INT(H)+.7)*11 + (H&lt;INT(H)+.3 AND INT(H)>0)*13
    ///   IF H(Y)>9.7 THEN J(Y)=105
    ///   H(Y) clamped to [1.1, 9.9], cleared to 0 if position = None.
    /// </summary>
    public static void RecalculateStatus(Player player)
    {
        if (player.Position == PlayerPosition.None)
        {
            player.Skill = 0;
            return;
        }
    }

    // ── Hidden potential (new mechanic — no BASIC equivalent) ────────────────

    /// <summary>Last age of the 26–30 peak window; decline begins the season after.</summary>
    private const int PeakWindowEndAge = 30;

    /// <summary>
    /// Weeks of recovery credited over the off-season break — injuries shorter
    /// than this are fully healed by the start of the new season.
    /// </summary>
    private const int OffSeasonWeeks = 12;

    /// <summary>
    /// Assigns the hidden <see cref="Player.PeakAge"/> (26–30) and
    /// <see cref="Player.PotentialSkill"/> for a newly created player.
    /// PotentialSkill is always strictly greater than the skill the player was
    /// created with: a guaranteed base headroom of 0.3–0.5, plus 0.2–0.5 per
    /// year remaining until peak. New mechanic — no BASIC equivalent.
    /// </summary>
    public static void AssignPotential(Player player, Random rng)
    {
        player.PeakAge = 26 + rng.Next(5);                       // 26–30 inclusive

        int yearsToPeak        = Math.Max(0, player.PeakAge - player.DisplayAge);
        double baseHeadroom    = 0.3 + rng.Next(3) / 10.0;       // 0.3–0.5, always
        double headroomPerYear = 0.2 + rng.Next(4) / 10.0;       // 0.2–0.5
        player.PotentialSkill  = player.Skill + baseHeadroom
                               + yearsToPeak * headroomPerYear;
        // PotentialSkill's own setter clamps to 9.9

        AssignDevelopmentRate(player, rng);
    }

    // ── Transfer listing (line 1928) ─────────────────────────────────────────

    /// <summary>
    /// Toggles a player's transfer-listed flag by flipping the sign of their age.
    /// Corresponds to BASIC line 1928: G(I) = -G(I).
    /// </summary>
    /// <returns>False if the player is retiring and cannot be listed; true if the
    /// toggle was applied.</returns>
    public static bool ToggleTransferListed(Player player)
    {
        if (player.IsRetiring)
            return false;

        if (player.IsTransferListed)
            MarketService.UnlistFromTransfer(player);
        else
            MarketService.ListForTransfer(player);

        return true;
    }

    // ── Team ratings (subroutine 332, lines 371–387) ─────────────────────────

    /// <summary>
    /// Calculates the four positional strength ratings used by the match engine
    /// and the formation code and aggregate temper from the first 11 squad slots.
    ///
    /// BASIC subroutine 332:
    ///   BA = INT(H(1)) if T(1)=1
    ///   bc = SUM DEF skills → INT(bc/3.9), capped 0–9
    ///   bb = SUM MID skills → INT(bb/2.9), capped 0–9
    ///   bd = SUM ATK skills → INT(bd/2.9), capped 0–9
    ///   mu = formation code (100*DEF count + 10*MID count + ATK count)
    ///   pu = SUM of E(3,I) for starting 11
    /// </summary>
    public static TeamRatings CalculateTeamRatings(Player?[] squad)
    {
        var ratings = new TeamRatings();

        var goalkeeper = squad[1];
        if (goalkeeper?.Position == PlayerPosition.Goalkeeper)
            ratings.GoalkeeperRating = (int)goalkeeper.Skill;

        ratings.TeamTemper = goalkeeper?.Temper ?? 0;

        double rawDefenceSkill   = 0;
        double rawMidfieldSkill  = 0;
        double rawAttackSkill    = 0;

        for (int squadSlot = 2; squadSlot <= 11; squadSlot++)
        {
            var player = squad[squadSlot];
            if (player == null || player.Position == PlayerPosition.None) continue;

            int integerSkill = (int)player.Skill;

            switch (player.Position)
            {
                case PlayerPosition.Defender:
                    rawDefenceSkill        += integerSkill;
                    ratings.FormationCode  += 100;
                    break;
                case PlayerPosition.Midfielder:
                    rawMidfieldSkill       += integerSkill;
                    ratings.FormationCode  += 10;
                    break;
                case PlayerPosition.Attacker:
                    rawAttackSkill         += integerSkill;
                    ratings.FormationCode  += 1;
                    break;
            }

            ratings.TeamTemper += player.Temper;
        }

        // Apply divisors and cap at 9 (lines 381–386)
        ratings.DefenceRating = Math.Min(9, (int)(rawDefenceSkill  / 3.9));
        ratings.MidRating     = Math.Min(9, (int)(rawMidfieldSkill / 2.9));
        ratings.AttackRating  = Math.Min(9, (int)(rawAttackSkill   / 2.9));

        return ratings;
    }

    // ── Appearance-based development (new mechanic — no BASIC equivalent) ────
    // See docs/specs/player-development.md. Replaces the old result-based
    // post-match skill deltas: growth now depends only on playing time and
    // squad involvement, not the scoreline.

    /// <summary>Rough first-team appearances a season, used only to calibrate
    /// DevelopmentRate against PeakAge — not a simulated fixture count.</summary>
    private const int AppearancesPerSeason = 30;

    /// <summary>Lower bound of the random development-pace multiplier (0.6–1.6).</summary>
    private const double DevelopmentPaceMin = 0.6;

    /// <summary>Fraction of DevelopmentRate credited to a squad member who was
    /// available for the match but didn't feature (the substitute, or a reserve
    /// never named).</summary>
    private const double NonFeaturingGrowthFactor = 0.5;

    /// <summary>Hard ceiling on Skill gained from appearance-based development in
    /// a single season, regardless of DevelopmentRate or appearance count.</summary>
    private const double SeasonSkillGainCap = 4.0;

    /// <summary>
    /// Rolls the hidden per-appearance Skill gain from the player's current
    /// headroom to PotentialSkill, spread over the seasons remaining to PeakAge,
    /// and scaled by a random pace multiplier (0.6–1.6) so players with identical
    /// headroom still develop at different speeds. New mechanic — no BASIC
    /// equivalent. Call after PotentialSkill and PeakAge are both set.
    /// </summary>
    public static void AssignDevelopmentRate(Player player, Random rng)
    {
        double pace           = DevelopmentPaceMin + rng.Next(11) / 10.0;   // 0.6–1.6
        int    seasonsToPeak  = Math.Max(1, player.PeakAge - player.DisplayAge);
        double headroom       = Math.Max(0, player.PotentialSkill - player.Skill);

        player.DevelopmentRate = headroom / (seasonsToPeak * AppearancesPerSeason) * pace;
    }

    /// <summary>
    /// Applies each starting player's DevelopmentRate for a match played this
    /// week. Everyone else still in the squad (slots 12–20 — the unused
    /// substitute plus the reserves) grows at half rate instead of nothing, on
    /// the basis that training and being around the first team still counts for
    /// something even without minutes played. Every gain is capped so a player
    /// can never pick up more than SeasonSkillGainCap of development-driven
    /// Skill in one season, however high their DevelopmentRate or however many
    /// matches/weeks count toward it. New mechanic — no BASIC equivalent.
    /// </summary>
    public static void ApplyAppearanceGrowth(Player?[] squad)
    {
        for (int squadSlot = 1; squadSlot <= 11; squadSlot++)
        {
            var player = squad[squadSlot];
            if (player == null || player.Skill <= 0) continue;

            GrowPlayer(player, player.DevelopmentRate);
        }

        // Slots 12–20: the substitute (12, occupied only if never brought on —
        // ResolveIncident clears it and moves the player into the vacated
        // starting slot the moment a substitution happens) and the reserves
        // (13–20). Anyone occupying one of these slots at the weekly tick
        // either never featured this week, or started and was subbed/sent
        // off/injured out of the XI mid-match — either way they get half
        // credit rather than none.
        for (int squadSlot = 12; squadSlot <= 20; squadSlot++)
        {
            var player = squad[squadSlot];
            if (player == null || player.Skill <= 0) continue;

            GrowPlayer(player, player.DevelopmentRate * NonFeaturingGrowthFactor);
        }
    }

    /// <summary>
    /// Applies a development gain to a player, clipped so their running
    /// SkillGainedThisSeason total never exceeds SeasonSkillGainCap.
    /// </summary>
    private static void GrowPlayer(Player player, double proposedGain)
    {
        double remainingAllowance = Math.Max(0, SeasonSkillGainCap - player.SkillGainedThisSeason);
        double actualGain         = Math.Min(proposedGain, remainingAllowance);
        if (actualGain <= 0) return;

        player.Skill                  += actualGain;
        player.SkillGainedThisSeason  += actualGain;
        RecalculateStatus(player);
    }

    // ── Weekly countdown / aging (subroutine 751, lines 551–558) ────────────

    /// <summary>
    /// Called every week to tick down unavailability, apply small random skill
    /// drift due to age/fitness, and clear retired players.
    ///
    /// BASIC subroutine 751:
    ///   If J(Y)=82 (retiring) → clear slot (subroutine 200)
    ///   randomDrift = INT(RND*25)/10  (range 0.0–2.4)
    ///   H(Y) -= 1.4 - randomDrift    (net skill change ≈ −1.4 to +1.0)
    ///   Clamp and recalculate status (subroutine 525)
    ///   Players with J=42 (OnLoan) or J=76 (LoanUnavailable) are skipped.
    ///
    /// Extends the original with aging and the hidden-potential mechanic
    /// (no BASIC equivalent): each player ages one year, and the drift is
    /// biased downward by 0.05 per year beyond age 30 — the end of the peak
    /// window. Under-30 players draw from a narrower, less loss-weighted
    /// range (–0.5 to +0.5) than 30-and-over players (–0.7 to +0.5). The
    /// ceiling itself never drops once assigned. Injuries also heal over the
    /// off-season break (<see cref="OffSeasonWeeks"/>), so only the
    /// longest-term injuries carry into the new season.
    /// </summary>
    public static void ApplyEndOfSeasonSkillUpdate(Player?[] squad, Random rng)
    {
        for (int squadSlot = 1; squadSlot <= 20; squadSlot++)
        {
            var player = squad[squadSlot];
            if (player == null) continue;

            if (player.IsRetiring)
            {
                squad[squadSlot] = null;
                continue;
            }

            player.YellowCardsThisSeason = 0;
            player.SkillGainedThisSeason = 0;
            player.WeeksInjured = Math.Max(0, player.WeeksInjured - OffSeasonWeeks);

            if (player.Position == PlayerPosition.None) continue;

            // Age one year — increment away from zero because a negative Age
            // means transfer-listed (G(I)<0).
            player.Age += Math.Sign(player.Age);

            // Ages 26–30 are all peak years; decline only sets in past 30.
            // The ceiling (PotentialSkill) never drops once assigned. Under-30
            // players get a narrower, less loss-weighted range than 30+.
            double randomDrift = player.DisplayAge < PeakWindowEndAge
                ? rng.Next(21) / 20.0 - 0.5               // range –0.5 to +0.5, under 30
                : ((rng.Next(25) / 10.0) - 1.4) / 2;      // range –0.7 to +0.5, 30 and over
            int yearsPastPeak   = Math.Max(0, player.DisplayAge - PeakWindowEndAge);
            player.Skill       += randomDrift - yearsPastPeak * 0.05;

            RecalculateStatus(player);
        }
    }

    /// <summary>
    /// Increments the games-played counter for first-team players.
    /// Corresponds to subroutine 3301 (line 3036: x(Y)+=ABS(Y&lt;12)).
    /// </summary>
    public static void UpdateSquadAppearances(Player?[] squad)
    {
        for (int squadSlot = 1; squadSlot <= 11; squadSlot++)
        {
            var player = squad[squadSlot];
            if (player == null) continue;
            player.GamesPlayed++;
        }
    }
}
