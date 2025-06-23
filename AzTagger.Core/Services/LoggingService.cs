// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using Serilog;
using System;
using System.IO;
using System.Text;
using Serilog.Core;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Linq;

namespace AzTagger.Services
{
    public class LoggingService
    {
        public static readonly string LogDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzTagger");

        private static readonly string LogFilePath = Path.Combine(LogDirectory, "errorlog.txt");
        private static readonly string DiagnosticLogPath = Path.Combine(LogDirectory, "diagnostic.txt");
        private static Logger _logger;
        private static readonly object _lockObject = new object();
        private static bool _isInitialized = false;
        private static FileStream _crashLogStream;
        private static StreamWriter _crashLogWriter;
        private static Timer _heartbeatTimer;
        private static volatile bool _applicationClosing = false;

        // Immediate, unbuffered logging for critical debugging
        public static void WriteImmediateLog(string message, string category = "DEBUG")
        {
            try
            {
                lock (_lockObject)
                {
                    var logDir = Path.GetDirectoryName(DiagnosticLogPath);
                    if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    
                    var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{category}] {message}{Environment.NewLine}";
                    
                    // Write directly with FileStream for immediate disk write
                    using (var fs = new FileStream(DiagnosticLogPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(fs, Encoding.UTF8))
                    {
                        writer.Write(logEntry);
                        writer.Flush();
                        fs.Flush(true); // Force OS to write to disk immediately
                    }
                }
            }
            catch 
            { 
                // If this fails, try a different file
                try
                {
                    var backupPath = Path.Combine(Path.GetTempPath(), $"aztagger-crash-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt");
                    File.WriteAllText(backupPath, $"[{DateTime.UtcNow}] BACKUP LOG: {message}");
                }
                catch { /* Last resort - can't do anything */ }
            }
        }

        public static void Initialize()
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                    return;

