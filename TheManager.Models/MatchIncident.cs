namespace TheManager.Models;

/// <summary>A card or injury that occurred during a match, for live display.</summary>
public class MatchIncident
{
    public int          Minute     { get; set; }
    public string       PlayerName { get; set; } = string.Empty;
    public IncidentType Type       { get; set; }
    public int          WeeksOut   { get; set; }   // injury only
}
