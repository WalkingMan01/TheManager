using TheManager.Models;

namespace TheManager.Services;

/// <summary>
/// Manages the lifecycle of non-player staff: coach, physio, scouts,
/// and youth players (hire, fire, generate, random resignation).
///
/// Corresponds to subroutines 4101–4161 and 4153–4158 in FOOT.BAS
/// (lines 3460–3621).
/// </summary>
public static class StaffService
{
    // ── Coach ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hires a newly generated coach. Returns null if a coach is already employed.
    /// BASIC line 4122: NL toggle + GOSUB 4144 (generate) or 4145 (sack).
    /// </summary>
    public static Coach? HireCoach(GameState gameState, Random rng)
    {
        if (gameState.Club.HasCoach) return null;
        gameState.Coach         = GenerateCoach(rng);
        gameState.Club.HasCoach = true;
        return gameState.Coach;
    }

    /// <summary>Fires the current coach. BASIC line 4123: GOSUB 4145.</summary>
    public static void SackCoach(GameState gameState)
    {
        gameState.Coach         = null;
        gameState.Club.HasCoach = false;
    }

    // ── Physio ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hires a newly generated physio. Returns null if one is already employed.
    /// BASIC line 4125: NM toggle.
    /// </summary>
    public static Physio? HirePhysio(GameState gameState, Random rng)
    {
        if (gameState.Club.HasPhysio) return null;
        gameState.Physio         = GeneratePhysio(rng);
        gameState.Club.HasPhysio = true;
        return gameState.Physio;
    }

    /// <summary>Fires the current physio.</summary>
    public static void SackPhysio(GameState gameState)
    {
        gameState.Physio         = null;
        gameState.Club.HasPhysio = false;
    }

    // ── Scouts ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hires a new scout (max 3). Returns null if already at capacity.
    /// BASIC lines 4126–4127: NN++ if NN &lt; 3.
    /// </summary>
    public static Scout? HireScout(GameState gameState, Random rng)
    {
        if (gameState.Club.ScoutCount >= 3) return null;
        var scout = GenerateScout(rng);
        gameState.Scouts.Add(scout);
        gameState.Club.ScoutCount++;
        return scout;
    }

    /// <summary>
    /// Fires the scout at <paramref name="index"/> (0-based).
    /// BASIC lines 4139–4149.
    /// </summary>
    public static bool SackScout(GameState gameState, int index)
    {
        if (index < 0 || index >= gameState.Scouts.Count) return false;
        gameState.Scouts.RemoveAt(index);
        gameState.Club.ScoutCount = Math.Max(0, gameState.Club.ScoutCount - 1);
        return true;
    }

    // ── Youth players ─────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to hire a youth player. The player is generated and shown;
    /// acceptance is probabilistic (1/3 chance they turn you down).
    /// Returns the player if hired, null if refused.
    ///
    /// BASIC lines 4133–4138:
    ///   Generate player (GOSUB 4144 → 4159).
    ///   RA=1+RND*3; if RA=3 → player refuses; else accept.
    /// </summary>
    public static YouthPlayer? HireYouthPlayer(GameState gameState, Random rng)
    {
        if (gameState.Club.YouthPlayerCount >= 7) return null;

        var candidate = GenerateYouthPlayer(rng);

        // 1/3 chance the prospect declines (line 4138: RA=1+RND*3; if RA=3 → refuse)
        if (1 + rng.Next(3) == 3) return null;

        gameState.YouthTeam.Add(candidate);
        gameState.Club.YouthPlayerCount++;
        return candidate;
    }

    /// <summary>
    /// Releases the youth player at <paramref name="index"/> (0-based).
    /// BASIC lines 4131–4132.
    /// </summary>
    public static bool SackYouthPlayer(GameState gameState, int index)
    {
        if (index < 0 || index >= gameState.YouthTeam.Count) return false;
        gameState.YouthTeam.RemoveAt(index);
        gameState.Club.YouthPlayerCount = Math.Max(0, gameState.Club.YouthPlayerCount - 1);
        return true;
    }

    // ── Youth promotion (BASIC lines 3998–4021) ──────────────────────────────

