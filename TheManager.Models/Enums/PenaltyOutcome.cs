namespace TheManager.Models;

/// <summary>
/// Outcome of a single penalty kick in a shootout.
/// No FOOT.BAS equivalent — shootouts are a new mechanic (the original replayed
/// drawn cup ties instead).
/// </summary>
public enum PenaltyOutcome
{
    Scored,
    Saved,
    Wide,
    OverBar
}
