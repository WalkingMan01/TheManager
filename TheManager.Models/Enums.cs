namespace TheManager.Models;

/// <summary>
/// Player's position on the pitch. Matches T(I) values in FOOT.BAS.
/// </summary>
public enum PlayerPosition
{
    None       = 0,
    Goalkeeper = 1,
    Defender   = 2,
    Midfielder = 3,
    Attacker   = 4
}

/// <summary>
/// Player's current availability status. Derived from J(I) ASCII codes in FOOT.BAS.
/// </summary>
public enum PlayerStatus
{
    Normal,        // J=32  ' '  fit and available
    Improving,     // J=43  '+'  recovering well, slight skill boost
    Recovering,    // J=45  '-'  recovering, slight skill penalty
    Star,          // J=105 'i'  skill > 9.7
    Injured,       // J=35  '#'  unavailable (u(I) weeks remaining)
    OnLoan,        // J=42  '*'  out on loan
    LoanUnavailable, // J=76 'L'  loaned player not yet back
    International, // J=73  'I'  released / sold
    Retiring,      // J=82  'R'  will retire end of season
    Suspended      // J=83  'S'  banned (u(I) weeks remaining)
}

/// <summary>Type of in-match incident resolved by <see cref="TheManager.Services.MatchEngine"/>.</summary>
public enum IncidentType { Injury, RedCard }

/// <summary>
/// Football division (1=top flight). Matches AP variable in FOOT.BAS.
/// </summary>
public enum Division
{
    One   = 1,
    Two   = 2,
    Three = 3,
    Four  = 4
}

/// <summary>
/// Type of cup competition. J=1 for League Cup, J=3 for FA Cup in draw arrays.
/// </summary>
public enum CupType
{
    LeagueCup = 1,
    FACup     = 3
}

/// <summary>
/// Round reached in a cup. Value 9 means the competition was won.
/// </summary>
public enum CupRound
{
    NotEntered    = 0,
    Round1        = 1,
    Round2        = 2,
    Round3        = 3,
    Round4        = 4,
    Round5        = 5,
    QuarterFinal  = 6,
    SemiFinal     = 7,
    Final         = 8,
    Winner        = 9
}

/// <summary>
/// European competition entered. Matches be variable in FOOT.BAS.
/// </summary>
public enum EuropeanCompetitionType
{
    None        = 0,
    European    = 1,   // Champions Cup equivalent
    UEFA        = 2,
    CupWinners  = 3
}

public enum MatchType
{
    League,
    LeagueCup,
    FACup,
    EuropeanFirstLeg,
    EuropeanSecondLeg,
    EuropeanFriendly,
    Replay,
    EndOfSeason
}
