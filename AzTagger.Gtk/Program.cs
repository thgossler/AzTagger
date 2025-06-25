// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Eto.Forms;
using Eto.GtkSharp;
using AzTagger.App;
using AzTagger.Services;

namespace AzTagger.Gtk;

public static class Program
{
    // P/Invoke declarations for GTK icon handling (Linux)
    [DllImport("gtk-3", CallingConvention = CallingConvention.Cdecl)]
    private static extern void gtk_window_set_default_icon_name(string icon_name);
    
    [DllImport("gtk-3", CallingConvention = CallingConvention.Cdecl)]
    private static extern void gtk_window_set_default_icon_from_file(string filename, out IntPtr error);

    public static void Main(string[] args)
    {
        LoggingService.Initialize();
        try
        {
            var platform = new Eto.GtkSharp.Platform();
            var app = new Application(platform);
            app.Terminating += (_, _) => LoggingService.CloseAndFlush();
            
            // Set GTK application icon - works on Linux
            SetGtkApplicationIcon();
            
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
    
    private static void SetGtkApplicationIcon()
    {
        try
        {
            // Find icon file - try multiple locations
            string iconPath = null;
            
            // First try the base directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidatePaths = new[]
            {
                Path.Combine(baseDir, "icon.png"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "images", "icon.png"))
            };
            
            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    iconPath = path;
                    break;
                }
            }
            
            if (iconPath == null)
            {
                LoggingService.LogInfo("No icon file found for GTK application icon");
                return;
            }
            
            // Use GTK to set the default window icon
            try
            {
                gtk_window_set_default_icon_from_file(iconPath, out var error);
                
                if (error == IntPtr.Zero)
                {
                    LoggingService.LogInfo($"Successfully set GTK application icon from: {iconPath}");
                }
                else
                {
                    LoggingService.LogError(new Exception("GTK icon error"), $"Failed to set GTK icon from: {iconPath}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, $"Exception setting GTK icon from: {iconPath}");
                
                // Fallback: try to set a generic icon name that might be available
                try
                {
                    gtk_window_set_default_icon_name("application-default-icon");
                    LoggingService.LogInfo("Set fallback GTK icon name");
                }
                catch (Exception fallbackEx)
                {
                    LoggingService.LogError(fallbackEx, "Failed to set fallback GTK icon");
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Failed to set GTK application icon");
        }
    }
}
