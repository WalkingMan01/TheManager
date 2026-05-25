namespace TheManager.Models;

/// <summary>
/// Player's current availability status. Derived from J(I) ASCII codes in FOOT.BAS.
/// </summary>
public enum PlayerStatus
{
    Normal,          // J=32  ' '  fit and available
    Improving,       // J=43  '+'  recovering well, slight skill boost
    Recovering,      // J=45  '-'  recovering, slight skill penalty
    Star,            // J=105 'i'  skill > 9.7
    Injured,         // J=35  '#'  unavailable (u(I) weeks remaining)
    OnLoan,          // J=42  '*'  out on loan
    LoanUnavailable, // J=76  'L'  loaned player not yet back
    International,   // J=73  'I'  released / sold
    Retiring,        // J=82  'R'  will retire end of season
    Suspended        // J=83  'S'  banned (u(I) weeks remaining)
}
