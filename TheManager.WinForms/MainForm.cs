using TheManager.Models;
using TheManager.Services;

namespace TheManager.WinForms;

/// <summary>Main application shell. Hosts the top nav bar and swaps UserControl views into the content area.</summary>
public partial class MainForm : Form
{
    //private GameState?     _state;
    private SquadView?      _squadView;
    private FixturesView?   _fixturesView;
    private CheckMatchView? _checkMatchView;
    private PlayMatchView?  _playMatchView;
    private readonly HomeView _homeView;

    private GameService _gameService;

    /// <summary>Initialises the main form and shows the home view.</summary>
    public MainForm()
    {
        InitializeComponent();

        _homeView = new HomeView() { Dock = DockStyle.Fill };

        _homeView.NewGameRequested      += (_, _) => StartGame(new GameState());
        _homeView.ContinueGameRequested += (_, _) => StartGame(new GameState()); // TODO: load from save

        pnlContent.Controls.Add(_homeView);

        SetNavButtonsVisible(false);
        SwitchToView(_homeView, null);
    }

    // ── Game startup ──────────────────────────────────────────────────────────

    private void StartGame(GameState state)
    {
        // ToDo: Need to resolve how to pick team and manager
        _gameService = new GameService()
        {
            Manager = "Steve",
            Team = "BURNLEY"
        };
        _gameService.StartGame();
        
        _squadView      = new SquadView(_gameService.State)                        { Dock = DockStyle.Fill };
        _fixturesView   = new FixturesView(_gameService.State)                      { Dock = DockStyle.Fill };
        _checkMatchView = new CheckMatchView(_gameService.State, _gameService.Rng)  { Dock = DockStyle.Fill };
        _playMatchView  = new PlayMatchView(_gameService)                           { Dock = DockStyle.Fill };
        pnlContent.Controls.Add(_squadView);
        pnlContent.Controls.Add(_fixturesView);
        pnlContent.Controls.Add(_checkMatchView);
        pnlContent.Controls.Add(_playMatchView);


        PopulateHeader();
        SetNavButtonsVisible(true);
        SwitchToView(_squadView, btnNavSquad);
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private void PopulateHeader()
    {
        if (_gameService.State is null) return;

        lblClubName.Text = string.IsNullOrEmpty(_gameService.State.Club.Name) ? "New Club" : _gameService.State.Club.Name;
        var div  = _gameService.State.Club.Division == 0 ? "Div —"  : $"Div {(int)_gameService.State.Club.Division}";
        var week = _gameService.State.CurrentWeek    == 0 ? "Week —" : $"Week {_gameService.State.CurrentWeek}";
        lblDivision.Text = $"{div} · {week}";
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private static readonly Color NavNormalBg = Color.White;
    private static readonly Color NavNormalFg = Color.FromArgb(100, 116, 139); // slate-500
    private static readonly Color NavActiveBg = Color.FromArgb(241, 245, 249); // slate-100
    private static readonly Color NavActiveFg = Color.FromArgb(15, 23, 42);    // slate-900

    private void btnNavSquad_Click(object sender, EventArgs e)      => SwitchToView(_squadView!,     btnNavSquad);
    private void btnNavPlayMatch_Click(object sender, EventArgs e)  => SwitchToView(_playMatchView!,  btnNavPlayMatch);
    private void btnNavFixtures_Click(object sender, EventArgs e)   => SwitchToView(_fixturesView!,  btnNavFixtures);
    private void btnNavCheckMatch_Click(object sender, EventArgs e) => SwitchToView(_checkMatchView!, btnNavCheckMatch);

    private void SetNavButtonsVisible(bool visible)
    {
        btnNavSquad.Visible      = visible;
        btnNavPlayMatch.Visible  = visible;
        btnNavFixtures.Visible   = visible;
        btnNavCheckMatch.Visible = visible;
    }

    private void SwitchToView(Control target, Button? activeBtn)
    {
        _homeView.Visible = target == _homeView;
        if (_playMatchView  != null) _playMatchView.Visible  = target == _playMatchView;
        if (_fixturesView   != null) _fixturesView.Visible   = target == _fixturesView;
        if (_squadView      != null) _squadView.Visible      = target == _squadView;
        if (_checkMatchView != null) _checkMatchView.Visible = target == _checkMatchView;

        foreach (var btn in (Button[])[btnNavPlayMatch, btnNavSquad, btnNavFixtures, btnNavCheckMatch])
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
