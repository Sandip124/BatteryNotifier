using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading;

namespace BatteryNotifier.Avalonia;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, "BatteryNotifier_SingleInstance_A7F2C3D4", out bool isNew);

        if (!isNew)
        {
            Console.WriteLine("BatteryNotifier is already running.");
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
