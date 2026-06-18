namespace TheManager.Models;

/// <summary>A suspension newly imposed during a match, for the weekly news block.</summary>
public class SuspensionNotice
{
    public string           PlayerName { get; set; } = string.Empty;
    public int               MatchesOut { get; set; }
    public SuspensionReason Reason     { get; set; }
}
