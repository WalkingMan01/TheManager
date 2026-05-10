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

        // Header
        pnlHeader      = new System.Windows.Forms.Panel();
        lblClubName    = new System.Windows.Forms.Label();
        lblManagerName = new System.Windows.Forms.Label();
        lblDivision    = new System.Windows.Forms.Label();
        lblMorale      = new System.Windows.Forms.Label();
        lblWeek        = new System.Windows.Forms.Label();

        // Nav
        pnlNav          = new System.Windows.Forms.Panel();
        pnlNavIndicator = new System.Windows.Forms.Panel();
        lblNavSection   = new System.Windows.Forms.Label();
        btnNavPlayMatch = new System.Windows.Forms.Button();
        btnNavSquad     = new System.Windows.Forms.Button();
        btnNavFixtures  = new System.Windows.Forms.Button();

        // Content shell
        pnlContent = new System.Windows.Forms.Panel();

        // Squad view
        pnlSquadView = new System.Windows.Forms.Panel();
        dgvSquad     = new System.Windows.Forms.DataGridView();
        colSelect    = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        colPos       = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colName      = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colSkill     = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colAge       = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colTemper    = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colGames     = new System.Windows.Forms.DataGridViewTextBoxColumn();

        // Placeholder views
        pnlPlayMatchView        = new System.Windows.Forms.Panel();
        lblPlayMatchPlaceholder = new System.Windows.Forms.Label();
        pnlFixturesView         = new System.Windows.Forms.Panel();
        lblFixturesPlaceholder  = new System.Windows.Forms.Label();

        pnlHeader.SuspendLayout();
        pnlNav.SuspendLayout();
        pnlContent.SuspendLayout();
        pnlSquadView.SuspendLayout();
        pnlPlayMatchView.SuspendLayout();
        pnlFixturesView.SuspendLayout();
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

        // ── pnlNav ───────────────────────────────────────────────────────────────
        pnlNav.BackColor = System.Drawing.Color.FromArgb(30, 42, 58);
        pnlNav.Controls.Add(lblNavSection);
        pnlNav.Controls.Add(btnNavPlayMatch);
        pnlNav.Controls.Add(btnNavSquad);
        pnlNav.Controls.Add(btnNavFixtures);
        pnlNav.Controls.Add(pnlNavIndicator); // added last so BringToFront works
        pnlNav.Dock     = System.Windows.Forms.DockStyle.Left;
        pnlNav.Name     = "pnlNav";
        pnlNav.Size     = new System.Drawing.Size(200, 564);
        pnlNav.TabIndex = 1;

        // lblNavSection
        lblNavSection.AutoSize  = false;
        lblNavSection.Font      = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblNavSection.ForeColor = System.Drawing.Color.FromArgb(74, 85, 104);
        lblNavSection.Location  = new System.Drawing.Point(20, 18);
        lblNavSection.Name      = "lblNavSection";
        lblNavSection.Size      = new System.Drawing.Size(160, 16);
        lblNavSection.TabIndex  = 0;
        lblNavSection.Text      = "MAIN MENU";

        // btnNavPlayMatch
        btnNavPlayMatch.BackColor                            = System.Drawing.Color.FromArgb(30, 42, 58);
        btnNavPlayMatch.FlatStyle                            = System.Windows.Forms.FlatStyle.Flat;
        btnNavPlayMatch.FlatAppearance.BorderSize            = 0;
        btnNavPlayMatch.FlatAppearance.MouseOverBackColor    = System.Drawing.Color.FromArgb(42, 58, 78);
        btnNavPlayMatch.FlatAppearance.MouseDownBackColor    = System.Drawing.Color.FromArgb(45, 64, 89);
        btnNavPlayMatch.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnNavPlayMatch.ForeColor = System.Drawing.Color.FromArgb(160, 174, 192);
        btnNavPlayMatch.Location  = new System.Drawing.Point(0, 44);
        btnNavPlayMatch.Name      = "btnNavPlayMatch";
        btnNavPlayMatch.Padding   = new System.Windows.Forms.Padding(24, 0, 0, 0);
        btnNavPlayMatch.Size      = new System.Drawing.Size(200, 48);
        btnNavPlayMatch.TabIndex  = 1;
        btnNavPlayMatch.Text      = "Play Match";
        btnNavPlayMatch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        btnNavPlayMatch.UseVisualStyleBackColor = false;
        btnNavPlayMatch.Click    += new System.EventHandler(btnNavPlayMatch_Click);

        // btnNavSquad  (active by default)
        btnNavSquad.BackColor                         = System.Drawing.Color.FromArgb(45, 64, 89);
        btnNavSquad.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnNavSquad.FlatAppearance.BorderSize         = 0;
        btnNavSquad.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(45, 64, 89);
        btnNavSquad.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(45, 64, 89);
        btnNavSquad.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnNavSquad.ForeColor = System.Drawing.Color.White;
        btnNavSquad.Location  = new System.Drawing.Point(0, 92);
        btnNavSquad.Name      = "btnNavSquad";
        btnNavSquad.Padding   = new System.Windows.Forms.Padding(24, 0, 0, 0);
        btnNavSquad.Size      = new System.Drawing.Size(200, 48);
        btnNavSquad.TabIndex  = 2;
        btnNavSquad.Text      = "Squad";
        btnNavSquad.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        btnNavSquad.UseVisualStyleBackColor = false;
        btnNavSquad.Click    += new System.EventHandler(btnNavSquad_Click);

        // btnNavFixtures
        btnNavFixtures.BackColor                            = System.Drawing.Color.FromArgb(30, 42, 58);
        btnNavFixtures.FlatStyle                            = System.Windows.Forms.FlatStyle.Flat;
        btnNavFixtures.FlatAppearance.BorderSize            = 0;
        btnNavFixtures.FlatAppearance.MouseOverBackColor    = System.Drawing.Color.FromArgb(42, 58, 78);
        btnNavFixtures.FlatAppearance.MouseDownBackColor    = System.Drawing.Color.FromArgb(45, 64, 89);
        btnNavFixtures.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnNavFixtures.ForeColor = System.Drawing.Color.FromArgb(160, 174, 192);
        btnNavFixtures.Location  = new System.Drawing.Point(0, 140);
        btnNavFixtures.Name      = "btnNavFixtures";
        btnNavFixtures.Padding   = new System.Windows.Forms.Padding(24, 0, 0, 0);
        btnNavFixtures.Size      = new System.Drawing.Size(200, 48);
        btnNavFixtures.TabIndex  = 3;
        btnNavFixtures.Text      = "Fixtures";
        btnNavFixtures.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        btnNavFixtures.UseVisualStyleBackColor = false;
        btnNavFixtures.Click    += new System.EventHandler(btnNavFixtures_Click);

        // pnlNavIndicator — slim accent bar overlaid on the active nav button
        pnlNavIndicator.BackColor = System.Drawing.Color.FromArgb(99, 179, 237);
        pnlNavIndicator.Location  = new System.Drawing.Point(0, 92); // matches btnNavSquad initially
        pnlNavIndicator.Name      = "pnlNavIndicator";
        pnlNavIndicator.Size      = new System.Drawing.Size(4, 48);
        pnlNavIndicator.TabIndex  = 4;
        pnlNavIndicator.BringToFront();

        // ── pnlContent ───────────────────────────────────────────────────────────
        pnlContent.Controls.Add(pnlFixturesView);
        pnlContent.Controls.Add(pnlPlayMatchView);
        pnlContent.Controls.Add(pnlSquadView);
        pnlContent.Dock     = System.Windows.Forms.DockStyle.Fill;
        pnlContent.Name     = "pnlContent";
        pnlContent.TabIndex = 2;

        // ── pnlSquadView ─────────────────────────────────────────────────────────
        pnlSquadView.Controls.Add(dgvSquad);
        pnlSquadView.Dock     = System.Windows.Forms.DockStyle.Fill;
        pnlSquadView.Name     = "pnlSquadView";
        pnlSquadView.TabIndex = 0;
        pnlSquadView.Visible  = true;

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

        // colSelect is intentionally ReadOnly = false — only column the user can interact with

        // dgvSquad
        dgvSquad.AllowUserToAddRows    = false;
        dgvSquad.AllowUserToDeleteRows = false;
        dgvSquad.AllowUserToResizeRows = false;
        dgvSquad.BackgroundColor       = System.Drawing.Color.FromArgb(248, 249, 251);
        dgvSquad.BorderStyle           = System.Windows.Forms.BorderStyle.None;
        dgvSquad.CellBorderStyle       = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
        dgvSquad.GridColor             = System.Drawing.Color.FromArgb(218, 222, 228);
        dgvSquad.EnableHeadersVisualStyles = false;
        dgvSquad.ColumnHeadersHeight         = 34;
        dgvSquad.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvSquad.ColumnHeadersDefaultCellStyle.BackColor          = System.Drawing.Color.FromArgb(45, 55, 72);
        dgvSquad.ColumnHeadersDefaultCellStyle.ForeColor          = System.Drawing.Color.FromArgb(226, 232, 240);
        dgvSquad.ColumnHeadersDefaultCellStyle.Font               = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        dgvSquad.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(45, 55, 72);
        dgvSquad.DefaultCellStyle.BackColor          = System.Drawing.Color.White;
        dgvSquad.DefaultCellStyle.ForeColor          = System.Drawing.Color.FromArgb(45, 55, 72);
        dgvSquad.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(190, 227, 248);
        dgvSquad.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(45, 55, 72);
        dgvSquad.DefaultCellStyle.Font               = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        dgvSquad.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(247, 250, 252);
        dgvSquad.RowTemplate.Height = 28;
        dgvSquad.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
        {
            colSelect, colPos, colName, colSkill, colAge, colTemper, colGames
        });
        dgvSquad.Dock            = System.Windows.Forms.DockStyle.Fill;
        dgvSquad.MultiSelect     = false;
        dgvSquad.Name            = "dgvSquad";
        dgvSquad.ReadOnly        = false;
        dgvSquad.RowHeadersVisible = false;
        dgvSquad.SelectionMode   = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        dgvSquad.TabIndex        = 0;

        // ── pnlPlayMatchView ─────────────────────────────────────────────────────
        pnlPlayMatchView.BackColor = System.Drawing.Color.FromArgb(248, 249, 251);
        pnlPlayMatchView.Controls.Add(lblPlayMatchPlaceholder);
        pnlPlayMatchView.Dock     = System.Windows.Forms.DockStyle.Fill;
        pnlPlayMatchView.Name     = "pnlPlayMatchView";
        pnlPlayMatchView.TabIndex = 1;
        pnlPlayMatchView.Visible  = false;

        lblPlayMatchPlaceholder.AutoSize  = false;
        lblPlayMatchPlaceholder.Dock      = System.Windows.Forms.DockStyle.Fill;
        lblPlayMatchPlaceholder.Font      = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblPlayMatchPlaceholder.ForeColor = System.Drawing.Color.FromArgb(160, 174, 192);
        lblPlayMatchPlaceholder.Name      = "lblPlayMatchPlaceholder";
        lblPlayMatchPlaceholder.TabIndex  = 0;
        lblPlayMatchPlaceholder.Text      = "Play Match — coming soon";
        lblPlayMatchPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // ── pnlFixturesView ──────────────────────────────────────────────────────
        pnlFixturesView.BackColor = System.Drawing.Color.FromArgb(248, 249, 251);
        pnlFixturesView.Controls.Add(lblFixturesPlaceholder);
        pnlFixturesView.Dock     = System.Windows.Forms.DockStyle.Fill;
        pnlFixturesView.Name     = "pnlFixturesView";
        pnlFixturesView.TabIndex = 2;
        pnlFixturesView.Visible  = false;

        lblFixturesPlaceholder.AutoSize  = false;
        lblFixturesPlaceholder.Dock      = System.Windows.Forms.DockStyle.Fill;
        lblFixturesPlaceholder.Font      = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblFixturesPlaceholder.ForeColor = System.Drawing.Color.FromArgb(160, 174, 192);
        lblFixturesPlaceholder.Name      = "lblFixturesPlaceholder";
        lblFixturesPlaceholder.TabIndex  = 0;
        lblFixturesPlaceholder.Text      = "Fixtures — coming soon";
        lblFixturesPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // ── SquadForm ────────────────────────────────────────────────────────────
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        BackColor           = System.Drawing.Color.FromArgb(248, 249, 251);
        ClientSize          = new System.Drawing.Size(820, 660);
        Controls.Add(pnlContent);
        Controls.Add(pnlNav);
        Controls.Add(pnlHeader);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Name            = "SquadForm";
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text            = "The Manager";

        pnlHeader.ResumeLayout(false);
        pnlNav.ResumeLayout(false);
        pnlContent.ResumeLayout(false);
        pnlSquadView.ResumeLayout(false);
        pnlPlayMatchView.ResumeLayout(false);
        pnlFixturesView.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvSquad).EndInit();
        ResumeLayout(false);
    }

    // Header
    private System.Windows.Forms.Panel pnlHeader;
    private System.Windows.Forms.Label lblClubName;
    private System.Windows.Forms.Label lblManagerName;
    private System.Windows.Forms.Label lblDivision;
    private System.Windows.Forms.Label lblMorale;
    private System.Windows.Forms.Label lblWeek;

    // Nav
    private System.Windows.Forms.Panel  pnlNav;
    private System.Windows.Forms.Panel  pnlNavIndicator;
    private System.Windows.Forms.Label  lblNavSection;
    private System.Windows.Forms.Button btnNavPlayMatch;
    private System.Windows.Forms.Button btnNavSquad;
    private System.Windows.Forms.Button btnNavFixtures;

    // Content shell
    private System.Windows.Forms.Panel pnlContent;

    // Squad view
    private System.Windows.Forms.Panel                           pnlSquadView;
    private System.Windows.Forms.DataGridView                    dgvSquad;
    private System.Windows.Forms.DataGridViewCheckBoxColumn      colSelect;
    private System.Windows.Forms.DataGridViewTextBoxColumn       colPos;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colSkill;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colAge;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colTemper;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colGames;

    // Placeholder views
    private System.Windows.Forms.Panel pnlPlayMatchView;
    private System.Windows.Forms.Label lblPlayMatchPlaceholder;
    private System.Windows.Forms.Panel pnlFixturesView;
    private System.Windows.Forms.Label lblFixturesPlaceholder;
}
