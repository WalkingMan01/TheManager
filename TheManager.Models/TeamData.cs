using TheManager.Models;

namespace TheManager.Models;

/// <summary>
/// English football team names.
///
/// Premier League occupies indices 1–20 (20 teams).
/// Championship, League One, League Two occupy indices 21–44, 45–68, 69–92 (24 teams each).
/// Cup-only (non-league) entrants occupy indices 93–124 (32 teams — the FA Cup
/// first round is 48 league + 32 non-league sides).
/// </summary>
public static class TeamData
{
    private static readonly string[] Names =
    [
        "",              // 0 — unused (BASIC is 1-based)

        // ── Premier League (1–20) ──────────────────────────────────────────────
        "AFC Bournemouth", "Arsenal", "Aston Villa", "Brentford", "Brighton & Hove Albion",
        "Chelsea", "Coventry City", "Crystal Palace", "Everton", "Fulham",
        "Hull City", "Ipswich Town", "Leeds United", "Liverpool", "Manchester City",
        "Manchester United", "Newcastle United", "Nottingham Forest", "Sunderland", "Tottenham Hotspur",

        // ── Championship (21–44) ──────────────────────────────────────────────
        "Birmingham City", "Blackburn Rovers", "Bolton Wanderers", "Bristol City", "Burnley",
        "Cardiff City", "Charlton Athletic", "Derby County", "Lincoln City", "Middlesbrough",
        "Millwall", "Norwich City", "Portsmouth", "Preston North End", "Queens Park Rangers",
        "Sheffield United", "Southampton", "Stoke City", "Swansea City", "Watford",
        "West Bromwich Albion", "West Ham United", "Wolverhampton Wanderers", "Wrexham",

        // ── League One (45–68) ────────────────────────────────────────────────
        "AFC Wimbledon", "Barnsley", "Blackpool", "Bradford City", "Bromley",
        "Burton Albion", "Cambridge United", "Doncaster Rovers", "Huddersfield Town", "Leicester City",
        "Leyton Orient", "Luton Town", "Mansfield Town", "MK Dons", "Notts County",
        "Oxford United", "Peterborough United", "Plymouth Argyle", "Reading", "Sheffield Wednesday",
        "Stevenage", "Stockport County", "Wigan Athletic", "Wycombe Wanderers",

        // ── League Two (69–92) ────────────────────────────────────────────────
        "Accrington Stanley", "Barnet", "Bristol Rovers", "Cheltenham Town", "Chesterfield",
        "Colchester United", "Crawley Town", "Crewe Alexandra", "Exeter City", "Fleetwood Town",
        "Gillingham", "Grimsby Town", "Newport County", "Northampton Town", "Oldham Athletic",
        "Port Vale", "Rochdale", "Rotherham United", "Salford City", "Shrewsbury Town",
        "Swindon Town", "Tranmere Rovers", "Walsall", "York City",

        // ── Cup-only non-league entrants (93–124) ─────────────────────────────
        "Bath City", "Blyth Spartans", "Boston United", "Aldershot Town", "Enfield",
        "Farnborough Town", "Kidderminster Harriers", "Maidstone United", "Morecambe", "Northwich Victoria",
        "Stafford Rangers", "Telford United", "Wealdstone", "Welling United", "Wimbledon Reserves",
        "Yeovil Town", "Altrincham", "Barrow", "Dagenham & Redbridge", "Dover Athletic",
        "Eastleigh", "Gateshead", "Halifax Town", "Hartlepool United", "Hereford",
        "Macclesfield Town", "Runcorn", "Scarborough", "Solihull Moors", "Southport",
        "Sutton United", "Woking",
    ];

    /// <summary>Returns the trimmed team names for the given division (1–4), in order.</summary>
    public static IReadOnlyList<string> GetDivisionTeams(Division division)
    {
        var (start, _) = Constants.DivisionRange(division);
        int count      = Constants.TeamCount(division);
        return Enumerable.Range(start, count).Select(i => Names[i].Trim()).ToList();
    }

    /// <summary>
    /// Seeds <see cref="GameState.AllTeamNames"/> with the built-in team list.
    /// All slots are set to at least an empty string so downstream code
    /// that calls .Trim() without a null check is safe.
    /// </summary>
    public static void Seed(GameState state)
    {
        for (int i = 0; i < state.AllTeamNames.Length; i++)
            state.AllTeamNames[i] = string.Empty;

        for (int i = 1; i < Names.Length; i++)
            state.AllTeamNames[i] = Names[i];
    }

    /// <summary>
    /// Copies the built-in names into any empty slots of an existing array.
    /// Used by save migration when the team pool grows between versions.
    /// </summary>
    public static void FillMissing(string[] allTeamNames)
    {
        int limit = Math.Min(Names.Length, allTeamNames.Length);
        for (int i = 1; i < limit; i++)
        {
            if (string.IsNullOrWhiteSpace(allTeamNames[i]))
                allTeamNames[i] = Names[i];
        }
    }
}
