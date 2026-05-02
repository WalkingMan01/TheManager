namespace FootballBoss.Models;

/// <summary>
/// Historical record for one completed season.
/// Up to 10 seasons are kept, rolling over oldest when full.
/// Maps to K(10, 8) in FOOT.BAS.
/// </summary>
public class SeasonRecord
{
    /// <summary>Calendar season number (ah + 1 at time of recording). Corresponds to K(I,1).</summary>
    public int SeasonNumber { get; set; }

    /// <summary>Final league position. Corresponds to K(I,2).</summary>
    public int FinalLeaguePosition { get; set; }

    /// <summary>Highest League Cup round reached (0 = not entered). Corresponds to K(I,3).</summary>
    public int LeagueCupRoundReached { get; set; }

    /// <summary>Highest FA Cup round reached (0 = not entered). Corresponds to K(I,4).</summary>
    public int FACupRoundReached { get; set; }

    /// <summary>Division the club played in this season. Corresponds to K(I,5).</summary>
    public Division Division { get; set; }

    /// <summary>True if the League Cup was won this season. Corresponds to K(I,6)=1.</summary>
    public bool WonLeagueCup { get; set; }

    /// <summary>True if the FA Cup was won this season. Corresponds to K(I,7)=1.</summary>
    public bool WonFACup { get; set; }

    /// <summary>True if a European trophy was won this season. Corresponds to K(I,8)=1.</summary>
    public bool WonEuropean { get; set; }
}