    /// <summary>
    /// Promotes a youth player into the first available reserve squad slot (13–20),
    /// converting their skill percentage to a player skill rating.
    ///
    /// BASIC lines 3998–4021:
    ///   Eligibility gate:  Y(2,HC+5) &lt; 50 → refused (line 3998).
    ///   Free-slot search:  T(I)=0 for I=13..20 (lines 3999–4001).
    ///   Skill conversion:  H(I) = INT(Y(2,HC+5)/30)           (line 4005)
    ///                           + INT(RND*3)/10  (0.0–0.2)    (line 4006)
    ///                           + (AP &lt; 3) ? (3−AP) : 0      (line 4007, division bonus)
    ///                      clamped to minimum 1.1              (line 4008).
    ///   Youth slot cleared: name and all Y() values zeroed     (lines 4014–4018).
    ///
    /// Returns the new <see cref="Player"/> and the squad slot it was placed in,
    /// or null if the youth player is ineligible or no free reserve slot exists.
    /// </summary>
    public static (Player player, int slot)? PromoteYouthPlayer(
        GameState gameState, int youthIndex, Random rng)
    {
        if (youthIndex < 0 || youthIndex >= gameState.YouthTeam.Count) return null;

        var youth = gameState.YouthTeam[youthIndex];
        if (!youth.IsEligibleForPromotion) return null;

        // Find first empty reserve slot (13–20)
        int freeSlot = -1;
        for (int i = 13; i <= 20; i++)
            if (gameState.Squad[i] is null) { freeSlot = i; break; }
        if (freeSlot < 0) return null;

        // Skill conversion: INT(skillPercent/30) + 0.0|0.1|0.2 + division bonus
        int    divNum       = (int)gameState.Club.Division;
        int    divBonus     = divNum < 3 ? 3 - divNum : 0;   // +2 Div1, +1 Div2
        double skill        = (int)(youth.SkillPercent / 30.0)
                            + rng.Next(3) / 10.0
                            + divBonus;
        skill = Math.Max(1.1, skill);

        var player = new Player
        {
            Name        = youth.Name,
            Position    = youth.Position,
            Skill       = skill,
            Age         = youth.Age,
            Temper      = youth.Temper,
            GamesPlayed = 0,
            SeasonGoals = 0,
            Appearances = 0
        };
        PlayerService.RecalculateStatus(player);

        gameState.Squad[freeSlot] = player;

        // Clear youth slot
        gameState.YouthTeam.RemoveAt(youthIndex);
        gameState.Club.YouthPlayerCount = Math.Max(0, gameState.Club.YouthPlayerCount - 1);

        return (player, freeSlot);
    }

    // ── Random resignation (subroutine 4153–4158) ────────────────────────────

    /// <summary>
    /// Called once per week to check whether a staff member randomly resigns.
    ///
    /// BASIC lines 4153–4158:
    ///   RA = 1 + RND(0–98); if RA != 8 → no resignation.
    ///   Pick a random staff slot (1–5). If occupied → that member leaves.
    ///   Coach (IB=1): NL toggle + clear.
    ///   Physio (IB=2): NM toggle + clear.
    ///   Scout (IB=3–5): NN-- + clear.
    ///
    /// Returns a description of who resigned, or null if no event fired.
    /// </summary>
    public static string? CheckRandomResignation(GameState gameState, Random rng)
    {
        int resignationRoll = 1 + rng.Next(99);
        if (resignationRoll != 8) return null;   // 1/99 chance each week

        int staffSlot = 1 + rng.Next(5);   // picks from coach/physio/scout 1-3

        switch (staffSlot)
        {
            case 1 when gameState.Club.HasCoach:
                string coachName = gameState.Coach?.Name.Trim() ?? "Coach";
                SackCoach(gameState);
                return coachName;

            case 2 when gameState.Club.HasPhysio:
                string physioName = gameState.Physio?.Name.Trim() ?? "Physio";
                SackPhysio(gameState);
                return physioName;

            case >= 3 and <= 5 when gameState.Club.ScoutCount > 0:
                int scoutIndex = Math.Min(staffSlot - 3, gameState.Scouts.Count - 1);
                string scoutName = gameState.Scouts[scoutIndex].Name.Trim();
                SackScout(gameState, scoutIndex);
                return scoutName;
        }

        return null;
    }

