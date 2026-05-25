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
