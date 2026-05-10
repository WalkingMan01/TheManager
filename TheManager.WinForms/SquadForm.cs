using TheManager.Models;

namespace TheManager.WinForms;

/// <summary>Main game form. Houses a left-hand nav pane and a swappable content area.</summary>
public partial class SquadForm : Form
{
    private readonly GameState _state;

    /// <summary>Initialises the form with the provided game state.</summary>
    public SquadForm(GameState state)
    {
        _state = state;
        InitializeComponent();
        Populate();
    }

    // ── Header population ─────────────────────────────────────────────────────

    private void Populate()
    {
        lblClubName.Text    = string.IsNullOrEmpty(_state.Club.Name)          ? "New Club"   : _state.Club.Name;
        lblManagerName.Text = $"Manager: {(string.IsNullOrEmpty(_state.Club.ManagerName) ? "—" : _state.Club.ManagerName)}";
        lblDivision.Text    = _state.Club.Division == 0                        ? "Division —" : $"Division {(int)_state.Club.Division}";
        lblMorale.Text      = $"Morale: {(_state.Club.TeamMorale  == 0        ? "—"          : _state.Club.TeamMorale.ToString())}";
        lblWeek.Text        = $"Week: {(_state.CurrentWeek        == 0        ? "—"          : _state.CurrentWeek.ToString())}";

        PopulateSquad();
    }

    // ── Squad view ────────────────────────────────────────────────────────────

    private void PopulateSquad()
    {
        dgvSquad.Rows.Clear();

        AddSectionHeader("FIRST TEAM");
        for (int slot = 1; slot <= 11; slot++)
            AddPlayerRow(slot, SlotLabel(slot));

        AddSectionHeader("SUBSTITUTE");
        AddPlayerRow(12, "SUB");

        AddSectionHeader("RESERVES");
        for (int slot = 13; slot <= 20; slot++)
            AddPlayerRow(slot, "RES");
    }

    private static readonly Color SectionBackColor       = Color.FromArgb(74, 85, 104);
    private static readonly Color SectionForeColor       = Color.White;
    private static readonly Font  SectionFont            = new("Segoe UI", 8F, FontStyle.Bold);

    private void AddSectionHeader(string title)
    {
        int idx = dgvSquad.Rows.Add(false, title, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        var row = dgvSquad.Rows[idx];
        row.DefaultCellStyle.BackColor          = SectionBackColor;
        row.DefaultCellStyle.ForeColor          = SectionForeColor;
        row.DefaultCellStyle.Font               = SectionFont;
        row.DefaultCellStyle.SelectionBackColor = SectionBackColor;
        row.DefaultCellStyle.SelectionForeColor = SectionForeColor;
        row.Cells[colSelect.Index].ReadOnly     = true;
        row.ReadOnly = true;
        row.Tag      = "header";
    }

    private void AddPlayerRow(int slot, string posLabel)
    {
        var player = _state.Squad[slot];
        int idx = dgvSquad.Rows.Add(
            false,
            posLabel,
            player?.Name        ?? "—",
            player != null ? player.DisplaySkill.ToString() : "—",
            player != null ? player.DisplayAge.ToString()   : "—",
            player != null ? player.Temper.ToString()       : "—",
            player != null ? player.GamesPlayed.ToString()  : "—"
        );
        dgvSquad.Rows[idx].Tag = slot;
    }

    private static string SlotLabel(int slot) => slot switch
    {
        1                => "GK",
        2 or 3 or 4 or 5 => "DEF",
        6 or 7 or 8      => "MID",
        9 or 10 or 11    => "ATK",
        _                => "—"
    };

    // ── Navigation ────────────────────────────────────────────────────────────

    private static readonly Color NavNormalBg = Color.FromArgb(30, 42, 58);
    private static readonly Color NavNormalFg = Color.FromArgb(160, 174, 192);
    private static readonly Color NavActiveBg = Color.FromArgb(45, 64, 89);

    private void btnNavSquad_Click(object sender, EventArgs e)     => SwitchView(pnlSquadView,    btnNavSquad);
    private void btnNavPlayMatch_Click(object sender, EventArgs e) => SwitchView(pnlPlayMatchView, btnNavPlayMatch);
    private void btnNavFixtures_Click(object sender, EventArgs e)  => SwitchView(pnlFixturesView,  btnNavFixtures);

    private void SwitchView(Panel target, Button activeBtn)
    {
        pnlSquadView.Visible     = target == pnlSquadView;
        pnlPlayMatchView.Visible = target == pnlPlayMatchView;
        pnlFixturesView.Visible  = target == pnlFixturesView;

        foreach (var btn in (Button[])[btnNavPlayMatch, btnNavSquad, btnNavFixtures])
        {
            btn.BackColor = NavNormalBg;
            btn.ForeColor = NavNormalFg;
        }

        activeBtn.BackColor    = NavActiveBg;
        activeBtn.ForeColor    = Color.White;
        pnlNavIndicator.Top    = activeBtn.Top;
    }
}
