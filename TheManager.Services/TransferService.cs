using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Calculates transfer fees, evaluates negotiation offers, resolves deals,
/// and estimates match attendance.
///
/// Corresponds to subroutines 4022–4062, 2601–2654, and 3801 in FOOT.BAS.
/// </summary>
public static class TransferService
{
    // ── Asking price (lines 4043–4045) ────────────────────────────────────────

    /// <summary>
    /// Calculates the selling club's asking price for a player.
    ///
    /// BASIC lines 4043–4045:
    ///   If INT(skill) = 1: askingPrice = 6500
    ///   Otherwise: askingPrice = 5000 * (INT(H)-1)^2 * 2 + (H>9.6 ? 310000 : 0)
    ///   Add random spread = INT(askingPrice/3)+6000
    ///   Age deduction: penalty = (Age-28), clamped ≥ 0;
    ///                  askingPrice -= INT(askingPrice/10) * penalty
    /// </summary>
    public static double CalculateAskingPrice(Player player, Random rng)
    {
        int integerSkill = (int)player.Skill;

        double askingPrice;
        if (integerSkill <= 1)
        {
            askingPrice = 6_500;
        }
        else
        {
            askingPrice = 5_000.0 * (integerSkill - 1) * (integerSkill - 1) * 2
                        + (player.Skill > 9.6 ? 310_000 : 0);
        }

        double priceSpread = (int)(askingPrice / 3) + 6_000;
        askingPrice        = askingPrice + (int)(rng.NextDouble() * priceSpread);

        // Age deduction — players over 28 are worth less (line 4378)
        int agePenalty = Math.Max(0, player.DisplayAge - 28);
        askingPrice    = (int)(askingPrice - (int)(askingPrice / 10.0) * agePenalty);

        return askingPrice;
    }

    // ── Deal sweetener deductions (lines 4046–4050) ──────────────────────────

    /// <summary>
    /// Reduces the asking price based on sweeteners offered in the deal
    /// (a loan player and/or a free transfer player).
    ///
    /// BASIC lines 4046–4050:
    ///   Loan sweetener:    effectiveSkill = INT(skill) − agePenalty (clamped ≥ 1)
    ///                      sweetenerReduction += effectiveSkill * loanWeeks * 100
    ///   Free xfer:         effectiveSkill = INT(skill) − agePenalty (clamped ≥ 1)
    ///                      sweetenerReduction += effectiveSkill^2 * 5000
    /// </summary>
    public static double ApplySweetenerDeductions(
        double askingPrice,
        Player? loanPlayer,       int loanWeeks,
        Player? freeTransferPlayer,
        int     buyingDivision)
    {
        double sweetenerReduction   = 0;
        double divisionSkillFloor   = 4.0 / buyingDivision;

        if (loanPlayer != null && loanWeeks > 0 && (int)loanPlayer.Skill > divisionSkillFloor)
        {
            int agePenalty       = Math.Max(0, loanPlayer.DisplayAge - 26);
            int effectiveSkill   = Math.Max(1, (int)loanPlayer.Skill - agePenalty);
            sweetenerReduction  += effectiveSkill * loanWeeks * 100;
        }

        if (freeTransferPlayer != null && (int)freeTransferPlayer.Skill > divisionSkillFloor)
        {
            int agePenalty      = Math.Max(0, freeTransferPlayer.DisplayAge - 26);
            int effectiveSkill  = Math.Max(1, (int)freeTransferPlayer.Skill - agePenalty);
            sweetenerReduction += (double)effectiveSkill * effectiveSkill * 5_000;
        }

        return askingPrice - sweetenerReduction;
    }

    // ── Late refusal (line 2638) ──────────────────────────────────────────────

    /// <summary>
    /// A 1-in-20 chance the player walks away even after agreeing terms.
    /// BASIC line 2638: a random late refusal applied even when all contract
    /// conditions are met.
    /// </summary>
    public static bool PlayerWalksAwayDespiteAgreedTerms(Random rng) => rng.Next(20) == 0;

    // ── Apply completed deal (lines 2616–2619) ────────────────────────────────

    /// <summary>
    /// Commits a successful transfer: deducts fees from bank balance, updates
    /// the record signing fee if applicable, and applies the agreed wage and
    /// contract length to the signed player.
    ///
    /// Caller is responsible for physically moving the player into the correct
    /// squad slot by swapping the relevant <see cref="TheManager.Models.GameState.Squad"/> entries.
    /// </summary>
    public static void CommitDeal(
        Player   player,
        Finances finances,
        double   transferFee,
        double   signingFee,
        double   wage,
        int      contractWeeks)
    {
        player.GamesPlayed   = 0;   // x(IB)=0 when NT=0 (line 2136)
        player.WeeklyWage    = wage;
        player.ContractWeeks = contractWeeks;

        double totalCost = transferFee + signingFee;
        finances.BankBalance -= (int)totalCost;

        if (totalCost > finances.RecordSigningFee)
            finances.RecordSigningFee = totalCost;
    }

    // ── Attendance / gate money calculation (subroutine 3801) ────────────────

    /// <summary>
    /// Estimates home gate attendance for a league match.
    ///
    /// BASIC subroutine 3801 (lines 3197–3229):
    ///   Base: (50,000 + RND*10,000) ÷ division ÷ division
    ///   Reduced by league position of both teams (1250/div/div per place)
    ///   Division 1 gets +1/3 bonus
    ///   Ticket price adjustment: too high reduces attendance; too low increases it
    ///   Hard cap for divisions 3+: ≤ 18,721
    /// </summary>
    public static double EstimateAttendance(
        int    division,
        int    ourLeaguePosition,
        int    opponentLeaguePosition,
        double ticketPriceInPounds,
        Random rng)
    {
        double baseAttendance       = 50_000 + rng.Next(10_000);
        baseAttendance              = (int)(baseAttendance / division) / division;

        double deductionPerPosition = (1_250.0 / division) / division;
        baseAttendance             -= deductionPerPosition * ourLeaguePosition;
        baseAttendance             -= deductionPerPosition * opponentLeaguePosition;

        if (division == 1) baseAttendance += baseAttendance / 3.0;

        // Ticket price effect (lines 3808–3809)
        double idealTicketPrice = 5 - division;
        if (ticketPriceInPounds > idealTicketPrice)
        {
            int priceExcess = (int)(ticketPriceInPounds - idealTicketPrice);
            if (priceExcess > 0) baseAttendance = baseAttendance / priceExcess;
        }
        else if (ticketPriceInPounds < idealTicketPrice)
        {
            int priceDiscount = (int)(idealTicketPrice - ticketPriceInPounds);
            baseAttendance   += priceDiscount * (1_000.0 / division);
        }

        // Maximum attendance cap (line 3811)
        double attendanceCap = (100_000.0 / division) / division;
        if (baseAttendance > attendanceCap) baseAttendance = attendanceCap;

        baseAttendance = Math.Max(0, baseAttendance) + 1 + rng.Next(50);

        // Lower-division hard cap (line 3813)
        if (division >= 3) baseAttendance = Math.Min(baseAttendance, 18_721);

        return (int)baseAttendance;
    }

    /// <summary>
    /// Converts attendance to gate money: attendance × ticket price.
    /// </summary>
    public static double CalculateGateMoney(double attendance, double ticketPriceInPounds)
        => (int)(attendance * ticketPriceInPounds);
}
