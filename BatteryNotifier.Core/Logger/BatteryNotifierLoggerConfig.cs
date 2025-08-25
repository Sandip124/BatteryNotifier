using Serilog;
using Serilog.Events;

namespace BatteryNotifier.Core.Logger;

public static class BatteryNotifierLoggerConfig
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BatteryNotifier", "Logs");

    public static void InitializeLogger()
    {
        Directory.CreateDirectory(LogDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithProperty("Application", "BatteryNotifier")
            .Enrich.WithProperty("Version", Constants.ApplicationVersion)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .WriteTo.File(
                path: Path.Combine(LogDirectory, "app-.log"),
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 50 * 1024 * 1024,
                retainedFileCountLimit: 30,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{ThreadId:D3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                buffered: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1)
            )
            .WriteTo.File(
                path: Path.Combine(LogDirectory, "errors-.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                retainedFileCountLimit: 90,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{ThreadId:D3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                buffered: true
            )

#if DEBUG
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] [{ThreadId:D3}] {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.Debug()
#endif

            .CreateLogger();

        Log.Information("Logger initialized. Log directory: {LogDirectory}", LogDirectory);
    }

    public static void ShutdownLogger()
    {
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
    }

    public static string GetLogDirectory() => LogDirectory;
}