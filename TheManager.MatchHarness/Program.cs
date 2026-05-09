namespace TheManager.MatchHarness;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MatchHarnessForm());
    }
}
