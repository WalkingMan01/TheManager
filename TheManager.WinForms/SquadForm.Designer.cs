namespace TheManager.WinForms;

partial class SquadForm
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

        pnlHeader      = new System.Windows.Forms.Panel();
        lblClubName    = new System.Windows.Forms.Label();
        lblManagerName = new System.Windows.Forms.Label();
        lblDivision    = new System.Windows.Forms.Label();
        lblMorale      = new System.Windows.Forms.Label();
        lblWeek        = new System.Windows.Forms.Label();
        dgvSquad       = new System.Windows.Forms.DataGridView();
        colPos         = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colName        = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colSkill       = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colAge         = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colTemper      = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colGames       = new System.Windows.Forms.DataGridViewTextBoxColumn();

        pnlHeader.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvSquad).BeginInit();
        SuspendLayout();

        // ── pnlHeader ────────────────────────────────────────────────────────────
        pnlHeader.BackColor = System.Drawing.Color.FromArgb(45, 55, 72);
        pnlHeader.Controls.Add(lblWeek);
        pnlHeader.Controls.Add(lblMorale);
        pnlHeader.Controls.Add(lblDivision);
        pnlHeader.Controls.Add(lblManagerName);
        pnlHeader.Controls.Add(lblClubName);
        pnlHeader.Dock     = System.Windows.Forms.DockStyle.Top;
        pnlHeader.Location = new System.Drawing.Point(0, 0);
        pnlHeader.Name     = "pnlHeader";
        pnlHeader.Size     = new System.Drawing.Size(820, 96);
        pnlHeader.TabIndex = 0;

        // lblClubName
        lblClubName.AutoSize  = false;
        lblClubName.Font      = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblClubName.ForeColor = System.Drawing.Color.White;
        lblClubName.Location  = new System.Drawing.Point(20, 14);
        lblClubName.Name      = "lblClubName";
        lblClubName.Size      = new System.Drawing.Size(420, 40);
        lblClubName.TabIndex  = 0;
        lblClubName.Text      = "Club Name";

        // lblManagerName
        lblManagerName.AutoSize  = false;
        lblManagerName.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblManagerName.ForeColor = System.Drawing.Color.FromArgb(160, 174, 192);
        lblManagerName.Location  = new System.Drawing.Point(22, 60);
        lblManagerName.Name      = "lblManagerName";
        lblManagerName.Size      = new System.Drawing.Size(260, 18);
        lblManagerName.TabIndex  = 1;
        lblManagerName.Text      = "Manager: —";

        // lblDivision
        lblDivision.AutoSize  = false;
        lblDivision.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblDivision.ForeColor = System.Drawing.Color.FromArgb(160, 174, 192);
        lblDivision.Location  = new System.Drawing.Point(580, 18);
        lblDivision.Name      = "lblDivision";
        lblDivision.Size      = new System.Drawing.Size(220, 18);
        lblDivision.TabIndex  = 2;
        lblDivision.Text      = "Division —";
        lblDivision.TextAlign = System.Drawing.ContentAlignment.TopRight;

        // lblMorale
        lblMorale.AutoSize  = false;
        lblMorale.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblMorale.ForeColor = System.Drawing.Color.FromArgb(160, 174, 192);
        lblMorale.Location  = new System.Drawing.Point(580, 42);
        lblMorale.Name      = "lblMorale";
        lblMorale.Size      = new System.Drawing.Size(220, 18);
        lblMorale.TabIndex  = 3;
        lblMorale.Text      = "Morale: —";
        lblMorale.TextAlign = System.Drawing.ContentAlignment.TopRight;

        // lblWeek
        lblWeek.AutoSize  = false;
        lblWeek.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblWeek.ForeColor = System.Drawing.Color.FromArgb(160, 174, 192);
        lblWeek.Location  = new System.Drawing.Point(580, 66);
        lblWeek.Name      = "lblWeek";
        lblWeek.Size      = new System.Drawing.Size(220, 18);
        lblWeek.TabIndex  = 4;
        lblWeek.Text      = "Week: —";
        lblWeek.TextAlign = System.Drawing.ContentAlignment.TopRight;

        // ── DataGridView columns ──────────────────────────────────────────────────

        // colPos
        colPos.HeaderText = "Pos";
        colPos.Name       = "colPos";
        colPos.ReadOnly   = true;
        colPos.Width      = 64;
        colPos.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
        colPos.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;

        // colName
        colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        colName.HeaderText   = "Name";
        colName.MinimumWidth = 140;
        colName.Name         = "colName";
        colName.ReadOnly     = true;
        colName.SortMode     = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        // colSkill
        colSkill.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colSkill.HeaderText = "Skill";
        colSkill.Name       = "colSkill";
        colSkill.ReadOnly   = true;
        colSkill.Width      = 80;
        colSkill.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        // colAge
        colAge.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colAge.HeaderText = "Age";
        colAge.Name       = "colAge";
        colAge.ReadOnly   = true;
        colAge.Width      = 72;
        colAge.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        // colTemper
        colTemper.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colTemper.HeaderText = "Temper";
        colTemper.Name       = "colTemper";
        colTemper.ReadOnly   = true;
        colTemper.Width      = 80;
        colTemper.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        // colGames
        colGames.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colGames.HeaderText = "Games";
        colGames.Name       = "colGames";
        colGames.ReadOnly   = true;
        colGames.Width      = 80;
        colGames.SortMode   = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

        // ── dgvSquad ─────────────────────────────────────────────────────────────
        dgvSquad.AllowUserToAddRows    = false;
        dgvSquad.AllowUserToDeleteRows = false;
        dgvSquad.AllowUserToResizeRows = false;
        dgvSquad.BackgroundColor       = System.Drawing.Color.FromArgb(248, 249, 251);
        dgvSquad.BorderStyle           = System.Windows.Forms.BorderStyle.None;
        dgvSquad.CellBorderStyle       = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
        dgvSquad.GridColor             = System.Drawing.Color.FromArgb(218, 222, 228);
        dgvSquad.EnableHeadersVisualStyles = false;

        dgvSquad.ColumnHeadersHeight          = 34;
        dgvSquad.ColumnHeadersHeightSizeMode  = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvSquad.ColumnHeadersDefaultCellStyle.BackColor  = System.Drawing.Color.FromArgb(45, 55, 72);
        dgvSquad.ColumnHeadersDefaultCellStyle.ForeColor  = System.Drawing.Color.FromArgb(226, 232, 240);
        dgvSquad.ColumnHeadersDefaultCellStyle.Font       = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        dgvSquad.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(45, 55, 72);

        dgvSquad.DefaultCellStyle.BackColor         = System.Drawing.Color.White;
        dgvSquad.DefaultCellStyle.ForeColor         = System.Drawing.Color.FromArgb(45, 55, 72);
        dgvSquad.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(190, 227, 248);
        dgvSquad.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(45, 55, 72);
        dgvSquad.DefaultCellStyle.Font              = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);

        dgvSquad.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(247, 250, 252);

        dgvSquad.RowTemplate.Height = 28;
        dgvSquad.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
        {
            colPos, colName, colSkill, colAge, colTemper, colGames
        });
        dgvSquad.Dock            = System.Windows.Forms.DockStyle.Fill;
        dgvSquad.MultiSelect     = false;
        dgvSquad.Name            = "dgvSquad";
        dgvSquad.ReadOnly        = true;
        dgvSquad.RowHeadersVisible = false;
        dgvSquad.SelectionMode   = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        dgvSquad.TabIndex        = 1;

        // ── SquadForm ────────────────────────────────────────────────────────────
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        BackColor           = System.Drawing.Color.FromArgb(248, 249, 251);
        ClientSize          = new System.Drawing.Size(820, 660);
        Controls.Add(dgvSquad);
        Controls.Add(pnlHeader);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Name            = "SquadForm";
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text            = "The Manager — Squad";

        pnlHeader.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvSquad).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.Panel pnlHeader;
    private System.Windows.Forms.Label lblClubName;
    private System.Windows.Forms.Label lblManagerName;
    private System.Windows.Forms.Label lblDivision;
    private System.Windows.Forms.Label lblMorale;
    private System.Windows.Forms.Label lblWeek;
    private System.Windows.Forms.DataGridView dgvSquad;
    private System.Windows.Forms.DataGridViewTextBoxColumn colPos;
    private System.Windows.Forms.DataGridViewTextBoxColumn colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn colSkill;
    private System.Windows.Forms.DataGridViewTextBoxColumn colAge;
    private System.Windows.Forms.DataGridViewTextBoxColumn colTemper;
    private System.Windows.Forms.DataGridViewTextBoxColumn colGames;
}
