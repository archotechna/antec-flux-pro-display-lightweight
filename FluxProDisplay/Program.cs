using FluxProDisplay.DTOs.AppSettings;
using Microsoft.Extensions.Configuration;

namespace FluxProDisplay;

internal static class Program
{
    // name of the mutex
    private const string MutexName = "FluxProDisplay_SingleInstance_Mutex";

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
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
                
                // set up appsettings configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var rootConfig = configuration.Get<RootConfig>();
                
                ApplicationConfiguration.Initialize();
                Application.Run(new FluxProDisplayTray(rootConfig!));
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