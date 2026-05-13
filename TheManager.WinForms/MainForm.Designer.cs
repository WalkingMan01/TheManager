namespace TheManager.WinForms;

partial class MainForm
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

        pnlHeader       = new System.Windows.Forms.Panel();
        lblBrand        = new System.Windows.Forms.Label();
        btnNavPlayMatch = new System.Windows.Forms.Button();
        btnNavSquad     = new System.Windows.Forms.Button();
        btnNavFixtures  = new System.Windows.Forms.Button();
        lblClubName     = new System.Windows.Forms.Label();
        lblDivision     = new System.Windows.Forms.Label();
        pnlNavIndicator = new System.Windows.Forms.Panel();
        pnlContent      = new System.Windows.Forms.Panel();

        pnlHeader.SuspendLayout();
        SuspendLayout();

        // ── pnlHeader ────────────────────────────────────────────────────────────
        pnlHeader.BackColor = System.Drawing.Color.White;
        pnlHeader.Controls.Add(lblBrand);
        pnlHeader.Controls.Add(btnNavPlayMatch);
        pnlHeader.Controls.Add(btnNavSquad);
        pnlHeader.Controls.Add(btnNavFixtures);
        pnlHeader.Controls.Add(lblClubName);
        pnlHeader.Controls.Add(lblDivision);
        pnlHeader.Controls.Add(pnlNavIndicator);
        pnlHeader.Dock     = System.Windows.Forms.DockStyle.Top;
        pnlHeader.Name     = "pnlHeader";
        pnlHeader.Size     = new System.Drawing.Size(820, 52);
        pnlHeader.TabIndex = 0;

        lblBrand.AutoSize  = false;
        lblBrand.Font      = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblBrand.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);    // slate-900
        lblBrand.Location  = new System.Drawing.Point(16, 17);
        lblBrand.Name      = "lblBrand";
        lblBrand.Size      = new System.Drawing.Size(120, 18);
        lblBrand.TabIndex  = 0;
        lblBrand.Text      = "TheManager";

        btnNavPlayMatch.BackColor                         = System.Drawing.Color.White;
        btnNavPlayMatch.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnNavPlayMatch.FlatAppearance.BorderSize         = 0;
        btnNavPlayMatch.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(248, 250, 252);
        btnNavPlayMatch.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(241, 245, 249);
        btnNavPlayMatch.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnNavPlayMatch.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        btnNavPlayMatch.Location  = new System.Drawing.Point(144, 0);
        btnNavPlayMatch.Name      = "btnNavPlayMatch";
        btnNavPlayMatch.Size      = new System.Drawing.Size(100, 52);
        btnNavPlayMatch.TabIndex  = 1;
        btnNavPlayMatch.Text      = "Play Match";
        btnNavPlayMatch.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        btnNavPlayMatch.UseVisualStyleBackColor = false;
        btnNavPlayMatch.Click    += new System.EventHandler(btnNavPlayMatch_Click);

        btnNavSquad.BackColor                         = System.Drawing.Color.FromArgb(241, 245, 249); // slate-100
        btnNavSquad.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnNavSquad.FlatAppearance.BorderSize         = 0;
        btnNavSquad.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(241, 245, 249);
        btnNavSquad.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(226, 232, 240);
        btnNavSquad.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        btnNavSquad.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);         // slate-900
        btnNavSquad.Location  = new System.Drawing.Point(244, 0);
        btnNavSquad.Name      = "btnNavSquad";
        btnNavSquad.Size      = new System.Drawing.Size(80, 52);
        btnNavSquad.TabIndex  = 2;
        btnNavSquad.Text      = "Squad";
        btnNavSquad.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        btnNavSquad.UseVisualStyleBackColor = false;
        btnNavSquad.Click    += new System.EventHandler(btnNavSquad_Click);

        btnNavFixtures.BackColor                         = System.Drawing.Color.White;
        btnNavFixtures.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnNavFixtures.FlatAppearance.BorderSize         = 0;
        btnNavFixtures.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(248, 250, 252);
        btnNavFixtures.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(241, 245, 249);
        btnNavFixtures.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnNavFixtures.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);  // slate-500
        btnNavFixtures.Location  = new System.Drawing.Point(324, 0);
        btnNavFixtures.Name      = "btnNavFixtures";
        btnNavFixtures.Size      = new System.Drawing.Size(90, 52);
        btnNavFixtures.TabIndex  = 3;
        btnNavFixtures.Text      = "Fixtures";
        btnNavFixtures.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        btnNavFixtures.UseVisualStyleBackColor = false;
        btnNavFixtures.Click    += new System.EventHandler(btnNavFixtures_Click);

        lblClubName.AutoSize  = false;
        lblClubName.Font      = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblClubName.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);        // slate-900
        lblClubName.Location  = new System.Drawing.Point(430, 9);
        lblClubName.Name      = "lblClubName";
        lblClubName.Size      = new System.Drawing.Size(374, 17);
        lblClubName.TabIndex  = 4;
        lblClubName.Text      = "Club Name";
        lblClubName.TextAlign = System.Drawing.ContentAlignment.TopRight;

        lblDivision.AutoSize  = false;
        lblDivision.Font      = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblDivision.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);     // slate-500
        lblDivision.Location  = new System.Drawing.Point(430, 28);
        lblDivision.Name      = "lblDivision";
        lblDivision.Size      = new System.Drawing.Size(374, 14);
        lblDivision.TabIndex  = 5;
        lblDivision.Text      = "Division —";
        lblDivision.TextAlign = System.Drawing.ContentAlignment.TopRight;

        pnlNavIndicator.BackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        pnlNavIndicator.Dock      = System.Windows.Forms.DockStyle.Bottom;
        pnlNavIndicator.Name      = "pnlNavIndicator";
        pnlNavIndicator.Size      = new System.Drawing.Size(820, 1);
        pnlNavIndicator.TabIndex  = 6;

        // ── pnlContent ───────────────────────────────────────────────────────────
        pnlContent.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);      // slate-50
        pnlContent.Dock     = System.Windows.Forms.DockStyle.Fill;
        pnlContent.Name     = "pnlContent";
        pnlContent.TabIndex = 1;

        // ── MainForm ─────────────────────────────────────────────────────────────
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        BackColor           = System.Drawing.Color.FromArgb(248, 250, 252);       // slate-50
        ClientSize          = new System.Drawing.Size(820, 660);
        Controls.Add(pnlContent);
        Controls.Add(pnlHeader);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Name            = "MainForm";
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text            = "The Manager";

        pnlHeader.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Panel  pnlHeader;
    private System.Windows.Forms.Label  lblBrand;
    private System.Windows.Forms.Button btnNavPlayMatch;
    private System.Windows.Forms.Button btnNavSquad;
    private System.Windows.Forms.Button btnNavFixtures;
    private System.Windows.Forms.Label  lblClubName;
    private System.Windows.Forms.Label  lblDivision;
    private System.Windows.Forms.Panel  pnlNavIndicator;
    private System.Windows.Forms.Panel  pnlContent;
}
