// Copyright (c) Thomas Gossler, 2024. All rights reserved.
// Licensed under the MIT license.

using AzTagger.Models;
using AzTagger.Services;
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
            var initSettingsRequired = !File.Exists(SettingsService.SettingsFilePath);
            var settings = SettingsService.Load();
            if (initSettingsRequired)
            {
                SettingsService.Save(settings);
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
