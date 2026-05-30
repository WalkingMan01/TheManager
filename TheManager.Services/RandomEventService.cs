using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Fires the unpredictable weekly events that run after the financial report.
///
/// Corresponds to subroutines 2529–2560 in FOOT.BAS (lines 1924–2086).
///
/// Each week a random roll (RA = 1 + RND*35) decides which chain of events
/// fires. Several events are conditional on squad state (star players with
/// J=105, injured players, unhappy available players).
/// </summary>
public static class RandomEventService
{
    /// <summary>
    /// Evaluates all weekly random events in the order they appear in FOOT.BAS
    /// and returns a list of events that fired this week.
    ///
    /// Call after <see cref="FinanceService.CalculateWeeklyReport"/> has run.
    /// </summary>
    public static List<RandomEvent> EvaluateWeeklyEvents(
        Player?[] squad,
        string    clubName,
        string[]  allTeamNames,
        Random    rng)
    {
        var firedEvents = new List<RandomEvent>();

        // ── International call-up / foreign transfer offer ─────────────────────
        int starEventRoll  = 1 + rng.Next(35);
        int starPlayerSlot = FindStarPlayerSlot(squad);

        if (starPlayerSlot > 0)
        {
            if (starEventRoll < 6)
                firedEvents.Add(BuildInternationalCallUpEvent(squad, starPlayerSlot));
            else if (starEventRoll == 9)
                firedEvents.Add(BuildForeignTransferOfferEvent(squad, starPlayerSlot, rng));
        }

        // ── Player retirement announcement (line 2564–2565) ──────────────────
        var retirement = CheckRetirementAnnouncement(squad, rng);
        if (retirement != null) firedEvents.Add(retirement);

        // ── Unhappy player requests transfer (lines 2549–2555) ───────────────
        if (1 + rng.Next(35) == 8)
        {
            var unhappy = CheckUnhappyPlayerTransferRequest(squad, clubName, allTeamNames, rng);
            if (unhappy != null) firedEvents.Add(unhappy);
        }

        return firedEvents;
    }

    // ── Apply player's decision on an event ──────────────────────────────────

    /// <summary>
    /// Resolves the manager's response to an international call-up event.
    /// Release: player slot is cleared and a substitute is promoted.
    /// Refuse: player receives a skill penalty.
    ///
    /// BASIC lines 2540–2541.
    /// </summary>
    public static void ResolveInternationalCallUp(
        Player?[] squad,
        int       playerSlot,
        bool      managerReleasesPlayer,
        Random    rng)
    {
        var player = squad[playerSlot];
        if (player == null) return;

        if (managerReleasesPlayer)
        {
            PromoteReserveToFirstTeam(squad, playerSlot);
        }
        else
        {
            // Skill penalty for refusing (line 2541: H(Y) -= RA/10)
            int penaltyRoll = 1 + rng.Next(15);
            player.Skill   -= penaltyRoll / 10.0;
            PlayerService.RecalculateStatus(player);
        }
    }

    /// <summary>
    /// Resolves the manager's response to a foreign transfer offer.
    /// Accept: player is sold for the offered fee and slot cleared.
    /// Decline: no action (player stays).
    ///
    /// BASIC lines 2558–2560.
    /// </summary>
    /// <returns>
    /// The new record sale player name if the sale beat the existing record; otherwise null.
    /// </returns>
    public static string? ResolveForeignTransferOffer(
        Player?[] squad,
        Finances  finances,
        int       playerSlot,
        double    offeredFee,
        bool      managerAccepts)
    {
        if (!managerAccepts) return null;

        var player = squad[playerSlot];
        if (player == null) return null;

        string? newRecordSaleName = null;

        // Update record sale if this beats it (line 2560)
        if (offeredFee >= finances.RecordSaleFee)
        {
            finances.RecordSaleFee = offeredFee;
            newRecordSaleName      = player.Name.Trim();
        }

        finances.BankBalance  += (int)offeredFee;
        finances.WeeklyProfit += offeredFee;

        squad[playerSlot] = null;

        return newRecordSaleName;
    }

    /// <summary>
    /// Resolves the manager's response to a player's transfer request.
    /// Accept: player sold for the requested tax-free fee.
    /// Refuse: player's skill drops slightly due to unhappiness.
    ///
    /// BASIC lines 2553–2555.
    /// </summary>
    public static void ResolveTransferRequest(
        Player?[] squad,
        Finances  finances,
        int       playerSlot,
        double    requestedFee,
        bool      managerAccepts,
        Random    rng)
    {
        var player = squad[playerSlot];
        if (player == null) return;

        if (managerAccepts)
        {
            finances.BankBalance  += (int)requestedFee;
            finances.WeeklyProfit += requestedFee;
            squad[playerSlot]      = null;
        }
        else
        {
            // Line 2555: H(I) -= H(I)/10 (unhappiness penalty)
            player.Skill -= player.Skill / 10.0;
            PlayerService.RecalculateStatus(player);
        }
    }

