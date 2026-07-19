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

    /// <summary>
    /// Real-world ground name and approximate capacity (circa 2025–26) for every
    /// league club in <see cref="Names"/> (indices 1–92). Cup-only non-league
    /// entrants are absent: only the managed club's ground is ever used.
    /// Spec: docs/specs/gate-receipts-ground-capacity.md.
    /// </summary>
    private static readonly Dictionary<string, (string GroundName, int Capacity)> Grounds = new()
    {
        // ── Premier League ────────────────────────────────────────────────────
        ["AFC Bournemouth"]         = ("Vitality Stadium", 11_300),
        ["Arsenal"]                 = ("Emirates Stadium", 60_700),
        ["Aston Villa"]             = ("Villa Park", 42_900),
        ["Brentford"]               = ("Gtech Community Stadium", 17_250),
        ["Brighton & Hove Albion"]  = ("Amex Stadium", 31_900),
        ["Chelsea"]                 = ("Stamford Bridge", 40_300),
        ["Coventry City"]           = ("CBS Arena", 32_600),
        ["Crystal Palace"]          = ("Selhurst Park", 25_500),
        ["Everton"]                 = ("Hill Dickinson Stadium", 52_900),
        ["Fulham"]                  = ("Craven Cottage", 29_600),
        ["Hull City"]               = ("MKM Stadium", 25_400),
        ["Ipswich Town"]            = ("Portman Road", 29_700),
        ["Leeds United"]            = ("Elland Road", 37_600),
        ["Liverpool"]               = ("Anfield", 61_300),
        ["Manchester City"]         = ("Etihad Stadium", 53_400),
        ["Manchester United"]       = ("Old Trafford", 74_300),
        ["Newcastle United"]        = ("St James' Park", 52_300),
        ["Nottingham Forest"]       = ("City Ground", 30_400),
        ["Sunderland"]              = ("Stadium of Light", 49_000),
        ["Tottenham Hotspur"]       = ("Tottenham Hotspur Stadium", 62_850),

        // ── Championship ──────────────────────────────────────────────────────
        ["Birmingham City"]         = ("St Andrew's", 29_400),
        ["Blackburn Rovers"]        = ("Ewood Park", 31_400),
        ["Bolton Wanderers"]        = ("Toughsheet Community Stadium", 28_700),
        ["Bristol City"]            = ("Ashton Gate", 27_000),
        ["Burnley"]                 = ("Turf Moor", 21_900),
        ["Cardiff City"]            = ("Cardiff City Stadium", 33_300),
        ["Charlton Athletic"]       = ("The Valley", 27_100),
        ["Derby County"]            = ("Pride Park", 33_000),
        ["Lincoln City"]            = ("LNER Stadium", 10_700),
        ["Middlesbrough"]           = ("Riverside Stadium", 34_700),
        ["Millwall"]                = ("The Den", 20_100),
        ["Norwich City"]            = ("Carrow Road", 27_200),
        ["Portsmouth"]              = ("Fratton Park", 21_000),
        ["Preston North End"]       = ("Deepdale", 23_400),
        ["Queens Park Rangers"]     = ("Loftus Road", 18_400),
        ["Sheffield United"]        = ("Bramall Lane", 32_100),
        ["Southampton"]             = ("St Mary's Stadium", 32_400),
        ["Stoke City"]              = ("bet365 Stadium", 30_100),
        ["Swansea City"]            = ("Swansea.com Stadium", 21_100),
        ["Watford"]                 = ("Vicarage Road", 22_200),
        ["West Bromwich Albion"]    = ("The Hawthorns", 26_800),
        ["West Ham United"]         = ("London Stadium", 62_500),
        ["Wolverhampton Wanderers"] = ("Molineux", 31_750),
        ["Wrexham"]                 = ("Racecourse Ground", 13_300),

        // ── League One ────────────────────────────────────────────────────────
        ["AFC Wimbledon"]           = ("Cherry Red Records Stadium", 9_200),
        ["Barnsley"]                = ("Oakwell", 23_300),
        ["Blackpool"]               = ("Bloomfield Road", 16_600),
        ["Bradford City"]           = ("Valley Parade", 25_100),
        ["Bromley"]                 = ("Hayes Lane", 5_000),
        ["Burton Albion"]           = ("Pirelli Stadium", 6_900),
        ["Cambridge United"]        = ("Abbey Stadium", 8_100),
        ["Doncaster Rovers"]        = ("Eco-Power Stadium", 15_200),
        ["Huddersfield Town"]       = ("John Smith's Stadium", 24_100),
        ["Leicester City"]          = ("King Power Stadium", 32_300),
        ["Leyton Orient"]           = ("Brisbane Road", 9_300),
        ["Luton Town"]              = ("Kenilworth Road", 11_500),
        ["Mansfield Town"]          = ("Field Mill", 9_200),
        ["MK Dons"]                 = ("Stadium MK", 30_500),
        ["Notts County"]            = ("Meadow Lane", 19_800),
        ["Oxford United"]           = ("Kassam Stadium", 12_500),
        ["Peterborough United"]     = ("Weston Homes Stadium", 15_300),
        ["Plymouth Argyle"]         = ("Home Park", 17_900),
        ["Reading"]                 = ("Select Car Leasing Stadium", 24_200),
        ["Sheffield Wednesday"]     = ("Hillsborough", 39_700),
        ["Stevenage"]               = ("Lamex Stadium", 7_800),
        ["Stockport County"]        = ("Edgeley Park", 13_300),
        ["Wigan Athletic"]          = ("Brick Community Stadium", 25_100),
        ["Wycombe Wanderers"]       = ("Adams Park", 10_100),

        // ── League Two ────────────────────────────────────────────────────────
        ["Accrington Stanley"]      = ("Wham Stadium", 5_450),
        ["Barnet"]                  = ("The Hive", 6_500),
        ["Bristol Rovers"]          = ("Memorial Stadium", 9_800),
        ["Cheltenham Town"]         = ("Whaddon Road", 7_100),
        ["Chesterfield"]            = ("Technique Stadium", 10_500),
        ["Colchester United"]       = ("JobServe Community Stadium", 10_100),
        ["Crawley Town"]            = ("Broadfield Stadium", 6_000),
        ["Crewe Alexandra"]         = ("Gresty Road", 10_150),
        ["Exeter City"]             = ("St James Park", 8_700),
        ["Fleetwood Town"]          = ("Highbury Stadium", 5_300),
        ["Gillingham"]              = ("Priestfield Stadium", 11_600),
        ["Grimsby Town"]            = ("Blundell Park", 9_100),
        ["Newport County"]          = ("Rodney Parade", 8_700),
        ["Northampton Town"]        = ("Sixfields Stadium", 7_800),
        ["Oldham Athletic"]         = ("Boundary Park", 13_500),
        ["Port Vale"]               = ("Vale Park", 15_000),
        ["Rochdale"]                = ("Crown Oil Arena", 10_200),
        ["Rotherham United"]        = ("New York Stadium", 12_000),
        ["Salford City"]            = ("Peninsula Stadium", 5_100),
        ["Shrewsbury Town"]         = ("Montgomery Waters Meadow", 9_900),
        ["Swindon Town"]            = ("County Ground", 15_700),
        ["Tranmere Rovers"]         = ("Prenton Park", 16_600),
        ["Walsall"]                 = ("Bescot Stadium", 11_300),
        ["York City"]               = ("LNER Community Stadium", 8_500),
    };

    /// <summary>
    /// Looks up the real ground name and capacity for a league club.
    /// Returns false for non-league sides, custom club names, or blanks.
    /// </summary>
    public static bool TryGetGround(string clubName, out string groundName, out int capacity)
    {
        if (Grounds.TryGetValue(clubName.Trim(), out var ground))
        {
            (groundName, capacity) = ground;
            return true;
        }

        groundName = "";
        capacity   = 0;
        return false;
    }

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
