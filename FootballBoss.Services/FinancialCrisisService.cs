using FootballBoss.Models;

namespace FootballBoss.Services;

/// <summary>
/// Evaluates club performance and financial health each week, triggering
/// director confidence votes and emergency rescue measures when needed.
///
/// Corresponds to subroutines 5417–5446 in FOOT.BAS (lines 4246–4363).
///
/// Execution order mirrors the BASIC:
///   1. Shares-sold danger  → immediate sack (5417)
///   2. Recent form check   → confidence vote if ≤ 6 form-points from last 14 matches (5418–5422)
///   3. Financial rescue    → borrow, mortgage, sell shares, force-sell players (5423–5435)
///   4. Insolvency sack     → if still negative after rescue attempts (5437)
/// </summary>
public static class FinancialCrisisService
{
    // ── Main entry point ──────────────────────────────────────────────────────

    /// <summary>
    /// Runs the full weekly crisis check and returns a <see cref="CrisisResult"/>
    /// describing what (if anything) happened.
    ///
    /// Call this once per week after <see cref="FinanceService.CalculateWeeklyReport"/>.
    /// If the result indicates the manager was sacked, the caller should trigger
    /// <see cref="InitializationService.JoinNewClub"/> for the next available post.
    /// </summary>
    public static CrisisResult Evaluate(GameState gameState, Random rng)
    {
        var finances = gameState.Finances;
        var club     = gameState.Club;

        // ── 1. Shares-sold danger (line 5417: JU=1) ──────────────────────────
        // When AU falls below 1 the new board sacks the manager immediately.
        if (finances.SharesSoldDanger)
        {
            club.ManagerContractWeeks = 0;
            return new CrisisResult(CrisisOutcome.SackedSharesSold,
                "Too many shares sold — the new board of directors have sacked you.");
        }

        // ── 2. Form check (lines 5418–5422) ───────────────────────────────────
        // Requires at least 15 fixtures played before any confidence check.
        if (gameState.FixturesPlayed >= 15)
        {
            int formPoints = CalculateFormPoints(gameState.CurrentLeague.WeeklyResults,
                                                 gameState.FixturesPlayed);

            // OP ≤ 6 means fewer than ~3 wins in 14 games → confidence vote
            if (formPoints <= 6)
            {
                int voteRoll = 1 + rng.Next(2);
                if (voteRoll == 1)
                {
                    club.ManagerContractWeeks = 0;
                    return new CrisisResult(CrisisOutcome.OnNotice,
                        "You are given a week's notice.");
                }
                return new CrisisResult(CrisisOutcome.AnotherChance,
                    "You are given another chance.");
            }
        }

        // ── 3. Solvency check (line 5423: IF AI>-1 THEN 5439) ─────────────────
        if (finances.BankBalance >= 0)
            return new CrisisResult(CrisisOutcome.NoAction);

        // ── 4. Emergency rescue sequence ──────────────────────────────────────
        return RunRescueSequence(gameState, rng);
    }

    // ── Form points (lines 5419–5420) ────────────────────────────────────────

    /// <summary>
    /// Counts form-points from the last 14 results.
    ///
    /// BASIC lines 5419–5420:
    ///   W$(I) format = opponent-score digit then our-score digit.
    ///   Win  (opponent &lt; us): +3    (ABS(RA&lt;RB)*3)
    ///   Draw (equal):           +1    (-(RA=RB) where BASIC true=-1, so -(-1)=+1)
    ///   Loss (opponent &gt; us): +0
    ///
    ///   IF formPoints &gt; 6 → directors are satisfied (no vote needed).
    /// </summary>
    public static int CalculateFormPoints(string[] weeklyResults, int fixturesPlayed)
    {
        int formPoints = 0;
        int startIndex = fixturesPlayed - 1;
        int endIndex   = Math.Max(0, startIndex - 14);

        for (int i = startIndex; i >= endIndex; i--)
        {
            string result = weeklyResults[i];
            if (string.IsNullOrEmpty(result) || result.Length < 2) continue;

            int opponentScore = result[0] - '0';
            int ourScore      = result[1] - '0';

            if (opponentScore < ourScore)      formPoints += 3;   // win
            else if (opponentScore == ourScore) formPoints += 1;   // draw
            // loss: nothing added
        }

        return formPoints;
    }

    // ── Emergency rescue (lines 5423–5446) ───────────────────────────────────

