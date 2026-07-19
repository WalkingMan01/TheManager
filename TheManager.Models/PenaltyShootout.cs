namespace TheManager.Models;

/// <summary>
/// One kick in a penalty shootout, in the order the kicks were taken.
/// No FOOT.BAS equivalent — new mechanic.
/// </summary>
public class PenaltyKick
{
    /// <summary>Our players' real name, or "&lt;Club&gt; — kick n" for the unnamed opposition.</summary>
    public string TakerName { get; set; } = string.Empty;

    public bool IsOurKick { get; set; }

    public PenaltyOutcome Outcome { get; set; }

    public bool Scored => Outcome == PenaltyOutcome.Scored;

    /// <summary>Our running tally after this kick.</summary>
    public int OurRunningScore { get; set; }

    /// <summary>Opponent running tally after this kick.</summary>
    public int TheirRunningScore { get; set; }

    /// <summary>True once the shootout has passed five kicks each and entered sudden death.</summary>
    public bool IsSuddenDeath { get; set; }
}

/// <summary>
/// A completed penalty shootout, pre-computed so the UI can reveal it one kick
/// at a time (the same approach the match engine uses for goal events).
/// </summary>
public class PenaltyShootoutResult
{
    /// <summary>Every kick taken, in order.</summary>
    public List<PenaltyKick> Kicks { get; set; } = new();

    public int  OurScore   { get; set; }
    public int  TheirScore { get; set; }
    public bool WeWon      { get; set; }

    /// <summary>True when the shootout went past five kicks per side.</summary>
    public bool WentToSuddenDeath { get; set; }
}
