// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using Serilog;
using System;
using System.IO;

namespace AzTagger.Services;

public class LoggingService
{
    public static readonly string LogDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzTagger");

    private static readonly string LogFilePath = Path.Combine(LogDirectory, "errorlog.txt");

    public static void Initialize()
    {
        var logDir = Path.GetDirectoryName(LogFilePath);
        if (!string.IsNullOrEmpty(logDir))
            Directory.CreateDirectory(logDir);
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

    public static string GetLatestLogFile()
    {
        if (!Directory.Exists(LogDirectory))
            return string.Empty;
        var files = Directory.GetFiles(LogDirectory, "errorlog*.txt");
        if (files.Length == 0)
            return string.Empty;
        Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
        return files[0];
    }
}
