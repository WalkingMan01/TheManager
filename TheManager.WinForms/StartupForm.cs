using TheManager.Models;

namespace TheManager.WinForms;

/// <summary>Landing page shown on application startup. Entry point for starting or loading a game.</summary>
public partial class StartupForm : Form
{
    /// <summary>Initialises the landing form and its designer-created controls.</summary>
    public StartupForm()
    {
        InitializeComponent();
    }

    private void btnNewGame_Click(object sender, EventArgs e)
    {
        OpenMainForm(new GameState());
    }

    private void btnContinueGame_Click(object sender, EventArgs e)
    {
        // TODO: open save-game browser and load persisted GameState
        OpenMainForm(new GameState());
    }

    private void OpenMainForm(GameState state)
    {
        Hide();
        var mainForm = new MainForm(state);
        mainForm.FormClosed += (_, _) => Show();
        mainForm.Show();
    }
}