    // ── Private event builders ────────────────────────────────────────────────

    private static int FindStarPlayerSlot(Player?[] squad)
    {
        for (int slot = 1; slot <= 20; slot++)
        {
            if (squad[slot]?.IsStar == true) return slot;
        }
        return 0;
    }

    private static RandomEvent BuildInternationalCallUpEvent(Player?[] squad, int playerSlot)
    {
        var player = squad[playerSlot]!;
        return new RandomEvent
        {
            Type       = RandomEventType.InternationalCallUp,
            PlayerName = player.Name.Trim(),
            PlayerSlot = playerSlot
        };
    }

    private static RandomEvent BuildForeignTransferOfferEvent(Player?[] squad, int playerSlot, Random rng)
    {
        var    player   = squad[playerSlot]!;
        double offerFee = 1_500_000 + rng.Next(1_000_000);   // 1.5M–2.5M (line 2046)

        return new RandomEvent
        {
            Type           = RandomEventType.ForeignTransferOffer,
            PlayerName     = player.Name.Trim(),
            PlayerSlot     = playerSlot,
            FinancialValue = offerFee,
            Description    = $"Foreign club offer £{(int)offerFee:N0} tax free"
        };
    }

    private static RandomEvent? CheckRetirementAnnouncement(Player?[] squad, Random rng)
    {
        for (int slot = 1; slot <= 20; slot++)
        {
            var player = squad[slot];
            if (player == null || player.DisplayAge <= 29) continue;

            // line 2564: random check — not every over-29 player announces every week
            if (rng.Next(10) != 0) continue;

            return new RandomEvent
            {
                Type        = RandomEventType.RetirementAnnouncement,
                PlayerName  = player.Name.Trim(),
                PlayerSlot  = slot,
                Description = $"{player.Name.Trim()} announces retirement at end of season"
            };
        }
        return null;
    }

    private static RandomEvent? CheckUnhappyPlayerTransferRequest(
        Player?[] squad, string clubName, string[] allTeamNames, Random rng)
    {
        // Scan from highest squad slot downward (line 2549: FOR I=20 TO 1 STEP -1)
        for (int slot = 20; slot >= 1; slot--)
        {
            var player = squad[slot];
            if (player == null || player.Position == PlayerPosition.None) continue;

            // Pick a random destination club (not our own) — line 2551
            int    destinationIndex = 1 + rng.Next(20);
            string destinationName  = allTeamNames[destinationIndex];
            if (destinationName.Trim() == clubName.Trim()) continue;

            // Requested fee = INT(H(I)) * H(I) * 6000  (line 2552)
            double requestedFee = (int)player.Skill * player.Skill * 6_000;

            return new RandomEvent
            {
                Type           = RandomEventType.PlayerTransferRequest,
                PlayerName     = player.Name.Trim(),
                PlayerSlot     = slot,
                FinancialValue = requestedFee,
                Description    = $"{player.Name.Trim()} requests transfer to {destinationName.Trim()}"
            };
        }
        return null;
    }

    private static void PromoteReserveToFirstTeam(Player?[] squad, int vacatedSlot)
    {
        squad[vacatedSlot] = null;

        // Find a reserve to fill the gap (slots 13–20)
        for (int reserveSlot = 13; reserveSlot <= 20; reserveSlot++)
        {
            var reserve = squad[reserveSlot];
            if (reserve != null && reserve.Position != PlayerPosition.None)
            {
                (squad[vacatedSlot], squad[reserveSlot]) = (squad[reserveSlot], squad[vacatedSlot]);
                return;
            }
        }
    }
}

// ── Data classes ─────────────────────────────────────────────────────────────

public enum RandomEventType
{
    RetirementAnnouncement,
    InternationalCallUp,
    ForeignTransferOffer,
    PlayerTransferRequest
}

/// <summary>A random event that fired during the weekly news phase.</summary>
public class RandomEvent
{
    public RandomEventType Type           { get; set; }
    public string          PlayerName     { get; set; } = string.Empty;
    public int             PlayerSlot     { get; set; }
    public double          FinancialValue { get; set; }   // fee, wage, etc.
    public string          Description   { get; set; } = string.Empty;
}
