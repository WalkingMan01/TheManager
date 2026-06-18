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
        // ToDo: Remove this method ?
        if (player.Position == PlayerPosition.None)
        {
            player.Skill = 0;
            return;
        }
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

    // ── Post-match skill updates (subroutine 3306, lines 3042–3046) ──────────

    /// <summary>
    /// Adjusts each starting player's skill after a match result.
    /// Win = slight boost, loss = slight dip, clean sheet adds extra for defenders/GK.
    ///
    /// BASIC lines 3306–3308:
    ///   Outfield T>2: H += win/20 - loss/25
    ///   Others: H += win/20 - loss*0.06 + cleanSheet*0.03
    /// </summary>
    public static void ApplyPostMatchSkillChanges(
        Player?[] squad, bool won, bool lost, bool cleanSheet)
    {
        for (int squadSlot = 1; squadSlot <= 11; squadSlot++)
        {
            var player = squad[squadSlot];
            if (player == null || player.Skill <= 0) continue;

            if (player.Position is PlayerPosition.Midfielder or PlayerPosition.Attacker)
            {
                // line 3306
                if (won)  player.Skill += 1.0 / 20;
                if (lost) player.Skill -= 1.0 / 25;
            }
            else
            {
                // line 3307 — GK and defenders
                if (won)        player.Skill += 1.0 / 20;
                if (lost)       player.Skill -= 0.06;
                if (cleanSheet) player.Skill += 0.03;
            }

            RecalculateStatus(player);
        }
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

            if (player.Position == PlayerPosition.None) continue;

            double randomDrift = ((rng.Next(25) / 10.0) - 1.4) / 2;   // range –0.7 to +0.5
            player.Skill += randomDrift;

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
