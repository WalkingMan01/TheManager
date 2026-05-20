using TheManager.Models;
using TheManager.Services;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.WinForms;

/// <summary>UserControl that plays the next fixture and displays the result.</summary>
public partial class PlayMatchView : UserControl
{
    private readonly GameService _gameService;

    /// <summary>Initialises the play match view with the provided game service.</summary>
    public PlayMatchView(GameService gameService)
    {
        _gameService = gameService;
        InitializeComponent();
        PopulateMatchPreview();
    }

    // ── Pre-match preview ─────────────────────────────────────────────────────

    private void PopulateMatchPreview()
    {
        var match = FixtureSchedulerService.GetCurrentMatch(_gameService.State);

        if (match.MatchType == MatchType.EndOfSeason)
        {
            lblMatchInfo.Text    = "End of season — no further fixtures to play.";
            lblHomeTeam.Text     = string.Empty;
            lblAwayTeam.Text     = string.Empty;
            btnPlayMatch.Enabled = false;
            return;
        }

        string venueLabel = match.IsHomeGame ? "Home" : "Away";
        lblMatchInfo.Text = $"Week {match.Week}  ·  {MatchTypeLabel(match.MatchType)}  ·  {venueLabel}";

        string ourName = _gameService.State.Club.Name.Trim();
        string oppName = match.OpponentName.Trim();
        lblHomeTeam.Text = match.IsHomeGame ? ourName : oppName;
        lblAwayTeam.Text = match.IsHomeGame ? oppName : ourName;

        btnPlayMatch.Enabled = true;
    }

    // ── Play match ────────────────────────────────────────────────────────────

    private void btnPlayMatch_Click(object sender, EventArgs e)
    {
        
        btnPlayMatch.Enabled = false;

        var result = _gameService.PlayMatch();

        if (result.WasEndOfSeason)
        {
            lblMatchInfo.Text    = "End of season processed.";
            lblHomeTeam.Text     = string.Empty;
            lblAwayTeam.Text     = string.Empty;
            return;
        }

        ShowResult(result);

        // Refresh preview for the next match
        PopulateMatchPreview();
    }

    private void ShowResult(MatchResult result)
    {
        // Scores
        int homeScore = result.IsHomeGame ? result.OurScore : result.TheirScore;
        int awayScore = result.IsHomeGame ? result.TheirScore : result.OurScore;
        lblHomeScore.Text = homeScore.ToString();
        lblAwayScore.Text = awayScore.ToString();

        // Switch centre from button to score
        btnPlayMatch.Visible = false;
        pnlScore.Visible     = true;

        // Result badge
        bool weWon  = result.OurScore > result.TheirScore;
        bool weDrew = result.OurScore == result.TheirScore;
        (lblResult.Text, lblResult.ForeColor) = (weWon, weDrew) switch
        {
            (true,  _)    => ("WIN",  Color.FromArgb(21, 128, 61)),   // green-700
            (_,     true) => ("DRAW", Color.FromArgb(146, 64, 14)),   // amber-800
            _             => ("LOSS", Color.FromArgb(185, 28, 28))    // red-700
        };
        pnlSep.Visible    = true;
        lblResult.Visible = true;

        // Goals grid
        dgvGoals.Rows.Clear();
        string oppName = result.OpponentName.Trim();

        foreach (var goal in result.Goals)
        {
            string minute = $"{goal.Minute}'";
            string eventText;
            Color  rowFore;

            if (goal.IsOurGoal)
            {
                eventText = string.IsNullOrWhiteSpace(goal.Scorer)
                    ? result.OurClubName.Trim()
                    : goal.Scorer.Trim();
                rowFore = Color.FromArgb(21, 128, 61);   // green-700
            }
            else
            {
                eventText = oppName;
                rowFore   = Color.FromArgb(185, 28, 28); // red-700
            }

            int idx = dgvGoals.Rows.Add(minute, eventText);
            dgvGoals.Rows[idx].DefaultCellStyle.ForeColor          = rowFore;
            dgvGoals.Rows[idx].DefaultCellStyle.SelectionForeColor = rowFore;
        }

        if (result.Goals.Count == 0)
        {
            int idx = dgvGoals.Rows.Add("—", "No goals scored");
            dgvGoals.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184); // slate-400
        }

        dgvGoals.Visible = true;
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
