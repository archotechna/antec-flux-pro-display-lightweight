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
        bool createdNew;

        // use a mutex to only ever allow 1 instance of FluxProDisplay
        using (new Mutex(true, MutexName, out createdNew))
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
}