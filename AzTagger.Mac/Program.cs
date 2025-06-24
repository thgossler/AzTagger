// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using Eto.Forms;
using Eto.Mac;
using AzTagger.App;
using AzTagger.Services;

namespace AzTagger.Mac;

public static class Program
{
    public static void Main(string[] args)
    {
        LoggingService.Initialize();
        try
        {
            var platform = new Eto.Mac.Platform();
            var app = new Application(platform);
            app.Terminating += (_, _) => LoggingService.CloseAndFlush();
            
            // Create the main form and set it as the application's main form
            // This ensures the application quits when the main form is closed
            var mainForm = new MainForm();
            app.MainForm = mainForm;
            
            // Enable closing the main form to quit the application on macOS
            var handler = app.Handler as Eto.Mac.Forms.ApplicationHandler;
            if (handler != null)
            {
                handler.AllowClosingMainForm = true;
            }
            
            app.Run(mainForm);
        }
        catch (Exception ex)
        {
            LoggingService.LogFatal(ex, "Unhandled exception");
            MessageBox.Show("An unexpected error occurred. See the log for details.", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }
}
