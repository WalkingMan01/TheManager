namespace TheManager.WinForms;

partial class FixturesView
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
        components  = new System.ComponentModel.Container();
        dgvFixtures = new System.Windows.Forms.DataGridView();
        colWeek     = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colVenue    = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colOpponent = new System.Windows.Forms.DataGridViewTextBoxColumn();
        colType     = new System.Windows.Forms.DataGridViewTextBoxColumn();

        ((System.ComponentModel.ISupportInitialize)dgvFixtures).BeginInit();
        SuspendLayout();

        // dgvFixtures
        dgvFixtures.AllowUserToAddRows    = false;
        dgvFixtures.AllowUserToDeleteRows = false;
        dgvFixtures.AllowUserToResizeRows = false;
        dgvFixtures.BackgroundColor       = System.Drawing.Color.FromArgb(248, 250, 252);
        dgvFixtures.BorderStyle           = System.Windows.Forms.BorderStyle.None;
        dgvFixtures.CellBorderStyle       = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
        dgvFixtures.ColumnHeadersBorderStyle                        = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
        dgvFixtures.ColumnHeadersDefaultCellStyle.BackColor         = System.Drawing.Color.FromArgb(241, 245, 249);
        dgvFixtures.ColumnHeadersDefaultCellStyle.ForeColor         = System.Drawing.Color.FromArgb(100, 116, 139);
        dgvFixtures.ColumnHeadersDefaultCellStyle.Font              = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        dgvFixtures.ColumnHeadersDefaultCellStyle.Padding           = new System.Windows.Forms.Padding(4, 0, 0, 0);
        dgvFixtures.ColumnHeadersHeight                             = 28;
        dgvFixtures.ColumnHeadersHeightSizeMode                     = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvFixtures.Columns.AddRange(colWeek, colVenue, colOpponent, colType);
        dgvFixtures.DefaultCellStyle.BackColor          = System.Drawing.Color.FromArgb(248, 250, 252);
        dgvFixtures.DefaultCellStyle.ForeColor          = System.Drawing.Color.FromArgb(15, 23, 42);
        dgvFixtures.DefaultCellStyle.Font               = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        dgvFixtures.DefaultCellStyle.Padding            = new System.Windows.Forms.Padding(4, 0, 0, 0);
        dgvFixtures.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(226, 232, 240);
        dgvFixtures.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
        dgvFixtures.Dock                                = System.Windows.Forms.DockStyle.Fill;
        dgvFixtures.GridColor                           = System.Drawing.Color.FromArgb(226, 232, 240);
        dgvFixtures.MultiSelect                         = false;
        dgvFixtures.Name                                = "dgvFixtures";
        dgvFixtures.ReadOnly                            = true;
        dgvFixtures.RowHeadersVisible                   = false;
        dgvFixtures.RowTemplate.Height                  = 26;
        dgvFixtures.SelectionMode                       = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        dgvFixtures.TabIndex                            = 0;

        // colWeek
        colWeek.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colWeek.HeaderText                 = "WK";
        colWeek.Name                       = "colWeek";
        colWeek.Width                      = 45;

        // colVenue
        colVenue.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
        colVenue.HeaderText                 = "H/A";
        colVenue.Name                       = "colVenue";
        colVenue.Width                      = 45;

        // colOpponent
        colOpponent.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        colOpponent.HeaderText   = "Opponent";
        colOpponent.Name         = "colOpponent";

        // colType
        colType.HeaderText = "Type";
        colType.Name       = "colType";
        colType.Width      = 90;

        // FixturesView
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        BackColor           = System.Drawing.Color.FromArgb(248, 250, 252);
        Controls.Add(dgvFixtures);
        Name = "FixturesView";
        Size = new System.Drawing.Size(820, 608);

        ((System.ComponentModel.ISupportInitialize)dgvFixtures).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.DataGridView              dgvFixtures;
    private System.Windows.Forms.DataGridViewTextBoxColumn colWeek;
    private System.Windows.Forms.DataGridViewTextBoxColumn colVenue;
    private System.Windows.Forms.DataGridViewTextBoxColumn colOpponent;
    private System.Windows.Forms.DataGridViewTextBoxColumn colType;
}
