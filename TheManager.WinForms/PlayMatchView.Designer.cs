namespace TheManager.WinForms;

partial class PlayMatchView
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
        lblMatchInfo = new System.Windows.Forms.Label();
        pnlTeamsRow  = new System.Windows.Forms.Panel();
        lblHomeTeam  = new System.Windows.Forms.Label();
        pnlCenter    = new System.Windows.Forms.Panel();
        btnPlayMatch = new System.Windows.Forms.Button();
        pnlScore     = new System.Windows.Forms.Panel();
        lblHomeScore = new System.Windows.Forms.Label();
        lblScoreSep  = new System.Windows.Forms.Label();
        lblAwayScore = new System.Windows.Forms.Label();
        lblAwayTeam  = new System.Windows.Forms.Label();
        pnlSep       = new System.Windows.Forms.Panel();
        lblResult    = new System.Windows.Forms.Label();
        dgvGoals     = new System.Windows.Forms.DataGridView();
        colMinute    = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colEvent     = new System.Windows.Forms.DataGridViewTextBoxColumn();

        pnlCard.SuspendLayout();
        pnlTeamsRow.SuspendLayout();
        pnlCenter.SuspendLayout();
        pnlScore.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvGoals).BeginInit();
        SuspendLayout();

        // ── pnlCard ──────────────────────────────────────────────────────────────
        pnlCard.BackColor = System.Drawing.Color.White;
        pnlCard.Controls.Add(lblMatchInfo);
        pnlCard.Controls.Add(pnlTeamsRow);
        pnlCard.Controls.Add(pnlSep);
        pnlCard.Controls.Add(lblResult);
        pnlCard.Controls.Add(dgvGoals);
        pnlCard.Location = new System.Drawing.Point(110, 40);
        pnlCard.Name     = "pnlCard";
        pnlCard.Size     = new System.Drawing.Size(600, 490);

        // ── lblMatchInfo ─────────────────────────────────────────────────────────
        lblMatchInfo.AutoSize  = false;
        lblMatchInfo.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblMatchInfo.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        lblMatchInfo.Location  = new System.Drawing.Point(0, 24);
        lblMatchInfo.Name      = "lblMatchInfo";
        lblMatchInfo.Size      = new System.Drawing.Size(600, 20);
        lblMatchInfo.Text      = "—";
        lblMatchInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // ── pnlTeamsRow ───────────────────────────────────────────────────────────
        pnlTeamsRow.BackColor = System.Drawing.Color.FromArgb(241, 245, 249); // slate-100
        pnlTeamsRow.Controls.Add(lblHomeTeam);
        pnlTeamsRow.Controls.Add(pnlCenter);
        pnlTeamsRow.Controls.Add(lblAwayTeam);
        pnlTeamsRow.Location  = new System.Drawing.Point(0, 54);
        pnlTeamsRow.Name      = "pnlTeamsRow";
        pnlTeamsRow.Size      = new System.Drawing.Size(600, 88);

        // ── lblHomeTeam ───────────────────────────────────────────────────────────
        lblHomeTeam.AutoSize  = false;
        lblHomeTeam.Font      = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblHomeTeam.ForeColor = System.Drawing.Color.FromArgb(4, 120, 87);  // emerald-700
        lblHomeTeam.Location  = new System.Drawing.Point(0, 0);
        lblHomeTeam.Name      = "lblHomeTeam";
        lblHomeTeam.Size      = new System.Drawing.Size(185, 88);
        lblHomeTeam.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

        // ── pnlCenter ─────────────────────────────────────────────────────────────
        pnlCenter.BackColor = System.Drawing.Color.FromArgb(241, 245, 249); // slate-100
        pnlCenter.Controls.Add(btnPlayMatch);
        pnlCenter.Controls.Add(pnlScore);
        pnlCenter.Location  = new System.Drawing.Point(185, 0);
        pnlCenter.Name      = "pnlCenter";
        pnlCenter.Size      = new System.Drawing.Size(230, 88);

        // ── btnPlayMatch ──────────────────────────────────────────────────────────
        btnPlayMatch.BackColor                         = System.Drawing.Color.FromArgb(5, 150, 105);   // emerald-600
        btnPlayMatch.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
        btnPlayMatch.FlatAppearance.BorderSize         = 0;
        btnPlayMatch.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(4, 120, 87);    // emerald-700
        btnPlayMatch.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(6, 95, 70);     // emerald-800
        btnPlayMatch.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        btnPlayMatch.ForeColor = System.Drawing.Color.White;
        btnPlayMatch.Location  = new System.Drawing.Point(15, 19);
        btnPlayMatch.Name      = "btnPlayMatch";
        btnPlayMatch.Size      = new System.Drawing.Size(200, 50);
        btnPlayMatch.TabIndex  = 0;
        btnPlayMatch.Text      = "PLAY MATCH";
        btnPlayMatch.UseVisualStyleBackColor = false;
        btnPlayMatch.Click    += new System.EventHandler(btnPlayMatch_Click);

        // ── pnlScore ──────────────────────────────────────────────────────────────
        pnlScore.BackColor = System.Drawing.Color.FromArgb(241, 245, 249); // slate-100
        pnlScore.Controls.Add(lblHomeScore);
        pnlScore.Controls.Add(lblScoreSep);
        pnlScore.Controls.Add(lblAwayScore);
        pnlScore.Location  = new System.Drawing.Point(0, 0);
        pnlScore.Name      = "pnlScore";
        pnlScore.Size      = new System.Drawing.Size(230, 88);
        pnlScore.Visible   = false;

        // ── lblHomeScore ──────────────────────────────────────────────────────────
        lblHomeScore.AutoSize  = false;
        lblHomeScore.Font      = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblHomeScore.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42); // slate-900
        lblHomeScore.Location  = new System.Drawing.Point(0, 0);
        lblHomeScore.Name      = "lblHomeScore";
        lblHomeScore.Size      = new System.Drawing.Size(80, 88);
        lblHomeScore.Text      = "0";
        lblHomeScore.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

        // ── lblScoreSep ───────────────────────────────────────────────────────────
        lblScoreSep.AutoSize  = false;
        lblScoreSep.Font      = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblScoreSep.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184); // slate-400
        lblScoreSep.Location  = new System.Drawing.Point(80, 0);
        lblScoreSep.Name      = "lblScoreSep";
        lblScoreSep.Size      = new System.Drawing.Size(70, 88);
        lblScoreSep.Text      = "—";
        lblScoreSep.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // ── lblAwayScore ──────────────────────────────────────────────────────────
        lblAwayScore.AutoSize  = false;
        lblAwayScore.Font      = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblAwayScore.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42); // slate-900
        lblAwayScore.Location  = new System.Drawing.Point(150, 0);
        lblAwayScore.Name      = "lblAwayScore";
        lblAwayScore.Size      = new System.Drawing.Size(80, 88);
        lblAwayScore.Text      = "0";
        lblAwayScore.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        // ── lblAwayTeam ───────────────────────────────────────────────────────────
        lblAwayTeam.AutoSize  = false;
        lblAwayTeam.Font      = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblAwayTeam.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42); // slate-900
        lblAwayTeam.Location  = new System.Drawing.Point(415, 0);
        lblAwayTeam.Name      = "lblAwayTeam";
        lblAwayTeam.Size      = new System.Drawing.Size(185, 88);
        lblAwayTeam.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        // ── pnlSep ────────────────────────────────────────────────────────────────
        pnlSep.BackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        pnlSep.Location  = new System.Drawing.Point(0, 150);
        pnlSep.Name      = "pnlSep";
        pnlSep.Size      = new System.Drawing.Size(600, 1);
        pnlSep.Visible   = false;

        // ── lblResult ─────────────────────────────────────────────────────────────
        lblResult.AutoSize  = false;
        lblResult.Font      = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblResult.Location  = new System.Drawing.Point(0, 160);
        lblResult.Name      = "lblResult";
        lblResult.Size      = new System.Drawing.Size(600, 30);
        lblResult.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        lblResult.Visible   = false;

        // ── dgvGoals ──────────────────────────────────────────────────────────────
        dgvGoals.AllowUserToAddRows    = false;
        dgvGoals.AllowUserToDeleteRows = false;
        dgvGoals.AllowUserToResizeRows = false;
        dgvGoals.BackgroundColor       = System.Drawing.Color.White;
        dgvGoals.BorderStyle           = System.Windows.Forms.BorderStyle.None;
        dgvGoals.CellBorderStyle       = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
        dgvGoals.ColumnHeadersBorderStyle                        = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
        dgvGoals.ColumnHeadersDefaultCellStyle.BackColor         = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        dgvGoals.ColumnHeadersDefaultCellStyle.ForeColor         = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        dgvGoals.ColumnHeadersDefaultCellStyle.Font              = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        dgvGoals.ColumnHeadersDefaultCellStyle.Padding           = new System.Windows.Forms.Padding(6, 0, 0, 0);
        dgvGoals.ColumnHeadersHeight                             = 30;
        dgvGoals.ColumnHeadersHeightSizeMode                     = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvGoals.Columns.AddRange(colMinute, colEvent);
        dgvGoals.DefaultCellStyle.BackColor          = System.Drawing.Color.White;
        dgvGoals.DefaultCellStyle.ForeColor          = System.Drawing.Color.FromArgb(15, 23, 42); // slate-900
        dgvGoals.DefaultCellStyle.Font               = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        dgvGoals.DefaultCellStyle.Padding            = new System.Windows.Forms.Padding(6, 0, 0, 0);
        dgvGoals.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        dgvGoals.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
        dgvGoals.EnableHeadersVisualStyles  = false;
        dgvGoals.GridColor                  = System.Drawing.Color.FromArgb(226, 232, 240); // slate-200
        dgvGoals.Location                   = new System.Drawing.Point(0, 200);
        dgvGoals.MultiSelect                = false;
        dgvGoals.Name                       = "dgvGoals";
        dgvGoals.ReadOnly                   = true;
        dgvGoals.RowHeadersVisible          = false;
        dgvGoals.RowTemplate.Height         = 30;
        dgvGoals.SelectionMode              = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        dgvGoals.Size                       = new System.Drawing.Size(600, 282);
        dgvGoals.TabIndex                   = 1;
        dgvGoals.Visible                    = false;

        // ── colMinute ─────────────────────────────────────────────────────────────
        colMinute.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        colMinute.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // slate-500
        colMinute.DefaultCellStyle.Padding   = new System.Windows.Forms.Padding(0, 0, 8, 0);
        colMinute.HeaderText                 = "MIN";
        colMinute.Name                       = "colMinute";
        colMinute.ReadOnly                   = true;
        colMinute.Width                      = 60;

        // ── colEvent ──────────────────────────────────────────────────────────────
        colEvent.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        colEvent.HeaderText   = "GOAL";
        colEvent.Name         = "colEvent";
        colEvent.ReadOnly     = true;

        // ── PlayMatchView ─────────────────────────────────────────────────────────
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        BackColor           = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        Controls.Add(pnlCard);
        Name = "PlayMatchView";
        Size = new System.Drawing.Size(820, 608);

        pnlCard.ResumeLayout(false);
        pnlTeamsRow.ResumeLayout(false);
        pnlCenter.ResumeLayout(false);
        pnlScore.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvGoals).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.Panel                     pnlCard;
    private System.Windows.Forms.Label                     lblMatchInfo;
    private System.Windows.Forms.Panel                     pnlTeamsRow;
    private System.Windows.Forms.Label                     lblHomeTeam;
    private System.Windows.Forms.Panel                     pnlCenter;
    private System.Windows.Forms.Button                    btnPlayMatch;
    private System.Windows.Forms.Panel                     pnlScore;
    private System.Windows.Forms.Label                     lblHomeScore;
    private System.Windows.Forms.Label                     lblScoreSep;
    private System.Windows.Forms.Label                     lblAwayScore;
    private System.Windows.Forms.Label                     lblAwayTeam;
    private System.Windows.Forms.Panel                     pnlSep;
    private System.Windows.Forms.Label                     lblResult;
    private System.Windows.Forms.DataGridView              dgvGoals;
    private System.Windows.Forms.DataGridViewTextBoxColumn colMinute;
    private System.Windows.Forms.DataGridViewTextBoxColumn colEvent;
}
