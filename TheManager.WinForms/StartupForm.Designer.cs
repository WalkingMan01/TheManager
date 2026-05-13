namespace TheManager.WinForms;

partial class StartupForm
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
        lblBrand       = new System.Windows.Forms.Label();
        pnlSeparator   = new System.Windows.Forms.Panel();
        pnlContent     = new System.Windows.Forms.Panel();
        lblTitle       = new System.Windows.Forms.Label();
        lblSubtitle    = new System.Windows.Forms.Label();
        pnlDivider     = new System.Windows.Forms.Panel();
        btnNewGame     = new System.Windows.Forms.Button();
        btnContinueGame = new System.Windows.Forms.Button();

        pnlHeader.SuspendLayout();
        pnlContent.SuspendLayout();
        SuspendLayout();

        // ── pnlHeader ────────────────────────────────────────────────────────────
        pnlHeader.BackColor = System.Drawing.Color.White;
        pnlHeader.Controls.Add(lblBrand);
        pnlHeader.Controls.Add(pnlSeparator);
        pnlHeader.Dock     = System.Windows.Forms.DockStyle.Top;
        pnlHeader.Name     = "pnlHeader";
        pnlHeader.Size     = new System.Drawing.Size(480, 52);
        pnlHeader.TabIndex = 0;

        lblBrand.AutoSize  = false;
        lblBrand.Font      = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblBrand.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);    // slate-900
        lblBrand.Location  = new System.Drawing.Point(16, 17);
        lblBrand.Name      = "lblBrand";
        lblBrand.Size      = new System.Drawing.Size(120, 18);
        lblBrand.TabIndex  = 0;
        lblBrand.Text      = "TheManager";

        pnlSeparator.BackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        pnlSeparator.Dock      = System.Windows.Forms.DockStyle.Bottom;
        pnlSeparator.Name      = "pnlSeparator";
        pnlSeparator.Size      = new System.Drawing.Size(480, 1);
        pnlSeparator.TabIndex  = 1;

        // ── pnlContent ───────────────────────────────────────────────────────────
        pnlContent.BackColor = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(lblSubtitle);
        pnlContent.Controls.Add(pnlDivider);
        pnlContent.Controls.Add(btnNewGame);
        pnlContent.Controls.Add(btnContinueGame);
        pnlContent.Dock     = System.Windows.Forms.DockStyle.Fill;
        pnlContent.Name     = "pnlContent";
        pnlContent.TabIndex = 1;

        // lblTitle
        lblTitle.AutoSize  = false;
        lblTitle.Font      = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);    // slate-900
        lblTitle.Location  = new System.Drawing.Point(90, 50);
        lblTitle.Name      = "lblTitle";
        lblTitle.Size      = new System.Drawing.Size(300, 44);
        lblTitle.TabIndex  = 0;
        lblTitle.Text      = "The Manager";
        lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // lblSubtitle
        lblSubtitle.AutoSize  = false;
        lblSubtitle.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        lblSubtitle.Location  = new System.Drawing.Point(90, 100);
        lblSubtitle.Name      = "lblSubtitle";
        lblSubtitle.Size      = new System.Drawing.Size(300, 20);
        lblSubtitle.TabIndex  = 1;
        lblSubtitle.Text      = "Build your dynasty";
        lblSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // pnlDivider
        pnlDivider.BackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        pnlDivider.Location  = new System.Drawing.Point(110, 136);
        pnlDivider.Name      = "pnlDivider";
        pnlDivider.Size      = new System.Drawing.Size(260, 1);
        pnlDivider.TabIndex  = 2;

        // btnNewGame (primary action)
        btnNewGame.BackColor                         = System.Drawing.Color.FromArgb(15, 23, 42);  // slate-900
        btnNewGame.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnNewGame.FlatAppearance.BorderSize         = 0;
        btnNewGame.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(30, 41, 59);  // slate-800
        btnNewGame.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(51, 65, 85);  // slate-700
        btnNewGame.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        btnNewGame.ForeColor = System.Drawing.Color.White;
        btnNewGame.Location  = new System.Drawing.Point(110, 152);
        btnNewGame.Name      = "btnNewGame";
        btnNewGame.Size      = new System.Drawing.Size(260, 44);
        btnNewGame.TabIndex  = 3;
        btnNewGame.Text      = "New Game";
        btnNewGame.UseVisualStyleBackColor = false;
        btnNewGame.Click    += new System.EventHandler(btnNewGame_Click);

        // btnContinueGame (secondary action)
        btnContinueGame.BackColor                         = System.Drawing.Color.White;
        btnContinueGame.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnContinueGame.FlatAppearance.BorderSize         = 1;
        btnContinueGame.FlatAppearance.BorderColor        = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        btnContinueGame.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        btnContinueGame.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(241, 245, 249); // slate-100
        btnContinueGame.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnContinueGame.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        btnContinueGame.Location  = new System.Drawing.Point(110, 204);
        btnContinueGame.Name      = "btnContinueGame";
        btnContinueGame.Size      = new System.Drawing.Size(260, 44);
        btnContinueGame.TabIndex  = 4;
        btnContinueGame.Text      = "Continue Game";
        btnContinueGame.UseVisualStyleBackColor = false;
        btnContinueGame.Click    += new System.EventHandler(btnContinueGame_Click);

        // ── StartupForm ──────────────────────────────────────────────────────────
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        BackColor           = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        ClientSize          = new System.Drawing.Size(480, 340);
        Controls.Add(pnlContent);
        Controls.Add(pnlHeader);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Name            = "StartupForm";
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text            = "The Manager";

        pnlHeader.ResumeLayout(false);
        pnlContent.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Panel  pnlHeader;
    private System.Windows.Forms.Label  lblBrand;
    private System.Windows.Forms.Panel  pnlSeparator;
    private System.Windows.Forms.Panel  pnlContent;
    private System.Windows.Forms.Label  lblTitle;
    private System.Windows.Forms.Label  lblSubtitle;
    private System.Windows.Forms.Panel  pnlDivider;
    private System.Windows.Forms.Button btnNewGame;
    private System.Windows.Forms.Button btnContinueGame;
}
