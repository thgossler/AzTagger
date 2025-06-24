// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

#nullable enable

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
    // P/Invoke declarations for macOS dock icon
    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
    private static extern IntPtr objc_getClass(string className);
    
    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);
    
    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);
    
    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
    private static extern IntPtr sel_registerName(string selectorName);
    
    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, string arg1);

    public static void Main(string[] args)
    {
        LoggingService.Initialize();
        try
        {
            var platform = new Eto.GtkSharp.Platform();
            var app = new Application(platform);
            app.Terminating += (_, _) => LoggingService.CloseAndFlush();
            
            // Set macOS dock icon if running on macOS
            // Temporarily disabled due to P/Invoke issues - will be improved in future version
            // if (Environment.OSVersion.Platform == PlatformID.Unix && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            // {
            //     SetMacOSDockIcon();
            // }
            
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
    
    private static void SetMacOSDockIcon()
    {
        try
        {
            // Find icon file - try multiple locations
            string? iconPath = null;
            
            // First try the base directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidatePaths = new[]
            {
                Path.Combine(baseDir, "Icon.icns"),
                Path.Combine(baseDir, "icon.icns"),
                Path.Combine(baseDir, "icon.png"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "images", "Icon.icns")),
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
                LoggingService.LogInfo("No icon file found for macOS dock icon");
                return;
            }
            
            // Use Objective-C runtime to set the dock icon
            var nsApplicationClass = objc_getClass("NSApplication");
            var sharedApplicationSel = sel_registerName("sharedApplication");
            var nsApp = objc_msgSend(nsApplicationClass, sharedApplicationSel);
            
            var nsImageClass = objc_getClass("NSImage");
            var allocSel = sel_registerName("alloc");
            var initWithContentsOfFileSel = sel_registerName("initWithContentsOfFile:");
            var nsStringClass = objc_getClass("NSString");
            var stringWithUTF8StringSel = sel_registerName("stringWithUTF8String:");
            
            // Create NSString for the file path
            var nsIconPath = objc_msgSend(nsStringClass, stringWithUTF8StringSel, iconPath);
            
            // Create NSImage instance
            var nsImageInstance = objc_msgSend(nsImageClass, allocSel);
            var nsImage = objc_msgSend(nsImageInstance, initWithContentsOfFileSel, nsIconPath);
            
            if (nsImage != IntPtr.Zero)
            {
                // Set the application icon
                var setApplicationIconImageSel = sel_registerName("setApplicationIconImage:");
                objc_msgSend(nsApp, setApplicationIconImageSel, nsImage);
                
                LoggingService.LogInfo($"Successfully set macOS dock icon from: {iconPath}");
            }
            else
            {
                LoggingService.LogInfo($"Failed to create NSImage from: {iconPath}");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Failed to set macOS dock icon");
        }
    }
}
