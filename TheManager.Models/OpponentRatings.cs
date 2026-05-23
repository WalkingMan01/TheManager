namespace TheManager.Models;

/// <summary>Estimated strength profile of the opposing team.</summary>
public class OpponentRatings
{
    public int GoalkeeperRating { get; set; }   // EI1
    public int DefenceRating    { get; set; }   // ej
    public int MidRating        { get; set; }   // ek
    public int AttackRating     { get; set; }   // el
    public int Morale           { get; set; }   // mm
    public int Temper           { get; set; }   // pv
    public int LeaguePosition   { get; set; }   // mn
    public int FormationCode    { get; set; }   // mv  (e.g. 442 or 433)
}
