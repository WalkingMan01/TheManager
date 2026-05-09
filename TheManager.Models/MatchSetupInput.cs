namespace TheManager.Models;

/// <summary>
/// All inputs the match engine needs to set up a fixture.
/// Caller assembles this from <see cref="TeamRatings"/> and <see cref="Club"/> state.
/// </summary>
public class MatchSetupInput
{
    // Our team ratings (from TeamRatings / subroutine 332)
    public int OurGoalkeeperSkill      { get; set; }   // BA
    public int OurDefence              { get; set; }   // bc
    public int OurMid                  { get; set; }   // bb
    public int OurAttack               { get; set; }   // bd

    // Opponent estimated ratings (set by subroutine 413–419)
    public int OpponentGoalkeeperSkill { get; set; }   // EI1
    public int OpponentDefence         { get; set; }   // ej
    public int OpponentMid             { get; set; }   // ek
    public int OpponentAttack          { get; set; }   // el

    public bool IsHomeGame             { get; set; }   // BK%=1
    public bool LostLastMatch          { get; set; }   // aa=1
    public int  LineupChanges          { get; set; }   // bj — positional moves made this week
    public int  OurMorale              { get; set; }   // me
    public int  OpponentMorale         { get; set; }   // mm
    public int  OurTemper              { get; set; }   // pu
    public int  OpponentTemper         { get; set; }   // pv
    public int  Division               { get; set; }   // AP — used for division bonus on opponent shots

    // Cup tie second-leg carry-overs (jb/jc from first leg)
    public int  PreviousLegOurScore    { get; set; }
    public int  PreviousLegTheirScore  { get; set; }
}
