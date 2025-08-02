using System;
using System.IO;
using System.Windows.Forms;
using Serilog;
using Serilog.Events;

namespace BatteryNotifier.Lib.Logger;

public static class BatteryNotifierLoggerConfig
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BatteryNotifier", "Logs");

    public static void InitializeLogger()
    {
        // Ensure log directory exists
        Directory.CreateDirectory(LogDirectory);

        // Configure Serilog with multiple sinks for performance and reliability
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)

            // Enrich logs with additional information
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithProperty("Application", "BatteryNotifier")
            .Enrich.WithProperty("Version", Application.ProductVersion)
            .Enrich.WithProperty("MachineName", Environment.MachineName)

            // File sink - Main application logs (high performance with buffering)
            .WriteTo.File(
                path: Path.Combine(LogDirectory, "app-.log"),
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 50 * 1024 * 1024, // 50MB per file
                retainedFileCountLimit: 30, // Keep 30 days
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{ThreadId:D3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                buffered: true, // Critical for performance
                flushToDiskInterval: TimeSpan.FromSeconds(1) // Flush every second
            )

            // Error-only file sink for quick error analysis
            .WriteTo.File(
                path: Path.Combine(LogDirectory, "errors-.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB per file
                retainedFileCountLimit: 90, // Keep 90 days of errors
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{ThreadId:D3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                buffered: true
            )

            // Console sink for development (remove in production)
#if DEBUG
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] [{ThreadId:D3}] {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.Debug()
#endif

            .CreateLogger();

        // Log the initialization
        Log.Information("Logger initialized. Log directory: {LogDirectory}", LogDirectory);
    }

    public static void ShutdownLogger()
    {
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
    }

    public static string GetLogDirectory() => LogDirectory;
}