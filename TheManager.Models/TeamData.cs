using TheManager.Models;

namespace TheManager.Models;

/// <summary>
/// Hardcoded 1988-era English football team names, mirroring the data.fd
/// file read by FOOT.BAS into Y$(1..106).
///
/// Divisions 1–4 occupy indices 1–80 (20 teams each).
/// Cup-only entrants occupy indices 81–96.
/// Indices 97–106 are unused (name suffixes are hardcoded in NameGenerationService).
/// All names are padded to exactly 9 characters to match the BASIC's
///   Y$(I) = Y$(I) + SPACE$(9 - LEN(Y$(I)))
/// </summary>
public static class TeamData
{
    private static readonly string[] Names =
    [
        "",              // 0 — unused (BASIC is 1-based)

        // ── Division 1 (1–20) ──────────────────────────────────────────────────
        "ARSENAL  ", "CHARLTON ", "CHELSEA  ", "COVENTRY ", "DERBY    ",
        "EVERTON  ", "LIVERPOOL", "LUTON    ", "MAN UTD  ", "MAN CITY ",
        "NEWCASTLE", "NORWICH  ", "NOTTM FOR", "OXFORD   ", "PORTSMPTH",
        "QPR      ", "SHEFF WED", "SOUTHMPTN", "WATFORD  ", "WEST HAM ",

        // ── Division 2 (21–40) ─────────────────────────────────────────────────
        "ASTON VIL", "BARNSLEY ", "BIRMINGHA", "BLACKBURN", "BRADFORD ",
        "CRYSTAL P", "HUDDERSFD", "HULL     ", "IPSWICH  ", "LEEDS    ",
        "LEICESTER", "MIDDLESBR", "MILLWALL ", "OLDHAM   ", "PLYMOUTH ",
        "SHEFF UTD", "SHREWSBY ", "STOKE    ", "SUNDERLD ", "SWINDON  ",

        // ── Division 3 (41–60) ─────────────────────────────────────────────────
        "BOLTON   ", "BRENTFORD", "BRISTOL C", "BRISTOL R", "BURY     ",
        "CARLISLE ", "CHESTER  ", "CHESTFD  ", "DONCASTER", "FULHAM   ",
        "GILLNGHAM", "NEWPORT  ", "NORTHMPTN", "NOTTS CO ", "PORT VALE",
        "ROTHERHAM", "SWANSEA  ", "WALSALL  ", "WIGAN    ", "YORK     ",

        // ── Division 4 (61–80) ─────────────────────────────────────────────────
        "ALDERSHOT", "BURNLEY  ", "CAMBRIDGE", "CARDIFF  ", "COLCHESTR",
        "CREWE    ", "DARLINGTN", "EXETER   ", "HARTLEPL ", "HEREFORD ",
        "LEYTON O ", "LINCOLN  ", "MANSFIELD", "PETERBRGH", "ROCHDALE ",
        "SCUNTHRPE", "SOUTHEND ", "STOCKPORT", "TORQUAY  ", "WREXHAM  ",

        // ── Cup-only entrants (81–96) ──────────────────────────────────────────
        "BATH CITY", "BLYTH SPR", "BOSTON U ", "CHELTNHAM", "ENFIELD  ",
        "FARNBRGH ", "KIDDERMIN", "MAIDSTONE", "MORECAMBE", "NORTHWICH",
        "STAFFORD ", "TELFORD  ", "WEALDSTNE", "WELLING U", "WMBLDON R",
        "YEOVIL   ",
    ];

    /// <summary>
    /// Seeds <see cref="GameState.AllTeamNames"/> with the built-in team list.
    /// All 107 slots are set to at least an empty string so downstream code
    /// that calls .Trim() without a null check is safe.
    /// </summary>
    public static void Seed(GameState state)
    {
        for (int i = 0; i < state.AllTeamNames.Length; i++)
            state.AllTeamNames[i] = string.Empty;

        for (int i = 1; i < Names.Length; i++)
            state.AllTeamNames[i] = Names[i];
    }
}
