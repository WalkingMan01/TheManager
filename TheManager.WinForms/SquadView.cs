using TheManager.Models;

namespace TheManager.WinForms;

/// <summary>UserControl that displays the full squad in a DataGridView.</summary>
public partial class SquadView : UserControl
{
    private readonly GameState _state;

    /// <summary>Initialises the squad view with the provided game state.</summary>
    public SquadView(GameState state)
    {
        _state = state;
        InitializeComponent();
        Populate();
    }

    // ── Squad population ──────────────────────────────────────────────────────

    private void Populate()
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
}
