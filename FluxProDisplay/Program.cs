namespace FluxProDisplay;

static class Program
{
    // name of the mutex
    private static readonly string MutexName = "FluxProDisplay_SingleInstance_Mutex";

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        try
        {
            using (new Mutex(true, MutexName, out var createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "Another instance of FluxProDisplay is already running.",
                        "Instance Running",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                ApplicationConfiguration.Initialize();
                Application.Run(new FluxProDisplayTray());
            }
        }
        catch (Exception ex)
        {
            // debug logs for this are located in %APPDATA%\FluxProDisplay\error.log
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FluxProDisplay",
                "error.log");

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.WriteAllText(logPath, ex.ToString());

            throw;
        }
    }
}