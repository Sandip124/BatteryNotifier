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
        VelopackApp.Build().Run();

        // Initialize logging
        BatteryNotifierLoggerConfig.InitializeLogger();
        BatteryNotifierAppLogger.LogStartup();

        // Write Diaonostic report at startup
        BatteryNotifier.Core.Diagnostics.SystemDiagnostics.WriteReport();

        using var mutex = new Mutex(true, "BatteryNotifier_SingleInstance_A7F2C3D4", out bool isNew);

        if (!isNew)
        {
            Console.WriteLine("BatteryNotifier is already running.");
            return;
        }

        // Global exception handlers
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
            // Avalonia on Linux tries to connect to com.canonical.AppMenu.Registrar
            // (Unity global menu) which doesn't exist on modern GNOME — harmless, suppress it.
            if (e.Exception.InnerException is Tmds.DBus.Protocol.DBusException dbus
                && dbus.ErrorName == "org.freedesktop.DBus.Error.ServiceUnknown")
            {
                e.SetObserved();
                return;
            }

            BatteryNotifierAppLogger.Error(e.Exception, "Unobserved task exception");
            e.SetObserved(); // Prevent process termination
        };

        // ReactiveUI exception handler
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

    public static AppBuilder BuildAvaloniaApp()
    {
        // On Wayland, GNOME's Mutter adds server-side decorations to XWayland windows
        // and ignores _MOTIF_WM_HINTS, so SystemDecorations.None has no effect.
        // Force pure X11 to ensure decoration removal works as expected.
        if (OperatingSystem.IsLinux() &&
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
        {
            Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", null);
        }

        return AppBuilder.Configure<App>()
            .WithInterFont()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
    }
}
