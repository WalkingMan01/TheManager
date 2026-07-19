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
    NoFixture
}
