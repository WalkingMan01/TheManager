namespace FootballBoss.Models;

/// <summary>Outcome of a crowd or discipline incident during a match.</summary>
public class IncidentResult
{
    public IncidentType Type       { get; set; }
    public int          PlayerSlot { get; set; }
    public string       PlayerName { get; set; } = string.Empty;
    public int          WeeksOut   { get; set; }   // injury only
}
