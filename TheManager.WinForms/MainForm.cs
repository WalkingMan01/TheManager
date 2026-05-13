using TheManager.Models;

namespace TheManager.WinForms;

/// <summary>Main application shell. Hosts the top nav bar and swaps UserControl views into the content area.</summary>
public partial class MainForm : Form
{
    private GameState?     _state;
    private SquadView?     _squadView;
    private readonly HomeView      _homeView;
    private readonly PlayMatchView _playMatchView;
    private readonly FixturesView  _fixturesView;

    /// <summary>Initialises the main form and shows the home view.</summary>
    public MainForm()
    {
        InitializeComponent();

        _homeView      = new HomeView()      { Dock = DockStyle.Fill };
        _playMatchView = new PlayMatchView() { Dock = DockStyle.Fill };
        _fixturesView  = new FixturesView()  { Dock = DockStyle.Fill };

        _homeView.NewGameRequested      += (_, _) => StartGame(new GameState());
        _homeView.ContinueGameRequested += (_, _) => StartGame(new GameState()); // TODO: load from save

        pnlContent.Controls.Add(_fixturesView);
        pnlContent.Controls.Add(_playMatchView);
        pnlContent.Controls.Add(_homeView);

        SetNavButtonsVisible(false);
        SwitchToView(_homeView, null);
    }

    // ── Game startup ──────────────────────────────────────────────────────────

    private void StartGame(GameState state)
    {
        _state     = state;
        _squadView = new SquadView(state) { Dock = DockStyle.Fill };
        pnlContent.Controls.Add(_squadView);

        PopulateHeader();
        SetNavButtonsVisible(true);
        SwitchToView(_squadView, btnNavSquad);
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private void PopulateHeader()
    {
        if (_state is null) return;
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

    private void btnNavSquad_Click(object sender, EventArgs e)     => SwitchToView(_squadView!,    btnNavSquad);
    private void btnNavPlayMatch_Click(object sender, EventArgs e) => SwitchToView(_playMatchView, btnNavPlayMatch);
    private void btnNavFixtures_Click(object sender, EventArgs e)  => SwitchToView(_fixturesView,  btnNavFixtures);

    private void SetNavButtonsVisible(bool visible)
    {
        btnNavSquad.Visible     = visible;
        btnNavPlayMatch.Visible = visible;
        btnNavFixtures.Visible  = visible;
    }

    private void SwitchToView(Control target, Button? activeBtn)
    {
        _homeView.Visible      = target == _homeView;
        _playMatchView.Visible = target == _playMatchView;
        _fixturesView.Visible  = target == _fixturesView;
        if (_squadView != null)
            _squadView.Visible = target == _squadView;

        foreach (var btn in (Button[])[btnNavPlayMatch, btnNavSquad, btnNavFixtures])
        {
            btn.BackColor = NavNormalBg;
            btn.ForeColor = NavNormalFg;
            btn.Font      = new Font("Segoe UI", 9F, FontStyle.Regular);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 250, 252);
        }

        if (activeBtn is not null)
        {
            activeBtn.BackColor = NavActiveBg;
            activeBtn.ForeColor = NavActiveFg;
            activeBtn.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            activeBtn.FlatAppearance.MouseOverBackColor = NavActiveBg;
        }
    }
}
