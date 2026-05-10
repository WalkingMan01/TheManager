using TheManager.Models;

namespace TheManager.WinForms;

/// <summary>Main game form. Houses a top nav bar and a swappable content area.</summary>
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
        lblClubName.Text = string.IsNullOrEmpty(_state.Club.Name) ? "New Club" : _state.Club.Name;

        var div  = _state.Club.Division == 0 ? "Div —"    : $"Div {(int)_state.Club.Division}";
        var week = _state.CurrentWeek    == 0 ? "Week —"   : $"Week {_state.CurrentWeek}";
        lblDivision.Text = $"{div} · {week}";

        PopulateSquad();
    }

    // ── Squad view ────────────────────────────────────────────────────────────

    private void PopulateSquad()
    {
        dgvSquad.Rows.Clear();

        AddSectionHeader("FIRST TEAM", Color.FromArgb(236, 253, 245), Color.FromArgb(4, 120, 87));   // emerald-50 / emerald-700
        for (int slot = 1; slot <= 11; slot++)
            AddPlayerRow(slot, SlotLabel(slot));

        AddSectionHeader("SUBSTITUTE", Color.FromArgb(240, 249, 255), Color.FromArgb(3, 105, 161));  // sky-50 / sky-700
        AddPlayerRow(12, "SUB");

        AddSectionHeader("RESERVES",   Color.FromArgb(241, 245, 249), Color.FromArgb(100, 116, 139)); // slate-100 / slate-500
        for (int slot = 13; slot <= 20; slot++)
            AddPlayerRow(slot, "RES");
    }

    private static readonly Font SectionFont = new("Segoe UI", 7.5F, FontStyle.Bold);

    private void AddSectionHeader(string title, Color backColor, Color foreColor)
    {
        int idx = dgvSquad.Rows.Add(false, title, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        var row = dgvSquad.Rows[idx];
        row.DefaultCellStyle.BackColor          = backColor;
        row.DefaultCellStyle.ForeColor          = foreColor;
        row.DefaultCellStyle.Font               = SectionFont;
        row.DefaultCellStyle.SelectionBackColor = backColor;
        row.DefaultCellStyle.SelectionForeColor = foreColor;
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

    private static readonly Color NavNormalBg = Color.White;
    private static readonly Color NavNormalFg = Color.FromArgb(100, 116, 139); // slate-500
    private static readonly Color NavActiveBg = Color.FromArgb(241, 245, 249); // slate-100
    private static readonly Color NavActiveFg = Color.FromArgb(15, 23, 42);    // slate-900

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
            btn.Font      = new Font("Segoe UI", 9F, FontStyle.Regular);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 250, 252);
        }

        activeBtn.BackColor = NavActiveBg;
        activeBtn.ForeColor = NavActiveFg;
        activeBtn.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        activeBtn.FlatAppearance.MouseOverBackColor = NavActiveBg;
    }
}
