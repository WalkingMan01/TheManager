namespace TheManager.Models;

/// <summary>A single cup fixture (one tie between two clubs).</summary>
public class CupFixture
{
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;

    /// <summary>Index of the home team in GameState.AllTeamNames.</summary>
    public int HomeTeamIndex { get; set; }

    /// <summary>Index of the away team in GameState.AllTeamNames.</summary>
    public int AwayTeamIndex { get; set; }

    public Division HomeDivision { get; set; }
    public Division AwayDivision { get; set; }

    /// <summary>Null until the match has been played.</summary>
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }

    /// <summary>True when this is a replay after a draw. Corresponds to dt flag.</summary>
    public bool IsReplay { get; set; }

    /// <summary>Winning team's name (scores can be level when decided on penalties).</summary>
    public string Winner { get; set; } = string.Empty;

    /// <summary>True when the tie was level and decided on penalties.</summary>
    public bool WonOnPenalties { get; set; }

    /// <summary>Home side's shootout tally. Null unless decided on penalties.</summary>
    public int? HomePenalties { get; set; }

    /// <summary>Away side's shootout tally. Null unless decided on penalties.</summary>
    public int? AwayPenalties { get; set; }
}
