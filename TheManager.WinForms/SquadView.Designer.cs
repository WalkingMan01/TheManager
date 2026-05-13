namespace TheManager.WinForms;

partial class SquadView
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        dgvSquad  = new System.Windows.Forms.DataGridView();
        colSelect = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        colPos    = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colName   = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colSkill  = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colAge    = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colTemper = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colGames  = new System.Windows.Forms.DataGridViewTextBoxColumn();

        ((System.ComponentModel.ISupportInitialize)dgvSquad).BeginInit();
        SuspendLayout();

        // DataGridView columns
        colSelect.HeaderText = string.Empty;
        colSelect.Name       = "colSelect";
        colSelect.ReadOnly   = false;
        colSelect.Width      = 36;
        colSelect.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        colPos.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colPos.HeaderText = "Pos";
        colPos.Name       = "colPos";
        colPos.ReadOnly   = true;
        colPos.Width      = 64;
        colPos.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        colName.HeaderText   = "Name";
        colName.MinimumWidth = 120;
        colName.Name         = "colName";
        colName.ReadOnly     = true;
        colName.SortMode     = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        colSkill.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colSkill.HeaderText = "Skill";
        colSkill.Name       = "colSkill";
        colSkill.ReadOnly   = true;
        colSkill.Width      = 80;
        colSkill.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        colAge.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colAge.HeaderText = "Age";
        colAge.Name       = "colAge";
        colAge.ReadOnly   = true;
        colAge.Width      = 72;
        colAge.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        colTemper.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colTemper.HeaderText = "Temper";
        colTemper.Name       = "colTemper";
        colTemper.ReadOnly   = true;
        colTemper.Width      = 80;
        colTemper.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        colGames.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colGames.HeaderText = "Games";
        colGames.Name       = "colGames";
        colGames.ReadOnly   = true;
        colGames.Width      = 80;
        colGames.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        // dgvSquad
        dgvSquad.AllowUserToAddRows    = false;
        dgvSquad.AllowUserToDeleteRows = false;
        dgvSquad.AllowUserToResizeRows = false;
        dgvSquad.BackgroundColor       = System.Drawing.Color.White;
        dgvSquad.BorderStyle           = System.Windows.Forms.BorderStyle.None;
        dgvSquad.CellBorderStyle       = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
        dgvSquad.GridColor             = System.Drawing.Color.FromArgb(226, 232, 240);     // slate-200
        dgvSquad.EnableHeadersVisualStyles   = false;
        dgvSquad.ColumnHeadersHeight         = 34;
        dgvSquad.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvSquad.ColumnHeadersDefaultCellStyle.BackColor          = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        dgvSquad.ColumnHeadersDefaultCellStyle.ForeColor          = System.Drawing.Color.FromArgb(148, 163, 184); // slate-400
        dgvSquad.ColumnHeadersDefaultCellStyle.Font               = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        dgvSquad.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(248, 250, 252);
        dgvSquad.ColumnHeadersDefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(148, 163, 184);
        dgvSquad.DefaultCellStyle.BackColor          = System.Drawing.Color.White;
        dgvSquad.DefaultCellStyle.ForeColor          = System.Drawing.Color.FromArgb(51, 65, 85);    // slate-700
        dgvSquad.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        dgvSquad.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(15, 23, 42);    // slate-900
        dgvSquad.DefaultCellStyle.Font               = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        dgvSquad.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        dgvSquad.RowTemplate.Height = 32;
        dgvSquad.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
        {
            colSelect, colPos, colName, colSkill, colAge, colTemper, colGames
        });
        dgvSquad.Dock              = System.Windows.Forms.DockStyle.Fill;
        dgvSquad.MultiSelect       = false;
        dgvSquad.Name              = "dgvSquad";
        dgvSquad.ReadOnly          = false;
        dgvSquad.RowHeadersVisible = false;
        dgvSquad.SelectionMode     = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        dgvSquad.TabIndex          = 0;

        // ── SquadView ─────────────────────────────────────────────────────────────
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        Controls.Add(dgvSquad);
        Name = "SquadView";
        Size = new System.Drawing.Size(820, 608);

        ((System.ComponentModel.ISupportInitialize)dgvSquad).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.DataGridView               dgvSquad;
    private System.Windows.Forms.DataGridViewCheckBoxColumn colSelect;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colPos;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colSkill;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colAge;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colTemper;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colGames;
}
