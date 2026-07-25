namespace TheManager.Models;

public enum MatchType
{
    League,
    LeagueCup,
    FACup,
    EuropeanFirstLeg,
    EuropeanSecondLeg,
    EuropeanFriendly,
    Replay,
    EndOfSeason,

    /// <summary>A rest matchday — no fixture scheduled (Division One league gaps,
    /// or a cup matchday after elimination). The weekly tick still runs.</summary>
    NoFixture,

    /// <summary>A promotion play-off semi-final leg or final (Divisions Two–Four
    /// only). No FOOT.BAS equivalent — see docs/specs/promotion-playoffs.md.</summary>
    Playoff
}
