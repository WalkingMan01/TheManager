namespace TheManager.Models;

/// <summary>
/// Round reached in a cup. Value 9 means the competition was won.
/// </summary>
public enum CupRound
{
    NotEntered   = 0,
    Round1       = 1,
    Round2       = 2,
    Round3       = 3,
    Round4       = 4,
    Round5       = 5,
    QuarterFinal = 6,
    SemiFinal    = 7,
    Final        = 8,
    Winner       = 9
}
