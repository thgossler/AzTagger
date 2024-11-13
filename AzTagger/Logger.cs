using System;
using System.IO;
using Serilog;

namespace AzTagger
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AzTagger",
            "errorlog.txt");

        static Logger()
        {
            var directory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(LogFilePath, rollingInterval: RollingInterval.Day, fileSizeLimitBytes: 10 * 1024 * 1024, rollOnFileSizeLimit: true)
                .CreateLogger();
        }

        public static void LogError(Exception ex)
        {
            Log.Error(ex, "An error occurred");
        }

        public static void LogInformation(string message)
        {
            Log.Information(message);
        }
    }
}
