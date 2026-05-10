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

        // Header (top nav bar)
        pnlHeader      = new System.Windows.Forms.Panel();
        lblClubName    = new System.Windows.Forms.Label();
        lblManagerName = new System.Windows.Forms.Label();
        lblDivision    = new System.Windows.Forms.Label();
        lblMorale      = new System.Windows.Forms.Label();
        lblWeek        = new System.Windows.Forms.Label();

        // Nav controls (now live inside the header bar)
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
        pnlContent.SuspendLayout();
        pnlSquadView.SuspendLayout();
        pnlPlayMatchView.SuspendLayout();
        pnlFixturesView.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvSquad).BeginInit();
        SuspendLayout();

        // ── pnlHeader (top nav bar) ──────────────────────────────────────────────
        pnlHeader.BackColor = System.Drawing.Color.White;
        pnlHeader.Controls.Add(lblNavSection);    // brand label
        pnlHeader.Controls.Add(btnNavPlayMatch);
        pnlHeader.Controls.Add(btnNavSquad);
        pnlHeader.Controls.Add(btnNavFixtures);
        pnlHeader.Controls.Add(lblClubName);
        pnlHeader.Controls.Add(lblDivision);
        pnlHeader.Controls.Add(lblManagerName);   // hidden
        pnlHeader.Controls.Add(lblMorale);        // hidden
        pnlHeader.Controls.Add(lblWeek);          // hidden (merged into lblDivision)
        pnlHeader.Controls.Add(pnlNavIndicator);  // bottom separator line
        pnlHeader.Dock     = System.Windows.Forms.DockStyle.Top;
        pnlHeader.Name     = "pnlHeader";
        pnlHeader.Size     = new System.Drawing.Size(820, 52);
        pnlHeader.TabIndex = 0;

        // lblNavSection → repurposed as "TheManager" brand label
        lblNavSection.AutoSize  = false;
        lblNavSection.Font      = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblNavSection.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);    // slate-900
        lblNavSection.Location  = new System.Drawing.Point(16, 17);
        lblNavSection.Name      = "lblNavSection";
        lblNavSection.Size      = new System.Drawing.Size(120, 18);
        lblNavSection.TabIndex  = 0;
        lblNavSection.Text      = "TheManager";

        // btnNavPlayMatch
        btnNavPlayMatch.BackColor                         = System.Drawing.Color.White;
        btnNavPlayMatch.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnNavPlayMatch.FlatAppearance.BorderSize         = 0;
        btnNavPlayMatch.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(248, 250, 252);
        btnNavPlayMatch.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(241, 245, 249);
        btnNavPlayMatch.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnNavPlayMatch.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);  // slate-500
        btnNavPlayMatch.Location  = new System.Drawing.Point(144, 0);
        btnNavPlayMatch.Name      = "btnNavPlayMatch";
        btnNavPlayMatch.Size      = new System.Drawing.Size(100, 52);
        btnNavPlayMatch.TabIndex  = 1;
        btnNavPlayMatch.Text      = "Play Match";
        btnNavPlayMatch.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        btnNavPlayMatch.UseVisualStyleBackColor = false;
        btnNavPlayMatch.Click    += new System.EventHandler(btnNavPlayMatch_Click);

        // btnNavSquad (active by default)
        btnNavSquad.BackColor                         = System.Drawing.Color.FromArgb(241, 245, 249);  // slate-100
        btnNavSquad.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnNavSquad.FlatAppearance.BorderSize         = 0;
        btnNavSquad.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(241, 245, 249);
        btnNavSquad.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(226, 232, 240);
        btnNavSquad.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        btnNavSquad.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);          // slate-900
        btnNavSquad.Location  = new System.Drawing.Point(244, 0);
        btnNavSquad.Name      = "btnNavSquad";
        btnNavSquad.Size      = new System.Drawing.Size(80, 52);
        btnNavSquad.TabIndex  = 2;
        btnNavSquad.Text      = "Squad";
        btnNavSquad.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        btnNavSquad.UseVisualStyleBackColor = false;
        btnNavSquad.Click    += new System.EventHandler(btnNavSquad_Click);

        // btnNavFixtures
        btnNavFixtures.BackColor                         = System.Drawing.Color.White;
        btnNavFixtures.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnNavFixtures.FlatAppearance.BorderSize         = 0;
        btnNavFixtures.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(248, 250, 252);
        btnNavFixtures.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(241, 245, 249);
        btnNavFixtures.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnNavFixtures.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);   // slate-500
        btnNavFixtures.Location  = new System.Drawing.Point(324, 0);
        btnNavFixtures.Name      = "btnNavFixtures";
        btnNavFixtures.Size      = new System.Drawing.Size(90, 52);
        btnNavFixtures.TabIndex  = 3;
        btnNavFixtures.Text      = "Fixtures";
        btnNavFixtures.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        btnNavFixtures.UseVisualStyleBackColor = false;
        btnNavFixtures.Click    += new System.EventHandler(btnNavFixtures_Click);

        // pnlNavIndicator → bottom separator line for the nav bar
        pnlNavIndicator.BackColor = System.Drawing.Color.FromArgb(226, 232, 240);  // slate-200
        pnlNavIndicator.Dock      = System.Windows.Forms.DockStyle.Bottom;
        pnlNavIndicator.Name      = "pnlNavIndicator";
        pnlNavIndicator.Size      = new System.Drawing.Size(820, 1);
        pnlNavIndicator.TabIndex  = 9;

        // lblClubName — right-aligned club name
        lblClubName.AutoSize  = false;
        lblClubName.Font      = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblClubName.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);         // slate-900
        lblClubName.Location  = new System.Drawing.Point(430, 9);
        lblClubName.Name      = "lblClubName";
        lblClubName.Size      = new System.Drawing.Size(374, 17);
        lblClubName.TabIndex  = 5;
        lblClubName.Text      = "Club Name";
        lblClubName.TextAlign = System.Drawing.ContentAlignment.TopRight;

        // lblDivision — "Div X · Week Y" subtitle (week merged in by Populate)
        lblDivision.AutoSize  = false;
        lblDivision.Font      = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblDivision.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);      // slate-500
        lblDivision.Location  = new System.Drawing.Point(430, 28);
        lblDivision.Name      = "lblDivision";
        lblDivision.Size      = new System.Drawing.Size(374, 14);
        lblDivision.TabIndex  = 6;
        lblDivision.Text      = "Division —";
        lblDivision.TextAlign = System.Drawing.ContentAlignment.TopRight;

        // lblManagerName — hidden in the slim top bar
        lblManagerName.AutoSize = false;
        lblManagerName.Name     = "lblManagerName";
        lblManagerName.Size     = new System.Drawing.Size(0, 0);
        lblManagerName.TabIndex = 7;
        lblManagerName.Visible  = false;

        // lblMorale — hidden
        lblMorale.AutoSize = false;
        lblMorale.Name     = "lblMorale";
        lblMorale.Size     = new System.Drawing.Size(0, 0);
        lblMorale.TabIndex = 4;
        lblMorale.Visible  = false;

        // lblWeek — hidden; text merged into lblDivision by Populate()
        lblWeek.AutoSize = false;
        lblWeek.Name     = "lblWeek";
        lblWeek.Size     = new System.Drawing.Size(0, 0);
        lblWeek.TabIndex = 8;
        lblWeek.Visible  = false;

        // ── pnlNav — hidden (nav controls moved into pnlHeader) ──────────────────
        pnlNav.Dock     = System.Windows.Forms.DockStyle.None;
        pnlNav.Name     = "pnlNav";
        pnlNav.Size     = new System.Drawing.Size(0, 0);
        pnlNav.TabIndex = 1;
        pnlNav.Visible  = false;

        // ── pnlContent ───────────────────────────────────────────────────────────
        pnlContent.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);       // slate-50
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

        // dgvSquad
        dgvSquad.AllowUserToAddRows    = false;
        dgvSquad.AllowUserToDeleteRows = false;
        dgvSquad.AllowUserToResizeRows = false;
        dgvSquad.BackgroundColor       = System.Drawing.Color.White;
        dgvSquad.BorderStyle           = System.Windows.Forms.BorderStyle.None;
        dgvSquad.CellBorderStyle       = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
        dgvSquad.GridColor             = System.Drawing.Color.FromArgb(226, 232, 240);     // slate-200
        dgvSquad.EnableHeadersVisualStyles        = false;
        dgvSquad.ColumnHeadersHeight              = 34;
        dgvSquad.ColumnHeadersHeightSizeMode      = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
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

        // ── pnlPlayMatchView ─────────────────────────────────────────────────────
        pnlPlayMatchView.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
        pnlPlayMatchView.Controls.Add(lblPlayMatchPlaceholder);
        pnlPlayMatchView.Dock     = System.Windows.Forms.DockStyle.Fill;
        pnlPlayMatchView.Name     = "pnlPlayMatchView";
        pnlPlayMatchView.TabIndex = 1;
        pnlPlayMatchView.Visible  = false;

        lblPlayMatchPlaceholder.AutoSize  = false;
        lblPlayMatchPlaceholder.Dock      = System.Windows.Forms.DockStyle.Fill;
        lblPlayMatchPlaceholder.Font      = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblPlayMatchPlaceholder.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184); // slate-400
        lblPlayMatchPlaceholder.Name      = "lblPlayMatchPlaceholder";
        lblPlayMatchPlaceholder.TabIndex  = 0;
        lblPlayMatchPlaceholder.Text      = "Play Match — coming soon";
        lblPlayMatchPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // ── pnlFixturesView ──────────────────────────────────────────────────────
        pnlFixturesView.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
        pnlFixturesView.Controls.Add(lblFixturesPlaceholder);
        pnlFixturesView.Dock     = System.Windows.Forms.DockStyle.Fill;
        pnlFixturesView.Name     = "pnlFixturesView";
        pnlFixturesView.TabIndex = 2;
        pnlFixturesView.Visible  = false;

        lblFixturesPlaceholder.AutoSize  = false;
        lblFixturesPlaceholder.Dock      = System.Windows.Forms.DockStyle.Fill;
        lblFixturesPlaceholder.Font      = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblFixturesPlaceholder.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184); // slate-400
        lblFixturesPlaceholder.Name      = "lblFixturesPlaceholder";
        lblFixturesPlaceholder.TabIndex  = 0;
        lblFixturesPlaceholder.Text      = "Fixtures — coming soon";
        lblFixturesPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // ── SquadForm ────────────────────────────────────────────────────────────
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        BackColor           = System.Drawing.Color.FromArgb(248, 250, 252);        // slate-50
        ClientSize          = new System.Drawing.Size(820, 660);
        Controls.Add(pnlContent);
        Controls.Add(pnlNav);       // invisible, no dock — takes no space
        Controls.Add(pnlHeader);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Name            = "SquadForm";
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text            = "The Manager";

        pnlHeader.ResumeLayout(false);
        pnlContent.ResumeLayout(false);
        pnlSquadView.ResumeLayout(false);
        pnlPlayMatchView.ResumeLayout(false);
        pnlFixturesView.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvSquad).EndInit();
        ResumeLayout(false);
    }

    // Header / top nav bar
    private System.Windows.Forms.Panel pnlHeader;
    private System.Windows.Forms.Label lblClubName;
    private System.Windows.Forms.Label lblManagerName;
    private System.Windows.Forms.Label lblDivision;
    private System.Windows.Forms.Label lblMorale;
    private System.Windows.Forms.Label lblWeek;

    // Nav controls (now inside pnlHeader)
    private System.Windows.Forms.Panel  pnlNav;
    private System.Windows.Forms.Panel  pnlNavIndicator;
    private System.Windows.Forms.Label  lblNavSection;
    private System.Windows.Forms.Button btnNavPlayMatch;
    private System.Windows.Forms.Button btnNavSquad;
    private System.Windows.Forms.Button btnNavFixtures;

    // Content shell
    private System.Windows.Forms.Panel pnlContent;

    // Squad view
    private System.Windows.Forms.Panel                      pnlSquadView;
    private System.Windows.Forms.DataGridView               dgvSquad;
    private System.Windows.Forms.DataGridViewCheckBoxColumn colSelect;
    private System.Windows.Forms.DataGridViewTextBoxColumn  colPos;
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
