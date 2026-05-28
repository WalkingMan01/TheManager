namespace TheManager.Models;

/// <summary>A player discovered by a scout during a weekly report pass.</summary>
public class ScoutFinding
{
    /// <summary>0-based index into GameState.Scouts.</summary>
    public int    ScoutIndex      { get; set; }

    public string ScoutName       { get; set; } = string.Empty;

    /// <summary>Squad slot the player is parked in (21, 22, or 23).</summary>
    public int    SquadSlot       { get; set; }

    public Player Player          { get; set; } = new();

    public int    SourceClubIndex { get; set; }
    public string SourceClubName  { get; set; } = string.Empty;
}
