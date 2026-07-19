namespace TheManager.Models;

/// <summary>
/// State for the player's managed club.
/// </summary>
public class Club
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Club name (9 chars). Corresponds to Z$ in FOOT.BAS.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Current division. Corresponds to AP.</summary>
    public Division Division { get; set; }

    /// <summary>
    /// Current league position (1 = top). Set at season start and updated
    /// by the league sort routine (subroutine 3957). Corresponds to ag.
    /// </summary>
    public int LeaguePosition { get; set; }

    // ── Manager ───────────────────────────────────────────────────────────────

    /// <summary>Manager's name (up to 8 chars). Corresponds to F$.</summary>
    public string ManagerName { get; set; } = string.Empty;

    /// <summary>Manager's weekly wage. Corresponds to NA.</summary>
    public double ManagerWeeklyWage { get; set; }

    /// <summary>Manager's contract weeks remaining. Corresponds to NC.</summary>
    public int ManagerContractWeeks { get; set; }

    // ── Ground ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ticket price in pounds (e.g. 1.0 = £1.00). Corresponds to nj.
    /// Initialised to (5 - Division) at game start.
    /// </summary>
    public double TicketPriceInPounds { get; set; }

    /// <summary>
    /// Cost to upgrade to maximum ground capacity. 0 means already at maximum.
    /// Corresponds to NI. Value = INT(1,500,000 / Division).
    /// </summary>
    public double GroundImprovementCost { get; set; }

    public bool GroundIsMaxCapacity => GroundImprovementCost == 0;

    /// <summary>
    /// Ground capacity in spectators. New — no BASIC equivalent; the original's
    /// only capacity notion was a hard 18,721 cap while NI > 0 (subroutine 3801).
    /// 0 in a loaded save means "seed from club name / division" (see SaveLoadService).
    /// </summary>
    public int GroundCapacity { get; set; }

    /// <summary>
    /// Ground name (e.g. "Old Trafford"). New — no BASIC equivalent. Seeded with
    /// the capacity; "" in a loaded save means "seed from club name / division".
    /// </summary>
    public string GroundName { get; set; } = "";

    // ── League rules ──────────────────────────────────────────────────────────

    /// <summary>Points awarded for a win (2 or 3). Corresponds to PTS%.</summary>
    public int PointsPerWin { get; set; } = 3;

    // ── Team morale & temper ──────────────────────────────────────────────────

    /// <summary>
    /// Team morale 2–99. Affects attendance and match results.
    /// Rises on wins, falls on losses, influenced by bonus pay.
    /// Corresponds to me.
    /// </summary>
    public int TeamMorale { get; set; }

    /// <summary>
    /// Aggregate temper of the starting eleven. High value = disciplinary risk.
    /// Corresponds to pu (sum of E(3,I) for players 1–11).
    /// </summary>
    public int TeamTemper { get; set; }

    // ── Manager performance ───────────────────────────────────────────────────

    /// <summary>
    /// End-of-season manager rating (0–100%). Calculated from results,
    /// bank balance, and time in post. Corresponds to JW.
    /// </summary>
    public int ManagerRating { get; set; }

    // ── Extra training sessions (hours used this week) ────────────────────────

    /// <summary>Extra training hours for goalkeepers this week. Corresponds to mw.</summary>
    public int ExtraTrainingGoalkeeping { get; set; }

    /// <summary>Extra training hours for defenders this week. Corresponds to mx.</summary>
    public int ExtraTrainingDefence { get; set; }

    /// <summary>Extra training hours for midfielders this week. Corresponds to my.</summary>
    public int ExtraTrainingMidfield { get; set; }

    /// <summary>Extra training hours for attackers this week. Corresponds to mz.</summary>
    public int ExtraTrainingAttack { get; set; }

    // ── Staff presence flags ──────────────────────────────────────────────────

    /// <summary>Whether the club has a coach hired. Corresponds to NL=1.</summary>
    public bool HasCoach { get; set; }

    /// <summary>Whether the club has a physio hired. Corresponds to NM=1.</summary>
    public bool HasPhysio { get; set; }

    /// <summary>Number of scouts currently employed (0–3). Corresponds to NN.</summary>
    public int ScoutCount { get; set; }

    /// <summary>Number of youth players on the books (0–7). Corresponds to NO.</summary>
    public int YouthPlayerCount { get; set; }

    // ── Cup progression ───────────────────────────────────────────────────────

    /// <summary>Highest League Cup round reached this season. Corresponds to OJ.</summary>
    public CupRound LeagueCupRound { get; set; }

    /// <summary>Highest FA Cup round reached this season. Corresponds to OK.</summary>
    public CupRound FACupRound { get; set; }
}
