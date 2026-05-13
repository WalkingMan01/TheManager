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
        components = new System.ComponentModel.Container();

        lblPlaceholder = new System.Windows.Forms.Label();
        SuspendLayout();

        lblPlaceholder.AutoSize  = false;
        lblPlaceholder.Dock      = System.Windows.Forms.DockStyle.Fill;
        lblPlaceholder.Font      = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        lblPlaceholder.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184); // slate-400
        lblPlaceholder.Name      = "lblPlaceholder";
        lblPlaceholder.TabIndex  = 0;
        lblPlaceholder.Text      = "Fixtures — coming soon";
        lblPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        BackColor           = System.Drawing.Color.FromArgb(248, 250, 252); // slate-50
        Controls.Add(lblPlaceholder);
        Name = "FixturesView";
        Size = new System.Drawing.Size(820, 608);

        ResumeLayout(false);
    }

    private System.Windows.Forms.Label lblPlaceholder;
}
