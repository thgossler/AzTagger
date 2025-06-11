using System;
using Eto.Forms;
using Eto.Wpf;
using AzTagger.App;
using AzTagger.Services;

namespace AzTagger.Wpf;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        LoggingService.Initialize();
        try
        {
            var platform = new Eto.Wpf.Platform();
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
