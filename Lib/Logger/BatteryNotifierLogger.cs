using System;
using Serilog;

namespace BatteryNotifier.Lib.Logger;

public static class BatteryNotifierAppLogger
{
    // Cache loggers for better performance
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ILogger> _loggers = new();

    /// <summary>
    /// Get a logger for a specific class/context
    /// </summary>
    public static ILogger ForContext<T>()
    {
        var typeName = typeof(T).Name;
        return _loggers.GetOrAdd(typeName, _ => Log.ForContext<T>());
    }

    /// <summary>
    /// Get a logger for a specific context name
    /// </summary>
    public static ILogger ForContext(string contextName)
    {
        return _loggers.GetOrAdd(contextName, _ => Log.ForContext("SourceContext", contextName));
    }

    // Convenience methods for quick logging
    public static void Debug(string message) => Log.Debug(message);
    public static void Debug(string template, params object[] args) => Log.Debug(template, args);
        
    public static void Info(string message) => Log.Information(message);
    public static void Info(string template, params object[] args) => Log.Information(template, args);
        
    public static void Warning(string message) => Log.Warning(message);
    public static void Warning(string template, params object[] args) => Log.Warning(template, args);
        
    public static void Error(string message) => Log.Error(message);
    public static void Error(Exception ex, string message) => Log.Error(ex, message);
    public static void Error(Exception ex, string template, params object[] args) => Log.Error(ex, template, args);
        
    public static void Fatal(string message) => Log.Fatal(message);
    public static void Fatal(Exception ex, string message) => Log.Fatal(ex, message);
    public static void Fatal(Exception ex, string template, params object[] args) => Log.Fatal(ex, template, args);

    // Structured logging for user actions
    public static void LogUserAction(string action, string details = null, object additionalData = null)
    {
        Log.Information("User Action: {Action} {Details} {@Data}", action, details, additionalData);
    }

    // Application lifecycle logging
    public static void LogStartup() => Log.Information("=== APPLICATION STARTUP ===");
    public static void LogShutdown() => Log.Information("=== APPLICATION SHUTDOWN ===");
}