    // ── Staff generation (subroutine 4159–4161) ───────────────────────────────

    /// <summary>
    /// Generates a new coach with random name, salary, and skill.
    ///
    /// BASIC line 4159 (IB=1):
    ///   salary  = 50 + (100 + RND(0–99)) = 150–249
    ///   skill   = 1 + RND(0–98)          = 1–99 %
    /// </summary>
    public static Coach GenerateCoach(Random rng) => new()
    {
        Name          = NameGenerationService.GenerateName(rng),
        WeeklySalary  = 50 + 100 + rng.Next(100),   // 150–249
        SkillPercent  = 1  + rng.Next(99)            // 1–99
    };

    /// <summary>
    /// Generates a new physio with random name, salary, and skill.
    /// Same formula as coach (IB=2, IB &lt; 6 condition holds).
    /// </summary>
    public static Physio GeneratePhysio(Random rng) => new()
    {
        Name          = NameGenerationService.GenerateName(rng),
        WeeklySalary  = 50 + 100 + rng.Next(100),
        SkillPercent  = 1  + rng.Next(99)
    };

    /// <summary>
    /// Generates a new scout with random name, salary, skill, and scouting targets.
    ///
    /// BASIC line 4159–4160 (IB=3–5):
    ///   salary   = 50 + (100 + RND(0–99))
    ///   skill    = 1 + RND(0–98)
    ///   position = 1 + RND(0–3)  (looking for)
    ///   form     = 4 + RND(0–4)  (target skill tier 4–8 on the 1–9 player-skill scale)
    ///
    /// Note: the BASIC stored 16 + RND(0–2) here, which was the youth-player age-start
    /// value (16–18), not a skill-scale number. ScoutReportService quality-gates on
    /// LookingForForm &lt;= divisionBonus (max 9), so 16–18 caused scouts to never find
    /// anyone. The correct range is 4–8 to match the 1–9 player-skill scale.
    /// </summary>
    public static Scout GenerateScout(Random rng) => new()
    {
        Name         = NameGenerationService.GenerateName(rng),
        WeeklySalary = 50 + 100 + rng.Next(100),
        SkillPercent = 1  + rng.Next(99)
        // LookingForPosition and LookingForForm are set by the player after hiring
    };

    /// <summary>
    /// Generates a youth player with random attributes.
    ///
    /// BASIC subroutine 5623 (line 4644):
    ///   salary   = 50 (fixed)
    ///   skill    = RND(0–59) %
    ///   potential= 35 + RND(0–64) %
    ///   position = 1 + RND(0–3)
    ///   age      = 16 + RND(0–1)
    ///   temper   = clamped(-3 + RND(0–16), 0, 9)
    /// </summary>
    public static YouthPlayer GenerateYouthPlayer(Random rng)
    {
        int rawTemper = -3 + rng.Next(17);
        return new YouthPlayer
        {
            Name                  = NameGenerationService.GenerateName(rng),
            WeeklySalary          = 50,
            SkillPercent          = rng.Next(60),             // Y(2,I+5)
            PotentialSkillPercent = 35 + rng.Next(65),        // Y(3,I+5)
            Position              = (PlayerPosition)(1 + rng.Next(4)),
            Age                   = 16 + rng.Next(2),
            Temper                = Math.Max(0, Math.Min(9, rawTemper))
        };
    }

    // ── Weekly wage total for all staff ──────────────────────────────────────

    /// <summary>
    /// Sums the weekly wage of all current staff members.
    /// Corresponds to the staff wage loop in subroutine 4808 (lines 3928–3937).
    /// </summary>
    public static double TotalStaffWageBill(GameState gameState)
    {
        double total = 0;
        if (gameState.Coach  != null) total += gameState.Coach.WeeklySalary;
        if (gameState.Physio != null) total += gameState.Physio.WeeklySalary;
        foreach (var scout in gameState.Scouts)        total += scout.WeeklySalary;
        foreach (var youth in gameState.YouthTeam)     total += youth.WeeklySalary;
        return total;
    }
}
