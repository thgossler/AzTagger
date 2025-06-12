using System;
using Eto.Forms;
using Eto.GtkSharp;
using AzTagger.App;
using AzTagger.Services;

namespace AzTagger.Gtk;

public static class Program
{
    public static void Main(string[] args)
    {
        LoggingService.Initialize();
        try
        {
            var platform = new Eto.GtkSharp.Platform();
            var app = new Application(platform);
            app.Terminating += (_, _) => LoggingService.CloseAndFlush();
            
            // Create the main form and set it as the application's main form
            // This ensures the application quits when the main form is closed
            var mainForm = new MainForm();
            app.MainForm = mainForm;
            
            app.Run(mainForm);
        }
        catch (Exception ex)
        {
            LoggingService.LogFatal(ex, "Unhandled exception");
            MessageBox.Show("An unexpected error occurred. See the log for details.", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }
}
