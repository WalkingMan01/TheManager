using TheManager.Models;
using MatchType = TheManager.Models.MatchType;

namespace TheManager.WinForms;

/// <summary>UserControl that displays the full season fixture list ordered by week.</summary>
public partial class FixturesView : UserControl
{
    private readonly GameState _state;

    /// <summary>Initialises the fixtures view with the provided game state.</summary>
    public FixturesView(GameState state)
    {
        _state = state;
        InitializeComponent();
        Populate();
    }

    // ── Fixture population ────────────────────────────────────────────────────

    private void Populate()
    {
        dgvFixtures.Rows.Clear();

        foreach (var match in _state.Fixtures.OrderBy(f => f.Week))
        {
            int idx = dgvFixtures.Rows.Add(
                match.Week,
                match.IsHomeGame ? "H" : "A",
                match.OpponentName,
                MatchTypeLabel(match.MatchType));

            ApplyRowStyle(dgvFixtures.Rows[idx], match.Week);
        }
    }

    private void ApplyRowStyle(DataGridViewRow row, int week)
    {
        if (week < _state.CurrentWeek)
        {
            row.DefaultCellStyle.ForeColor          = Color.FromArgb(148, 163, 184); // slate-400
            row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(148, 163, 184);
        }
        else if (week == _state.CurrentWeek)
        {
            row.DefaultCellStyle.BackColor          = Color.FromArgb(254, 243, 199); // amber-100
            row.DefaultCellStyle.ForeColor          = Color.FromArgb(120, 53, 15);   // amber-900
            row.DefaultCellStyle.Font               = new Font("Segoe UI", 9F, FontStyle.Bold);
            row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 243, 199);
            row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(120, 53, 15);
        }
    }

    private static string MatchTypeLabel(MatchType type) => type switch
    {
        MatchType.League             => "League",
        MatchType.LeagueCup          => "Lg Cup",
        MatchType.FACup              => "FA Cup",
        MatchType.EuropeanFirstLeg   => "Euro (1)",
        MatchType.EuropeanSecondLeg  => "Euro (2)",
        MatchType.EuropeanFriendly   => "Euro Fr.",
        MatchType.Replay             => "Replay",
        MatchType.EndOfSeason        => "End",
        _                            => type.ToString()
    };
}
