using TheManager.Models;

namespace TheManager.WinForms;

/// <summary>Main squad view showing all player slots for the managed club.</summary>
public partial class SquadForm : Form
{
    private readonly GameState _state;

    /// <summary>Initialises the squad form with the provided game state.</summary>
    public SquadForm(GameState state)
    {
        _state = state;
        InitializeComponent();
        Populate();
    }

    private void Populate()
    {
        lblClubName.Text    = string.IsNullOrEmpty(_state.Club.Name)          ? "New Club"   : _state.Club.Name;
        lblManagerName.Text = $"Manager: {(string.IsNullOrEmpty(_state.Club.ManagerName) ? "—" : _state.Club.ManagerName)}";
        lblDivision.Text    = _state.Club.Division == 0                        ? "Division —" : $"Division {(int)_state.Club.Division}";
        lblMorale.Text      = $"Morale: {(_state.Club.TeamMorale == 0         ? "—"          : _state.Club.TeamMorale.ToString())}";
        lblWeek.Text        = $"Week: {(_state.CurrentWeek == 0               ? "—"          : _state.CurrentWeek.ToString())}";

        PopulateSquad();
    }

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

    private static readonly Color SectionBackColor      = Color.FromArgb(74, 85, 104);
    private static readonly Color SectionForeColor      = Color.White;
    private static readonly Font  SectionFont           = new("Segoe UI", 8F, FontStyle.Bold);

    private void AddSectionHeader(string title)
    {
        int idx = dgvSquad.Rows.Add(title, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        var row = dgvSquad.Rows[idx];
        row.DefaultCellStyle.BackColor          = SectionBackColor;
        row.DefaultCellStyle.ForeColor          = SectionForeColor;
        row.DefaultCellStyle.Font               = SectionFont;
        row.DefaultCellStyle.SelectionBackColor = SectionBackColor;
        row.DefaultCellStyle.SelectionForeColor = SectionForeColor;
        row.ReadOnly = true;
        row.Tag      = "header";
    }

    private void AddPlayerRow(int slot, string posLabel)
    {
        var player = _state.Squad[slot];
        int idx = dgvSquad.Rows.Add(
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
}