                try
                {
                    //SetupCrashLogging();
                    SetupLogging();
                    RegisterGlobalHandlers();
                    //StartHeartbeat();
                    
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    WriteEmergencyLog($"FATAL: Failed to initialize logging: {ex}");
                    throw;
                }
            }
        }

        private static void SetupCrashLogging()
        {
            try
            {
                var crashLogPath = Path.Combine(LogDirectory, "crash-monitor.log");
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);
                    
                _crashLogStream = new FileStream(crashLogPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                _crashLogWriter = new StreamWriter(_crashLogStream, Encoding.UTF8) { AutoFlush = true };
                
                WriteCrashLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] STARTUP: Crash monitoring started (PID: {Process.GetCurrentProcess().Id})");
            }
            catch (Exception ex)
            {
                WriteEmergencyLog($"Failed to setup crash logging: {ex}");
            }
        }

        private static void StartHeartbeat()
        {
            try
            {
                _heartbeatTimer = new Timer(HeartbeatCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed to start heartbeat timer");
            }
        }

        private static void HeartbeatCallback(object state)
        {
            if (_applicationClosing) return;
            
            try
            {
                WriteCrashLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] HEARTBEAT: Application alive (Thread: {Thread.CurrentThread.ManagedThreadId})");
            }
            catch
            {
                // Ignore heartbeat failures
            }
        }

        private static void WriteCrashLog(string message)
        {
            try
            {
                _crashLogWriter?.WriteLine(message);
                _crashLogWriter?.Flush();
            }
            catch
            {
                // Emergency fallback
                try
                {
                    var fallbackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "aztagger-crash.log");
                    File.AppendAllText(fallbackPath, message + Environment.NewLine);
                }
                catch
                {
                    // Give up
                }
            }
        }

        private static void SetupLogging()
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            _logger = new LoggerConfiguration()
                .WriteTo.File(LogFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger();
        }

        private static void RegisterGlobalHandlers()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    _applicationClosing = true;
                    var message = $"FATAL: Unhandled exception: {e.ExceptionObject}";
                    WriteEmergencyLog(message);
                    WriteCrashLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] UNHANDLED_EXCEPTION: {message}");
                    FlushAllLogs();
                };

                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    _applicationClosing = true;
                    WriteCrashLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] PROCESS_EXIT: Application exiting normally");
                    LogInfo("Application process exiting");
                    FlushAllLogs();
                };

                // Add first chance exception handler for early detection
                AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
                {
                    if (e.Exception is ArgumentOutOfRangeException aoore)
                    {
                        try
                        {
                            var detailedMessage = $"FIRST_CHANCE_AOORE: {aoore.Message}\n" +
                                                $"Parameter: {aoore.ParamName}\n" +
                                                $"Actual Value: {aoore.ActualValue}\n" +
                                                $"Thread: {Thread.CurrentThread.ManagedThreadId} ({Thread.CurrentThread.Name ?? "Unnamed"})\n" +
                                                $"Full Exception: {aoore}\n" +
                                                $"Stack Trace:\n{aoore.StackTrace}";
                            
                            WriteCrashLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {detailedMessage}");
                            LogError(aoore, $"First chance ArgumentOutOfRangeException - Param: {aoore.ParamName}, Value: {aoore.ActualValue}");
                            WriteImmediateLog(detailedMessage, "FIRST_CHANCE_AOORE");
                            
                            // Also log current call stack context
                            var stackTrace = Environment.StackTrace;
                            WriteImmediateLog($"Current Stack Context:\n{stackTrace}", "STACK_CONTEXT");
                        }
                        catch
                        {
                            // Emergency fallback
                            WriteCrashLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] FIRST_CHANCE_AOORE_BASIC: {e.Exception}");
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                WriteEmergencyLog($"Failed to register global handlers: {ex}");
            }
        }

        public static void LogCrashContext(string context, Exception ex = null)
        {
            var message = $"CRASH_CONTEXT: {context}";
            if (ex != null) message += $" - Exception: {ex}";
            
            LogError(ex, message);
            WriteImmediateLog(message, "CRASH_CONTEXT");
            WriteCrashLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            
            // Log current call stack for debugging
            try
            {
                var stackTrace = Environment.StackTrace;
                WriteImmediateLog($"Call Stack at Context:\n{stackTrace}", "STACK_TRACE");
                WriteCrashLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] CALL_STACK: {stackTrace.Replace('\n', ' ').Replace('\r', ' ')}");
            }
            catch
            {
                // Ignore stack trace failures
            }
            
            // Also log memory and thread info
            try
            {
                var process = Process.GetCurrentProcess();
                var memInfo = $"Memory: Working={process.WorkingSet64 / 1024 / 1024}MB, Private={process.PrivateMemorySize64 / 1024 / 1024}MB";
                var threadInfo = $"Threads: {process.Threads.Count}, Current: {Thread.CurrentThread.ManagedThreadId} ({Thread.CurrentThread.Name ?? "Unnamed"})";
                
                WriteCrashLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] DIAGNOSTICS: {memInfo}, {threadInfo}");
                WriteImmediateLog($"Process Diagnostics: {memInfo}, {threadInfo}", "DIAGNOSTICS");
            }
            catch
            {
                // Ignore diagnostic failures
            }
        }

        private static void FlushAllLogs()
        {
            try
            {
                _heartbeatTimer?.Dispose();
                FlushLogs();
                _crashLogWriter?.Flush();
                _crashLogStream?.Flush();
            }
            catch
            {
                // Best effort
            }
        }

        public static void LogInfo(string message)
        {
            _logger?.Information(message);
        }

        public static void LogError(Exception ex, string message)
        {
            _logger?.Error(ex, message);
            FlushLogs();
        }

        public static void LogFatal(Exception ex, string message)
        {
            _logger?.Fatal(ex, message);
            WriteEmergencyLog($"FATAL: {message}\nException: {ex}\nStack: {ex.StackTrace}");
            FlushLogs();
        }

        public static void LogCriticalError(Exception ex, string message)
        {
            _logger?.Fatal(ex, message);
            WriteEmergencyLog($"CRITICAL: {message}\nException: {ex}\nStack: {ex.StackTrace}");
            FlushLogs();
        }

        public static void FlushLogs()
        {
            try
            {
                (_logger as IDisposable)?.Dispose();
                SetupLogging();
            }
            catch (Exception ex)
            {
                WriteEmergencyLog($"Error during log flush: {ex}");
            }
        }

        public static void CloseAndFlush()
        {
            try
            {
                (_logger as IDisposable)?.Dispose();
            }
            catch (Exception ex)
            {
                WriteEmergencyLog($"Error during logging shutdown: {ex}");
            }
        }

        public static void WriteEmergencyLog(string message)
        {
            try
            {
                var emergencyPath = Path.Combine(LogDirectory, "emergency.log");
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);
                
                var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] EMERGENCY: {message}{Environment.NewLine}";
                File.AppendAllText(emergencyPath, logEntry);
            }
            catch
            {
                // Ultimate fallback
                try
                {
                    var desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "aztagger-emergency.log");
                    var logEntry = $"[{DateTime.UtcNow}] EMERGENCY: {message}{Environment.NewLine}";
                    File.AppendAllText(desktopPath, logEntry);
                }
                catch { /* Ultimate fallback failed */ }
            }
        }

        public static void Shutdown()
        {
            _applicationClosing = true;
            WriteCrashLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] SHUTDOWN: Logging service shutting down");
            
            try
            {
                FlushAllLogs();
                CloseAndFlush();
                
                _crashLogWriter?.Dispose();
                _crashLogStream?.Dispose();
            }
            catch (Exception ex)
            {
                WriteEmergencyLog($"Error during logging shutdown: {ex}");
            }
        }

        public static string GetLatestLogFile()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    return null;

                var logFiles = Directory.GetFiles(LogDirectory, "errorlog*.txt")
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .ToArray();

                return logFiles.Length > 0 ? logFiles[0] : null;
            }
            catch
            {
                return LogFilePath; // Fallback to expected path
            }
        }
    }
}
