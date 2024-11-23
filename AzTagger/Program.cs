// Copyright (c) Thomas Gossler, 2024. All rights reserved.
// Licensed under the MIT license.

using Serilog;
using System;
using System.IO;
using System.Windows.Forms;

namespace AzTagger;

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
            var initSettingsRequired = !File.Exists(Settings.SettingsFilePath);
            var settings = Settings.Load();
            if (initSettingsRequired)
            {
                settings.Save();
            }

            if (string.IsNullOrWhiteSpace(settings.TenantId))
            {
                MessageBox.Show(
@"Please configure your Microsoft Entra ID TenantId in the settings file and restart the application.

The application may also need to be registered in Entra ID if not done yet with the following permissions and its ClientAppId configured:
- Azure Service Management / Delegated / user_impersonation
- Microsoft Graph / Delegated / User.Read

The settings file will now be opened automatically.",
                    "AzTagger", MessageBoxButtons.OK, MessageBoxIcon.Information);

                var editor = Environment.GetEnvironmentVariable("EDITOR") ?? "notepad";
                System.Diagnostics.Process.Start(editor, Settings.SettingsFilePath);

                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(settings));
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An unhandled exception occurred.");
            MessageBox.Show("An unexpected error occurred. Please check the error log file in the program's AppData Local folder for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