    private static CrisisResult RunRescueSequence(GameState gameState, Random rng)
    {
        var finances = gameState.Finances;
        var club     = gameState.Club;
        var actions  = new List<string>();

        // Step A — use remaining overdraft as an emergency loan (lines 5424–5425)
        if (finances.OverdraftAvailable > 0)
        {
            double borrowAmount = finances.OverdraftAvailable;
            FinanceService.ObtainLoan(finances, borrowAmount);
            actions.Add($"Directors borrowed £{(int)borrowAmount:N0} to get out of financial trouble.");
        }

        // Step B — take a mortgage if not already held (lines 5426–5427)
        if (finances.BankBalance < 0 && finances.MortgageOutstanding == 0)
        {
            FinanceService.ObtainMortgage(finances, club.Division);
            actions.Add($"Directors took a mortgage of £{(int)finances.MortgageOutstanding:N0} to get out of financial trouble.");
        }

        // Step C — sell all but one share (lines 5428–5429, 5446)
        if (finances.BankBalance < 0 && finances.SharesOwned >= 2)
        {
            long sharesToSell     = finances.SharesOwned - 1;
            double sharePricePence = finances.SharePriceInPence;

            // Price drop due to bulk selling (line 5429: AV=INT(DW/1000))
            double priceDrop      = Math.Max(0, (int)(sharesToSell / 1000));
            finances.SharePriceInPence -= (finances.SharePriceInPence / 100.0) * priceDrop;

            double saleProceeds   = (int)(finances.SharePriceInPence * sharesToSell) / 100.0;
            finances.SharesOwned  = 1;
            finances.BankBalance += (int)saleProceeds;

            // Share price gravity correction (line 5446)
            double convergenceFactor = finances.SharePriceInPence < 95 ? 1 : -1;
            finances.SharePriceInPence +=
                (int)(((100 - finances.SharePriceInPence) / 2.0) * (1 + convergenceFactor));

            actions.Add($"Club sold shares raising £{(int)saleProceeds:N0} to get out of financial trouble.");
        }

        // Step D — force-sell transfer-listed players one at a time (lines 5430–5435)
        int rescueAttempts = 0;
        while (finances.BankBalance < 0 && rescueAttempts++ < 20)
        {
            int listedSlot = FindTransferListedSlot(gameState.Squad);
            if (listedSlot == 0) break;

            var    player    = gameState.Squad[listedSlot]!;
            double salePrice = ForceSalePrice(player, rng);
            string soldName  = player.Name.Trim();

            finances.BankBalance += (int)salePrice;
            PlayerService.ClearSlot(gameState.Squad, listedSlot);
            actions.Add($"Directors sold {soldName} for £{(int)salePrice:N0} to get out of financial trouble.");
        }

        // ── Final solvency check (line 5437) ──────────────────────────────────
        if (finances.BankBalance < 0)
        {
            club.ManagerContractWeeks = 0;
            actions.Add("You have been sacked.");
            return new CrisisResult(CrisisOutcome.Sacked, actions);
        }

        return new CrisisResult(CrisisOutcome.Rescued, actions);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Finds the first transfer-listed squad slot (Age &lt; 0).
    /// BASIC line 5430: IF G(I)&lt;0 THEN flag=I:I=99.
    /// </summary>
    private static int FindTransferListedSlot(Player?[] squad)
    {
        for (int slot = 1; slot <= 20; slot++)
        {
            var player = squad[slot];
            if (player != null && player.IsTransferListed)
                return slot;
        }
        return 0;
    }

    /// <summary>
    /// Calculates a forced-sale price for a player the directors are selling.
    ///
    /// BASIC lines 5432–5434:
    ///   If INT(skill)=1: price = 6500
    ///   Otherwise: price = 5000 * (INT(H)-1)^2 * 2 + (star player bonus)
    ///   Add random spread, then apply to finances.
    /// </summary>
    private static double ForceSalePrice(Player player, Random rng)
    {
        int intSkill = (int)player.Skill;

        double basePrice = intSkill <= 1
            ? 6_500
            : 5_000.0 * (intSkill - 1) * (intSkill - 1) * 2
              + (player.Status == PlayerStatus.Star ? 310_000 : 0);

        double spread = (int)(basePrice / 3) + 6_000;
        return (int)(basePrice + rng.NextDouble() * spread);
    }
}

// ── Result types ──────────────────────────────────────────────────────────────

public enum CrisisOutcome
{
    /// <summary>Bank balance positive and form acceptable. No action taken.</summary>
    NoAction,

    /// <summary>Poor recent form — directors gave the manager another chance.</summary>
    AnotherChance,

    /// <summary>Poor recent form — manager given one week's notice (contract zeroed).</summary>
    OnNotice,

    /// <summary>Club rescued from negative balance by directors' emergency measures.</summary>
    Rescued,

    /// <summary>Club could not be rescued — manager sacked for insolvency.</summary>
    Sacked,

    /// <summary>Too many shares were sold — new board sacked the manager immediately.</summary>
    SackedSharesSold
}

public class CrisisResult
{
    public CrisisOutcome  Outcome     { get; }
    public List<string>   Actions     { get; }
    public string         Summary     { get; }

    public bool ManagerSacked =>
        Outcome is CrisisOutcome.Sacked or CrisisOutcome.OnNotice or CrisisOutcome.SackedSharesSold;

    public CrisisResult(CrisisOutcome outcome, string summary = "")
    {
        Outcome = outcome;
        Summary = summary;
        Actions = new List<string>();
        if (!string.IsNullOrEmpty(summary)) Actions.Add(summary);
    }

    public CrisisResult(CrisisOutcome outcome, List<string> actions)
    {
        Outcome = outcome;
        Actions = actions;
        Summary = string.Join(" ", actions);
    }
}
