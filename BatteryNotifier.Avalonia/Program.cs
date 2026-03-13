using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Services;
using ReactiveUI;
using Velopack;

namespace BatteryNotifier.Avalonia;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack must run FIRST — handles install/uninstall/update hooks
        // and exits the process early when invoked by the installer.
        VelopackApp.Build().Run();

        // Initialize logging FIRST — before any other code can log
        BatteryNotifierLoggerConfig.InitializeLogger();
        BatteryNotifierAppLogger.LogStartup();

        using var mutex = new Mutex(true, "BatteryNotifier_SingleInstance_A7F2C3D4", out bool isNew);

        if (!isNew)
        {
            Console.WriteLine("BatteryNotifier is already running.");
            return;
        }

        // Global exception handlers — catch anything that escapes try/catch blocks
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            BatteryNotifierAppLogger.Fatal(
                e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()),
                "Unhandled AppDomain exception (terminating: {IsTerminating})", e.IsTerminating);
            CrashReporter.WriteCrashMarker(e.ExceptionObject as Exception);
            BatteryNotifierLoggerConfig.ShutdownLogger();
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            BatteryNotifierAppLogger.Error(e.Exception, "Unobserved task exception");
            e.SetObserved(); // Prevent process termination
        };

        // ReactiveUI exception handler — catches unhandled exceptions in reactive pipelines
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
        {
            BatteryNotifierAppLogger.Error(ex, "Unhandled exception in reactive pipeline");
        });

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            BatteryNotifierAppLogger.Fatal(ex, "Fatal exception in application lifetime");
            CrashReporter.WriteCrashMarker(ex);
            throw;
        }
        finally
        {
            BatteryNotifierAppLogger.LogShutdown();
            BatteryNotifierLoggerConfig.ShutdownLogger();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}
