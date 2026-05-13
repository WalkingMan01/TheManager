namespace TheManager.WinForms;

partial class HomeView
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

        lblTitle         = new System.Windows.Forms.Label();
        lblSubtitle      = new System.Windows.Forms.Label();
        pnlCardBorder    = new System.Windows.Forms.Panel();
        pnlCard          = new System.Windows.Forms.Panel();
        pnlSectionHeader = new System.Windows.Forms.Panel();
        lblSection       = new System.Windows.Forms.Label();
        pnlTopBorder     = new System.Windows.Forms.Panel();
        btnNewGame       = new System.Windows.Forms.Button();
        pnlMidDivider    = new System.Windows.Forms.Panel();
        btnContinueGame  = new System.Windows.Forms.Button();
        pnlBottomBorder  = new System.Windows.Forms.Panel();

        pnlCardBorder.SuspendLayout();
        pnlCard.SuspendLayout();
        pnlSectionHeader.SuspendLayout();
        SuspendLayout();

        // lblTitle
        lblTitle.AutoSize  = false;
        lblTitle.Font      = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);     // slate-900
        lblTitle.Location  = new System.Drawing.Point(0, 150);
        lblTitle.Name      = "lblTitle";
        lblTitle.Size      = new System.Drawing.Size(820, 48);
        lblTitle.TabIndex  = 0;
        lblTitle.Text      = "The Manager";
        lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // lblSubtitle
        lblSubtitle.AutoSize  = false;
        lblSubtitle.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        lblSubtitle.Location  = new System.Drawing.Point(0, 202);
        lblSubtitle.Name      = "lblSubtitle";
        lblSubtitle.Size      = new System.Drawing.Size(820, 20);
        lblSubtitle.TabIndex  = 1;
        lblSubtitle.Text      = "Build your dynasty";
        lblSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // pnlCardBorder — 1 px slate-200 border via background bleed
        pnlCardBorder.BackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        pnlCardBorder.Controls.Add(pnlCard);
        pnlCardBorder.Location  = new System.Drawing.Point(139, 238);
        pnlCardBorder.Name      = "pnlCardBorder";
        pnlCardBorder.Size      = new System.Drawing.Size(542, 185);
        pnlCardBorder.TabIndex  = 2;

        // pnlCard — white interior, inset by 1 px
        pnlCard.BackColor = System.Drawing.Color.White;
        pnlCard.Controls.Add(pnlSectionHeader);
        pnlCard.Controls.Add(pnlTopBorder);
        pnlCard.Controls.Add(btnNewGame);
        pnlCard.Controls.Add(pnlMidDivider);
        pnlCard.Controls.Add(btnContinueGame);
        pnlCard.Controls.Add(pnlBottomBorder);
        pnlCard.Location  = new System.Drawing.Point(1, 1);
        pnlCard.Name      = "pnlCard";
        pnlCard.Size      = new System.Drawing.Size(540, 183);
        pnlCard.TabIndex  = 0;

        // pnlSectionHeader — "GET STARTED" label, styled like SquadView section headers
        pnlSectionHeader.BackColor = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        pnlSectionHeader.Controls.Add(lblSection);
        pnlSectionHeader.Location  = new System.Drawing.Point(0, 0);
        pnlSectionHeader.Name      = "pnlSectionHeader";
        pnlSectionHeader.Size      = new System.Drawing.Size(540, 36);
        pnlSectionHeader.TabIndex  = 0;

        lblSection.AutoSize  = false;
        lblSection.Font      = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblSection.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        lblSection.Location  = new System.Drawing.Point(16, 11);
        lblSection.Name      = "lblSection";
        lblSection.Size      = new System.Drawing.Size(200, 14);
        lblSection.TabIndex  = 0;
        lblSection.Text      = "GET STARTED";

        // pnlTopBorder
        pnlTopBorder.BackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        pnlTopBorder.Location  = new System.Drawing.Point(0, 36);
        pnlTopBorder.Name      = "pnlTopBorder";
        pnlTopBorder.Size      = new System.Drawing.Size(540, 1);
        pnlTopBorder.TabIndex  = 1;

        // btnNewGame
        btnNewGame.BackColor                         = System.Drawing.Color.White;
        btnNewGame.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnNewGame.FlatAppearance.BorderSize         = 0;
        btnNewGame.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        btnNewGame.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(241, 245, 249); // slate-100
        btnNewGame.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        btnNewGame.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);    // slate-900
        btnNewGame.Location  = new System.Drawing.Point(0, 37);
        btnNewGame.Name      = "btnNewGame";
        btnNewGame.Size      = new System.Drawing.Size(540, 72);
        btnNewGame.TabIndex  = 2;
        btnNewGame.Text      = "New Game";
        btnNewGame.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        btnNewGame.UseVisualStyleBackColor = false;
        btnNewGame.Click    += new System.EventHandler(btnNewGame_Click);

        // pnlMidDivider
        pnlMidDivider.BackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        pnlMidDivider.Location  = new System.Drawing.Point(0, 109);
        pnlMidDivider.Name      = "pnlMidDivider";
        pnlMidDivider.Size      = new System.Drawing.Size(540, 1);
        pnlMidDivider.TabIndex  = 3;

        // btnContinueGame
        btnContinueGame.BackColor                         = System.Drawing.Color.White;
        btnContinueGame.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnContinueGame.FlatAppearance.BorderSize         = 0;
        btnContinueGame.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        btnContinueGame.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(241, 245, 249); // slate-100
        btnContinueGame.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnContinueGame.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        btnContinueGame.Location  = new System.Drawing.Point(0, 110);
        btnContinueGame.Name      = "btnContinueGame";
        btnContinueGame.Size      = new System.Drawing.Size(540, 72);
        btnContinueGame.TabIndex  = 4;
        btnContinueGame.Text      = "Continue Game";
        btnContinueGame.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        btnContinueGame.UseVisualStyleBackColor = false;
        btnContinueGame.Click    += new System.EventHandler(btnContinueGame_Click);

        // pnlBottomBorder
        pnlBottomBorder.BackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        pnlBottomBorder.Location  = new System.Drawing.Point(0, 182);
        pnlBottomBorder.Name      = "pnlBottomBorder";
        pnlBottomBorder.Size      = new System.Drawing.Size(540, 1);
        pnlBottomBorder.TabIndex  = 5;

        // ── HomeView ──────────────────────────────────────────────────────────────
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        BackColor           = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        Controls.Add(lblTitle);
        Controls.Add(lblSubtitle);
        Controls.Add(pnlCardBorder);
        Name = "HomeView";
        Size = new System.Drawing.Size(820, 608);

        pnlCardBorder.ResumeLayout(false);
        pnlCard.ResumeLayout(false);
        pnlSectionHeader.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label  lblTitle;
    private System.Windows.Forms.Label  lblSubtitle;
    private System.Windows.Forms.Panel  pnlCardBorder;
    private System.Windows.Forms.Panel  pnlCard;
    private System.Windows.Forms.Panel  pnlSectionHeader;
    private System.Windows.Forms.Label  lblSection;
    private System.Windows.Forms.Panel  pnlTopBorder;
    private System.Windows.Forms.Button btnNewGame;
    private System.Windows.Forms.Panel  pnlMidDivider;
    private System.Windows.Forms.Button btnContinueGame;
    private System.Windows.Forms.Panel  pnlBottomBorder;
}
