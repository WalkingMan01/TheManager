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
    public static Coach? HireCoach(Club club, Random rng)
    {
        if (club.HasCoach) return null;
        var coach       = GenerateCoach((int)club.Division, rng);
        club.HasCoach   = true;
        return coach;
    }

    /// <summary>Fires the current coach. BASIC line 4123: GOSUB 4145.</summary>
    public static void SackCoach(Club club)
    {
        club.HasCoach = false;
    }

    // ── Physio ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hires a newly generated physio. Returns null if one is already employed.
    /// BASIC line 4125: NM toggle.
    /// </summary>
    public static Physio? HirePhysio(Club club, Random rng)
    {
        if (club.HasPhysio) return null;
        var physio      = GeneratePhysio((int)club.Division, rng);
        club.HasPhysio  = true;
        return physio;
    }

    /// <summary>Fires the current physio.</summary>
    public static void SackPhysio(Club club)
    {
        club.HasPhysio = false;
    }

    // ── Scouts ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hires a new scout (max 3). Returns null if already at capacity.
    /// BASIC lines 4126–4127: NN++ if NN &lt; 3.
    /// </summary>
    public static Scout? HireScout(Club club, List<Scout> scouts, Random rng)
    {
        if (club.ScoutCount >= 3) return null;
        var scout = GenerateScout((int)club.Division, rng);
        scouts.Add(scout);
        club.ScoutCount++;
        return scout;
    }

    /// <summary>
    /// Fires the scout at <paramref name="index"/> (0-based).
    /// BASIC lines 4139–4149.
    /// </summary>
    public static bool SackScout(Club club, List<Scout> scouts, int index)
    {
        if (index < 0 || index >= scouts.Count) return false;
        scouts.RemoveAt(index);
        club.ScoutCount = Math.Max(0, club.ScoutCount - 1);
        return true;
    }

    // ── Youth players ─────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to hire a youth player. The player is generated and shown;
    /// acceptance is guaranteed. Returns the hired player, or null if the
    /// youth team is already full.
    ///
    /// BASIC lines 4133–4138:
    ///   Generate player (GOSUB 4144 → 4159).
    ///
    /// Deliberate deviation from FOOT.BAS: <paramref name="position"/> lets the
    /// manager choose the recruit's position (the original always rolled it).
    /// Null keeps the original random roll.
    /// </summary>
    public static YouthPlayer? HireYouthPlayer(
        Club club, List<YouthPlayer> youthTeam, Random rng, PlayerPosition? position = null)
    {
        if (club.YouthPlayerCount >= 7) return null;

        var candidate = GenerateYouthPlayer((int)club.Division, rng, position);

        youthTeam.Add(candidate);
        club.YouthPlayerCount++;
        return candidate;
    }

    /// <summary>
    /// Releases the youth player at <paramref name="index"/> (0-based).
    /// BASIC lines 4131–4132.
    /// </summary>
    public static bool SackYouthPlayer(Club club, List<YouthPlayer> youthTeam, int index)
    {
        if (index < 0 || index >= youthTeam.Count) return false;
        youthTeam.RemoveAt(index);
        club.YouthPlayerCount = Math.Max(0, club.YouthPlayerCount - 1);
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
        Player?[] squad, List<YouthPlayer> youthTeam, Club club, int youthIndex, Random rng)
    {
        if (youthIndex < 0 || youthIndex >= youthTeam.Count) return null;

        var youth = youthTeam[youthIndex];
        if (!youth.IsEligibleForPromotion) return null;

        // Find first empty reserve slot (13–20)
        int freeSlot = -1;
        for (int i = 13; i <= 20; i++)
            if (squad[i] is null) { freeSlot = i; break; }
        if (freeSlot < 0) return null;

        // Skill conversion: INT(skillPercent/30) + 0.0|0.1|0.2 + division bonus
        int    divNum   = (int)club.Division;
        int    divBonus = divNum < 3 ? 3 - divNum : 0;   // +2 Div1, +1 Div2
        double skill    = (int)(youth.SkillPercent / 30.0)
                        + rng.Next(3) / 10.0
                        + divBonus;
        skill = Math.Max(1.1, skill);

        var player = new Player
        {
            Name          = youth.Name,
            Position      = youth.Position,
            Skill         = skill,
            Age           = youth.Age,
            Temper        = youth.Temper,
            GamesPlayed   = 0,
            SeasonGoals   = 0,
            Appearances   = 0,
            ContractWeeks = 50,
            // A promoted youth earns a real first-team wage from day one — the old
            // hardcoded £50/wk bypassed CalculateWage entirely, leaving them earning
            // pocket change beside teammates on thousands a week (docs/specs/player-wage-scaling.md).
            WeeklyWage    = InitializationService.CalculateWage(skill, youth.Age, divNum, rng),
        };

        // Hidden ceiling: reuse the youth's rolled potential (Y(3,I), percent
        // scale ÷ 10), but always leave headroom above the promoted skill.
        player.PeakAge        = 26 + rng.Next(5);
        player.PotentialSkill = Math.Max(skill + 0.3 + rng.Next(3) / 10.0,
                                         youth.PotentialSkillPercent / 10.0);

        PlayerService.RecalculateStatus(player);

        squad[freeSlot] = player;

        youthTeam.RemoveAt(youthIndex);
        club.YouthPlayerCount = Math.Max(0, club.YouthPlayerCount - 1);

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
    public static ResignationResult CheckRandomResignation(
        Club club, Coach? coach, Physio? physio, List<Scout> scouts, Random rng)
    {
        int resignationRoll = 1 + rng.Next(99);
        if (resignationRoll != 8) return ResignationResult.None;   // 1/99 chance each week

        int staffSlot = 1 + rng.Next(5);

        switch (staffSlot)
        {
            case 1 when club.HasCoach:
                string coachName = coach?.Name.Trim() ?? "Coach";
                SackCoach(club);
                return new ResignationResult(coachName, ResignationType.Coach);

            case 2 when club.HasPhysio:
                string physioName = physio?.Name.Trim() ?? "Physio";
                SackPhysio(club);
                return new ResignationResult(physioName, ResignationType.Physio);

            case >= 3 and <= 5 when club.ScoutCount > 0:
                int scoutIndex = Math.Min(staffSlot - 3, scouts.Count - 1);
                string scoutName = scouts[scoutIndex].Name.Trim();
                SackScout(club, scouts, scoutIndex);
                return new ResignationResult(scoutName, ResignationType.Scout);
        }

        return ResignationResult.None;
    }

    // ── Staff generation (subroutine 4159–4161) ───────────────────────────────

    /// <summary>
    /// Generates a new coach with random name, salary, and skill.
    ///
    /// BASIC line 4159 (IB=1):
    ///   salary  = 50 + (100 + RND(0–99)) = 150–249
    ///   skill   = 1 + RND(0–98)          = 1–99 %
    /// Deviation: salary scaled by Constants.WageScaleFactor and
    /// Constants.DivisionWageMultiplier (see docs/specs/player-wage-scaling.md) —
    /// unscaled, a coach's £150–249/wk was a rounding error next to the rescaled
    /// player wage bill.
    /// </summary>
    public static Coach GenerateCoach(int divisionNumber, Random rng) => new()
    {
        Name          = NameGenerationService.GenerateName(rng),
        WeeklySalary  = (50 + 100 + rng.Next(100)) * StaffWageScale(divisionNumber),   // 150–249, scaled
        SkillPercent  = 1  + rng.Next(99)            // 1–99
    };

    /// <summary>
    /// Generates a new physio with random name, salary, and skill.
    /// Same formula as coach (IB=2, IB &lt; 6 condition holds).
    /// </summary>
    public static Physio GeneratePhysio(int divisionNumber, Random rng) => new()
    {
        Name          = NameGenerationService.GenerateName(rng),
        WeeklySalary  = (50 + 100 + rng.Next(100)) * StaffWageScale(divisionNumber),
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
    public static Scout GenerateScout(int divisionNumber, Random rng) => new()
    {
        Name         = NameGenerationService.GenerateName(rng),
        WeeklySalary = (50 + 100 + rng.Next(100)) * StaffWageScale(divisionNumber),
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
    ///   position = 1 + RND(0–3), unless the caller chose one (see
    ///              <see cref="HireYouthPlayer"/> — deviation from FOOT.BAS)
    ///   age      = 16 + RND(0–1)
    ///   temper   = clamped(-3 + RND(0–16), 0, 9)
    /// </summary>
    public static YouthPlayer GenerateYouthPlayer(int divisionNumber, Random rng, PlayerPosition? position = null)
    {
        int rawTemper = -3 + rng.Next(17);
        return new YouthPlayer
        {
            Name                  = NameGenerationService.GenerateName(rng),
            WeeklySalary          = 50 * StaffWageScale(divisionNumber),
            SkillPercent          = rng.Next(60),             // Y(2,I+5)
            PotentialSkillPercent = 35 + rng.Next(65),        // Y(3,I+5)
            Position              = position ?? (PlayerPosition)(1 + rng.Next(4)),
            Age                   = 16 + rng.Next(2),
            Temper                = Math.Max(0, Math.Min(9, rawTemper))
        };
    }

    /// <summary>Shared staff-salary scale — same constants as player wages (see
    /// docs/specs/player-wage-scaling.md), so a coach/physio/scout/youth stipend
    /// stays proportionate to the division's player wage bill.</summary>
    private static double StaffWageScale(int divisionNumber)
        => Constants.WageScaleFactor * Constants.DivisionWageMultiplier(divisionNumber);

    // ── Weekly wage total for all staff ─────────────────────────────────────

    /// <summary>
    /// Sums the weekly wage of all current staff members.
    /// Corresponds to the staff wage loop in subroutine 4808 (lines 3928–3937).
    /// </summary>
    public static double TotalStaffWageBill(
        Coach? coach, Physio? physio,
        IReadOnlyList<Scout> scouts, IReadOnlyList<YouthPlayer> youthTeam)
    {
        double total = 0;
        if (coach  != null) total += coach.WeeklySalary;
        if (physio != null) total += physio.WeeklySalary;
        foreach (var scout in scouts)    total += scout.WeeklySalary;
        foreach (var youth in youthTeam) total += youth.WeeklySalary;
        return total;
    }
}

// ── Result types ──────────────────────────────────────────────────────────────

public enum ResignationType { None, Coach, Physio, Scout }

/// <summary>Output of <see cref="StaffService.CheckRandomResignation"/>.</summary>
public record ResignationResult(string? ResignerName, ResignationType Type)
{
    public static readonly ResignationResult None = new(null, ResignationType.None);
}
