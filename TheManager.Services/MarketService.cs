using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Manages the transfer market: listing players for sale, generating weekly
/// interest from rival clubs, and executing outgoing transfers.
///
/// Corresponds to subroutine 312 lines 355–369 (incoming interest generation)
/// and subroutines 5001–5088 (offers &amp; negotiations) in FOOT.BAS.
/// </summary>
public static class MarketService
{
    // ── Transfer listing ──────────────────────────────────────────────────────

    /// <summary>
    /// Puts a player on the transfer list by negating their age, mirroring
    /// the BASIC convention where G(I) &lt; 0 flags a listed player.
    /// </summary>
    public static void ListForTransfer(Player player)
    {
        if (player.Age > 0) player.Age = -player.Age;
    }

    /// <summary>Removes a player from the transfer list.</summary>
    public static void UnlistFromTransfer(Player player)
    {
        if (player.Age < 0) player.Age = -player.Age;
    }

    public static bool IsListed(Player player) => player.Age < 0;

    // ── Weekly incoming interest (BASIC lines 355–369) ────────────────────────

    /// <summary>
    /// Generates rival-club interest in your transfer-listed players.
    /// Called once per week. Clears the previous week's entries and
    /// re-rolls for up to three simultaneous interested clubs.
    ///
    /// BASIC lines 355–369 (subroutine 312, second half):
    ///   FOR I = 24 TO 26
    ///     RA = 1 + RND(0–2);  IF RA &lt; 3 → no interest this slot
    ///     Pick random listed player; pick rival club from nearby division.
    /// </summary>
    public static List<TransferListing> GenerateIncomingInterest(
        Player?[] squad,
        Division  ourDivision,
        string    ourClubName,
        string[]  allTeamNames,
        Random    rng)
    {
        var interest = new List<TransferListing>(3);
        var listed   = FindListedPlayers(squad);
        if (listed.Count == 0) return interest;

        for (int slot = 24; slot <= 26; slot++)
        {
            // 1/3 chance of rival interest per slot
            if (1 + rng.Next(3) < 3) continue;

            var (squadSlot, player) = listed[rng.Next(listed.Count)];

            // Avoid duplicate entries for the same player
            if (interest.Any(l => l.SourceSquadSlot == squadSlot)) continue;

            // Pick a rival club from a nearby division
            int targetDiv = Math.Clamp((int)ourDivision + rng.Next(3) - 1, 1, 4);
            int teamIndex = (targetDiv - 1) * 20 + 1 + rng.Next(20);

            if (teamIndex < 1 || teamIndex >= allTeamNames.Length) continue;
            string clubName = allTeamNames[teamIndex];
            if (string.IsNullOrWhiteSpace(clubName)) continue;
            if (clubName.Trim() == ourClubName.Trim()) continue;

            int rivalDiv = Math.Clamp((teamIndex - 1) / 20 + 1, 1, 4);
            interest.Add(new TransferListing
            {
                SquadSlot       = slot,
                SourceSquadSlot = squadSlot,
                Player          = player,
                OwningClubIndex = teamIndex,
                OwningClubName  = clubName,
                OfferedBid      = GenerateRivalBid(player, rivalDiv, rng)
            });
        }

        return interest;
    }

    // ── Execute a sale ────────────────────────────────────────────────────────

    /// <summary>
    /// Sells a squad player to a rival club:
    /// adds the fee to the bank balance, updates the record sale if beaten,
    /// removes the player from the squad, and clears any market interest.
    /// </summary>
    /// <returns>The new record sale player name if the sale set a record; otherwise null.</returns>
    public static string? SellPlayer(
        Player?[]             squad,
        Finances              finances,
        List<TransferListing> playersBeingSought,
        int                   squadSlot,
        double                fee)
    {
        var player = squad[squadSlot];
        if (player is null) return null;

        finances.BankBalance += fee;

        string? newRecordSaleName = null;
        if (fee > finances.RecordSaleFee)
        {
            finances.RecordSaleFee = fee;
            newRecordSaleName      = player.Name;
        }

        squad[squadSlot] = null;
        playersBeingSought.RemoveAll(l => l.SourceSquadSlot == squadSlot);

        return newRecordSaleName;
    }

    // ── Generate a rival club's bid ───────────────────────────────────────────

    /// <summary>
    /// Generates the rival club's opening bid as 65–100% of the calculated
    /// asking price. The percentage varies by the interested club's division
    /// (higher-division clubs bid more aggressively).
    ///
    /// BASIC lines 4043–4062 adapted for the selling direction.
    /// </summary>
    public static double GenerateRivalBid(
        Player player,
        int    rivalDivision,
        Random rng)
    {
        double asking = TransferService.CalculateAskingPrice(player, rng);

        // Higher-division clubs bid closer to asking price
        double minFraction = rivalDivision switch
        {
            1 => 0.85,
            2 => 0.75,
            3 => 0.68,
            _ => 0.60
        };

        double fraction = minFraction + rng.NextDouble() * (1.0 - minFraction);
        return Math.Round(asking * fraction / 1000.0) * 1000.0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<(int slot, Player player)> FindListedPlayers(Player?[] squad)
    {
        var result = new List<(int, Player)>();
        for (int i = 1; i <= 20; i++)
        {
            var p = squad[i];
            if (p is not null && p.Age < 0)
                result.Add((i, p));
        }
        return result;
    }

    /// <summary>Returns all squad slots (1–20) whose player is on the transfer list.</summary>
    public static IEnumerable<(int slot, Player player)> GetListedPlayers(Player?[] squad)
        => FindListedPlayers(squad);
}
