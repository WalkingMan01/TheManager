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

        lblTitle = new System.Windows.Forms.Label();
        lblSubtitle = new System.Windows.Forms.Label();
        btnNewGame = new System.Windows.Forms.Button();
        btnContinueGame = new System.Windows.Forms.Button();
        pnlCenter = new System.Windows.Forms.Panel();
        pnlCenter.SuspendLayout();
        SuspendLayout();

        // lblTitle
        lblTitle.AutoSize = false;
        lblTitle.Dock = System.Windows.Forms.DockStyle.None;
        lblTitle.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblTitle.ForeColor = System.Drawing.Color.White;
        lblTitle.Location = new System.Drawing.Point(0, 40);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new System.Drawing.Size(480, 60);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "The Manager";
        lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // lblSubtitle
        lblSubtitle.AutoSize = false;
        lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
        lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(180, 220, 180);
        lblSubtitle.Location = new System.Drawing.Point(0, 105);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new System.Drawing.Size(480, 24);
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "Build your dynasty";
        lblSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // btnNewGame
        btnNewGame.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnNewGame.Location = new System.Drawing.Point(140, 160);
        btnNewGame.Name = "btnNewGame";
        btnNewGame.Size = new System.Drawing.Size(200, 52);
        btnNewGame.TabIndex = 2;
        btnNewGame.Text = "New Game";
        btnNewGame.UseVisualStyleBackColor = true;
        btnNewGame.Click += new System.EventHandler(btnNewGame_Click);

        // btnContinueGame
        btnContinueGame.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        btnContinueGame.Location = new System.Drawing.Point(140, 228);
        btnContinueGame.Name = "btnContinueGame";
        btnContinueGame.Size = new System.Drawing.Size(200, 52);
        btnContinueGame.TabIndex = 3;
        btnContinueGame.Text = "Continue Game";
        btnContinueGame.UseVisualStyleBackColor = true;
        btnContinueGame.Click += new System.EventHandler(btnContinueGame_Click);

        // pnlCenter
        pnlCenter.BackColor = System.Drawing.Color.FromArgb(30, 80, 40);
        pnlCenter.Controls.Add(lblTitle);
        pnlCenter.Controls.Add(lblSubtitle);
        pnlCenter.Controls.Add(btnNewGame);
        pnlCenter.Controls.Add(btnContinueGame);
        pnlCenter.Dock = System.Windows.Forms.DockStyle.Fill;
        pnlCenter.Location = new System.Drawing.Point(0, 0);
        pnlCenter.Name = "pnlCenter";
        pnlCenter.Size = new System.Drawing.Size(480, 320);
        pnlCenter.TabIndex = 0;

        // LandingForm
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        BackColor = System.Drawing.Color.FromArgb(30, 80, 40);
        ClientSize = new System.Drawing.Size(480, 320);
        Controls.Add(pnlCenter);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "LandingForm";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "The Manager";
        pnlCenter.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.Label lblSubtitle;
    private System.Windows.Forms.Button btnNewGame;
    private System.Windows.Forms.Button btnContinueGame;
    private System.Windows.Forms.Panel pnlCenter;
}
