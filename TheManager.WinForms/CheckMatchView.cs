using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.WinForms;

/// <summary>UserControl that previews the next fixture and compares team ratings.</summary>
public partial class CheckMatchView : UserControl
{
    private readonly GameState _state;
    private readonly Random    _rng;

    /// <summary>Initialises the check match view with the provided game state and random instance.</summary>
    public CheckMatchView(GameState state, Random rng)
    {
        _state = state;
        _rng   = rng;
        InitializeComponent();
        Populate();
    }

    // ── Data population ───────────────────────────────────────────────────────

    private void Populate()
    {
        var match = FixtureSchedulerService.GetCurrentMatch(_state);

        if (match.MatchType == MatchType.EndOfSeason)
        {
            lblMatchInfo.Text = "Season complete — no further fixtures.";
            lblOurTeam.Text   = string.Empty;
            lblOpponent.Text  = string.Empty;
            return;
        }

        string venueLabel = match.IsHomeGame ? "Home" : "Away";
        lblMatchInfo.Text = $"Week {match.Week}  ·  {MatchTypeLabel(match.MatchType)}  ·  {venueLabel}";

        string ourName = _state.Club.Name.Trim();
        string oppName = match.OpponentName.Trim();

        // Left label is always the home side, right is away — matches typical scoreline convention
        lblOurTeam.Text  = match.IsHomeGame ? ourName : oppName;
        lblOpponent.Text = match.IsHomeGame ? oppName : ourName;

        // Column headers show who is who regardless of home/away position
        colOurs.HeaderText     = ourName;
        colOpponent.HeaderText = oppName;

        var ourRatings = PlayerService.CalculateTeamRatings(_state.Squad);

        bool isCup = match.MatchType is MatchType.LeagueCup or MatchType.FACup;
        var oppRatings = OpponentRatingService.Estimate(
            match.OpponentName,
            _state.CurrentLeague,
            _state.Club.Division,
            _state.DifficultyLevel,
            cupRound: isCup ? (int)_state.LeagueCup.CurrentRound : 0,
            isCupMatch: isCup,
            _rng);

        AddRatingRow("Goalkeeper", ourRatings.GoalkeeperRating, oppRatings.GoalkeeperRating);
        AddRatingRow("Defence",    ourRatings.DefenceRating,    oppRatings.DefenceRating);
        AddRatingRow("Midfield",   ourRatings.MidRating,        oppRatings.MidRating);
        AddRatingRow("Attack",     ourRatings.AttackRating,     oppRatings.AttackRating);
    }

    private void AddRatingRow(string label, int ourRating, int oppRating)
    {
        int idx = dgvRatings.Rows.Add(label, ourRating, oppRating);
        var row = dgvRatings.Rows[idx];

        Color strongBg = Color.FromArgb(220, 252, 231); // green-100
        Color strongFg = Color.FromArgb(21, 128, 61);   // green-700
        Color weakBg   = Color.FromArgb(254, 226, 226); // red-100
        Color weakFg   = Color.FromArgb(185, 28, 28);   // red-700

        if (ourRating > oppRating)
        {
            StyleCell(row.Cells[colOurs.Index],     strongBg, strongFg);
            StyleCell(row.Cells[colOpponent.Index], weakBg,   weakFg);
        }
        else if (oppRating > ourRating)
        {
            StyleCell(row.Cells[colOurs.Index],     weakBg,   weakFg);
            StyleCell(row.Cells[colOpponent.Index], strongBg, strongFg);
        }
    }

    private static void StyleCell(DataGridViewCell cell, Color back, Color fore)
    {
        cell.Style.BackColor          = back;
        cell.Style.ForeColor          = fore;
        cell.Style.SelectionBackColor = back;
        cell.Style.SelectionForeColor = fore;
    }

    private static string MatchTypeLabel(MatchType type) => type switch
    {
        MatchType.League            => "League",
        MatchType.LeagueCup         => "Lg Cup",
        MatchType.FACup             => "FA Cup",
        MatchType.EuropeanFirstLeg  => "Euro (1)",
        MatchType.EuropeanSecondLeg => "Euro (2)",
        MatchType.EuropeanFriendly  => "Euro Fr.",
        MatchType.Replay            => "Replay",
        _                           => type.ToString()
    };
}
