namespace TheManager.WinForms;

/// <summary>UserControl shown on startup. Lets the user start a new game or continue a saved one.</summary>
public partial class HomeView : UserControl
{
    /// <summary>Raised when the user clicks New Game.</summary>
    public event EventHandler? NewGameRequested;

    /// <summary>Raised when the user clicks Continue Game.</summary>
    public event EventHandler? ContinueGameRequested;

    /// <summary>Initialises the home view.</summary>
    public HomeView()
    {
        InitializeComponent();
    }

    private void btnNewGame_Click(object sender, EventArgs e)
        => NewGameRequested?.Invoke(this, EventArgs.Empty);

    private void btnContinueGame_Click(object sender, EventArgs e)
        => ContinueGameRequested?.Invoke(this, EventArgs.Empty);
}
