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
            app.Run(new MainForm());
        }
        catch (Exception ex)
        {
            LoggingService.LogFatal(ex, "Unhandled exception");
            MessageBox.Show("An unexpected error occurred. See the log for details.", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }
}
