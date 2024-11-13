using System;
using System.IO;
using System.Windows.Forms;
using Serilog;
using System.Text.Json;

namespace AzTagger
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Initialize Serilog for logging
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzTagger", "errorlog.txt"),
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
                    rollOnFileSizeLimit: true)
                .CreateLogger();

            try
            {
                // Load settings from settings.json
                var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzTagger", "settings.json");
                Settings settings;
                if (File.Exists(settingsFilePath))
                {
                    var settingsJson = File.ReadAllText(settingsFilePath);
                    settings = JsonSerializer.Deserialize<Settings>(settingsJson);
                }
                else
                {
                    settings = new Settings();
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(settings));
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unhandled exception occurred.");
                MessageBox.Show("An unexpected error occurred. Please check the error log for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
