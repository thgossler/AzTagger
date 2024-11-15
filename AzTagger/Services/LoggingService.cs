// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using Serilog;
using System;
using System.IO;

namespace AzTagger.Services;

public class LoggingService
{
    private static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzTagger", "errorlog.txt");

    public static void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                LogFilePath,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
                rollOnFileSizeLimit: true)
            .CreateLogger();
    }

    public static void LogError(Exception ex, string message)
    {
        Log.Error(ex, message);
    }

    public static void LogFatal(Exception ex, string message)
    {
        Log.Fatal(ex, message);
    }

    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}
