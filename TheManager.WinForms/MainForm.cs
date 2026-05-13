using TheManager.Models;

namespace TheManager.WinForms;

/// <summary>Main application shell. Hosts the top nav bar and swaps UserControl views into the content area.</summary>
public partial class MainForm : Form
{
    private readonly GameState     _state;
    private readonly SquadView     _squadView;
    private readonly PlayMatchView _playMatchView;
    private readonly FixturesView  _fixturesView;

    /// <summary>Initialises the main form with the provided game state.</summary>
    public MainForm(GameState state)
    {
        _state = state;
        InitializeComponent();

        _squadView     = new SquadView(state)     { Dock = DockStyle.Fill };
        _playMatchView = new PlayMatchView()       { Dock = DockStyle.Fill };
        _fixturesView  = new FixturesView()        { Dock = DockStyle.Fill };

        pnlContent.Controls.Add(_fixturesView);
        pnlContent.Controls.Add(_playMatchView);
        pnlContent.Controls.Add(_squadView);

        PopulateHeader();
        SwitchView(_squadView, btnNavSquad);
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private void PopulateHeader()
    {
        lblClubName.Text = string.IsNullOrEmpty(_state.Club.Name) ? "New Club" : _state.Club.Name;

        var div  = _state.Club.Division == 0 ? "Div —"  : $"Div {(int)_state.Club.Division}";
        var week = _state.CurrentWeek    == 0 ? "Week —" : $"Week {_state.CurrentWeek}";
        lblDivision.Text = $"{div} · {week}";
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private static readonly Color NavNormalBg = Color.White;
    private static readonly Color NavNormalFg = Color.FromArgb(100, 116, 139); // slate-500
    private static readonly Color NavActiveBg = Color.FromArgb(241, 245, 249); // slate-100
    private static readonly Color NavActiveFg = Color.FromArgb(15, 23, 42);    // slate-900

    private void btnNavSquad_Click(object sender, EventArgs e)     => SwitchView(_squadView,     btnNavSquad);
    private void btnNavPlayMatch_Click(object sender, EventArgs e) => SwitchView(_playMatchView, btnNavPlayMatch);
    private void btnNavFixtures_Click(object sender, EventArgs e)  => SwitchView(_fixturesView,  btnNavFixtures);

    private void SwitchView(Control target, Button activeBtn)
    {
        _squadView.Visible     = target == _squadView;
        _playMatchView.Visible = target == _playMatchView;
        _fixturesView.Visible  = target == _fixturesView;

        foreach (var btn in (Button[])[btnNavPlayMatch, btnNavSquad, btnNavFixtures])
        {
            btn.BackColor = NavNormalBg;
            btn.ForeColor = NavNormalFg;
            btn.Font      = new Font("Segoe UI", 9F, FontStyle.Regular);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 250, 252);
        }

        activeBtn.BackColor = NavActiveBg;
        activeBtn.ForeColor = NavActiveFg;
        activeBtn.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        activeBtn.FlatAppearance.MouseOverBackColor = NavActiveBg;
    }
}
