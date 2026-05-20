namespace TheManager.WinForms;

partial class CheckMatchView
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
        components   = new System.ComponentModel.Container();
        pnlCard      = new System.Windows.Forms.Panel();
        lblTitle     = new System.Windows.Forms.Label();
        lblMatchInfo = new System.Windows.Forms.Label();
        pnlTeamsRow  = new System.Windows.Forms.Panel();
        lblOurTeam   = new System.Windows.Forms.Label();
        lblVs        = new System.Windows.Forms.Label();
        lblOpponent  = new System.Windows.Forms.Label();
        pnlSep       = new System.Windows.Forms.Panel();
        dgvRatings   = new System.Windows.Forms.DataGridView();
        colPosition  = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colOurs      = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colOpponent  = new System.Windows.Forms.DataGridViewTextBoxColumn();

        pnlCard.SuspendLayout();
        pnlTeamsRow.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvRatings).BeginInit();
        SuspendLayout();

        // ── pnlCard ──────────────────────────────────────────────────────────────
        pnlCard.BackColor   = System.Drawing.Color.White;
        pnlCard.BorderStyle = System.Windows.Forms.BorderStyle.None;
        pnlCard.Controls.Add(lblTitle);
        pnlCard.Controls.Add(lblMatchInfo);
        pnlCard.Controls.Add(pnlTeamsRow);
        pnlCard.Controls.Add(pnlSep);
        pnlCard.Controls.Add(dgvRatings);
        pnlCard.Location = new System.Drawing.Point(110, 40);
        pnlCard.Name     = "pnlCard";
        pnlCard.Size     = new System.Drawing.Size(600, 400);

        // ── lblTitle ─────────────────────────────────────────────────────────────
        lblTitle.AutoSize  = false;
        lblTitle.Font      = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42); // slate-900
        lblTitle.Location  = new System.Drawing.Point(0, 24);
        lblTitle.Name      = "lblTitle";
        lblTitle.Size      = new System.Drawing.Size(600, 30);
        lblTitle.Text      = "NEXT MATCH";
        lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // ── lblMatchInfo ─────────────────────────────────────────────────────────
        lblMatchInfo.AutoSize  = false;
        lblMatchInfo.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblMatchInfo.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        lblMatchInfo.Location  = new System.Drawing.Point(0, 58);
        lblMatchInfo.Name      = "lblMatchInfo";
        lblMatchInfo.Size      = new System.Drawing.Size(600, 20);
        lblMatchInfo.Text      = "—";
        lblMatchInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // ── pnlTeamsRow ───────────────────────────────────────────────────────────
        pnlTeamsRow.BackColor = System.Drawing.Color.FromArgb(241, 245, 249); // slate-100
        pnlTeamsRow.Controls.Add(lblOurTeam);
        pnlTeamsRow.Controls.Add(lblVs);
        pnlTeamsRow.Controls.Add(lblOpponent);
        pnlTeamsRow.Location = new System.Drawing.Point(0, 88);
        pnlTeamsRow.Name     = "pnlTeamsRow";
        pnlTeamsRow.Size     = new System.Drawing.Size(600, 72);

        // ── lblOurTeam ────────────────────────────────────────────────────────────
        lblOurTeam.AutoSize  = false;
        lblOurTeam.Font      = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblOurTeam.ForeColor = System.Drawing.Color.FromArgb(4, 120, 87); // emerald-700
        lblOurTeam.Location  = new System.Drawing.Point(0, 0);
        lblOurTeam.Name      = "lblOurTeam";
        lblOurTeam.Size      = new System.Drawing.Size(260, 72);
        lblOurTeam.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

        // ── lblVs ─────────────────────────────────────────────────────────────────
        lblVs.AutoSize  = false;
        lblVs.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblVs.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        lblVs.Location  = new System.Drawing.Point(260, 0);
        lblVs.Name      = "lblVs";
        lblVs.Size      = new System.Drawing.Size(80, 72);
        lblVs.Text      = "vs";
        lblVs.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // ── lblOpponent ───────────────────────────────────────────────────────────
        lblOpponent.AutoSize  = false;
        lblOpponent.Font      = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblOpponent.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42); // slate-900
        lblOpponent.Location  = new System.Drawing.Point(340, 0);
        lblOpponent.Name      = "lblOpponent";
        lblOpponent.Size      = new System.Drawing.Size(260, 72);
        lblOpponent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        // ── pnlSep ────────────────────────────────────────────────────────────────
        pnlSep.BackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        pnlSep.Location  = new System.Drawing.Point(0, 168);
        pnlSep.Name      = "pnlSep";
        pnlSep.Size      = new System.Drawing.Size(600, 1);

        // ── dgvRatings ────────────────────────────────────────────────────────────
        dgvRatings.AllowUserToAddRows    = false;
        dgvRatings.AllowUserToDeleteRows = false;
        dgvRatings.AllowUserToResizeRows = false;
        dgvRatings.BackgroundColor       = System.Drawing.Color.White;
        dgvRatings.BorderStyle           = System.Windows.Forms.BorderStyle.None;
        dgvRatings.CellBorderStyle       = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
        dgvRatings.ColumnHeadersBorderStyle                        = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
        dgvRatings.ColumnHeadersDefaultCellStyle.BackColor         = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        dgvRatings.ColumnHeadersDefaultCellStyle.ForeColor         = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        dgvRatings.ColumnHeadersDefaultCellStyle.Font              = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        dgvRatings.ColumnHeadersDefaultCellStyle.Padding           = new System.Windows.Forms.Padding(4, 0, 0, 0);
        dgvRatings.ColumnHeadersHeight                             = 32;
        dgvRatings.ColumnHeadersHeightSizeMode                     = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvRatings.Columns.AddRange(colPosition, colOurs, colOpponent);
        dgvRatings.DefaultCellStyle.BackColor          = System.Drawing.Color.White;
        dgvRatings.DefaultCellStyle.ForeColor          = System.Drawing.Color.FromArgb(15, 23, 42); // slate-900
        dgvRatings.DefaultCellStyle.Font               = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        dgvRatings.DefaultCellStyle.Padding            = new System.Windows.Forms.Padding(4, 0, 0, 0);
        dgvRatings.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        dgvRatings.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
        dgvRatings.EnableHeadersVisualStyles  = false;
        dgvRatings.GridColor                  = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        dgvRatings.Location                   = new System.Drawing.Point(0, 178);
        dgvRatings.MultiSelect                = false;
        dgvRatings.Name                       = "dgvRatings";
        dgvRatings.ReadOnly                   = true;
        dgvRatings.RowHeadersVisible          = false;
        dgvRatings.RowTemplate.Height         = 44;
        dgvRatings.SelectionMode              = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        dgvRatings.Size                       = new System.Drawing.Size(600, 214);
        dgvRatings.TabIndex                   = 0;

        // ── colPosition ───────────────────────────────────────────────────────────
        colPosition.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        colPosition.HeaderText            = "POSITION";
        colPosition.Name                  = "colPosition";
        colPosition.ReadOnly              = true;
        colPosition.Width                 = 140;

        // ── colOurs ───────────────────────────────────────────────────────────────
        colOurs.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colOurs.DefaultCellStyle.Font      = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        colOurs.AutoSizeMode               = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        colOurs.HeaderText                 = "YOUR TEAM";
        colOurs.Name                       = "colOurs";
        colOurs.ReadOnly                   = true;

        // ── colOpponent ───────────────────────────────────────────────────────────
        colOpponent.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colOpponent.DefaultCellStyle.Font      = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        colOpponent.AutoSizeMode               = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        colOpponent.HeaderText                 = "OPPONENT";
        colOpponent.Name                       = "colOpponent";
        colOpponent.ReadOnly                   = true;

        // ── CheckMatchView ────────────────────────────────────────────────────────
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        BackColor           = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        Controls.Add(pnlCard);
        Name = "CheckMatchView";
        Size = new System.Drawing.Size(820, 608);

        pnlCard.ResumeLayout(false);
        pnlTeamsRow.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvRatings).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.Panel                     pnlCard;
    private System.Windows.Forms.Label                     lblTitle;
    private System.Windows.Forms.Label                     lblMatchInfo;
    private System.Windows.Forms.Panel                     pnlTeamsRow;
    private System.Windows.Forms.Label                     lblOurTeam;
    private System.Windows.Forms.Label                     lblVs;
    private System.Windows.Forms.Label                     lblOpponent;
    private System.Windows.Forms.Panel                     pnlSep;
    private System.Windows.Forms.DataGridView              dgvRatings;
    private System.Windows.Forms.DataGridViewTextBoxColumn colPosition;
    private System.Windows.Forms.DataGridViewTextBoxColumn colOurs;
    private System.Windows.Forms.DataGridViewTextBoxColumn colOpponent;
}
