using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using BatteryNotifier.Avalonia.Services;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Avalonia.Views;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Avalonia;

public partial class App : Application
{
    private TrayIconService? _trayIconService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = AppSettings.Instance;

            // Apply theme from saved settings
            RequestedThemeVariant = settings.ThemeMode switch
            {
                ThemeMode.Light => ThemeVariant.Light,
                ThemeMode.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            // Configure launch at startup
            if (settings.LaunchAtStartup)
            {
                StartupManager.SetStartup(true);
            }

            // Initialize tray icon
            _trayIconService = new TrayIconService();
            _trayIconService.Initialize();

            // Apply start minimized
            if (settings.StartMinimized)
            {
                desktop.MainWindow.WindowState = global::Avalonia.Controls.WindowState.Minimized;
                desktop.MainWindow.Hide();
            }

            // Minimize to tray on close
            desktop.MainWindow.Closing += (s, e) =>
            {
                e.Cancel = true;
                desktop.MainWindow.Hide();

                if (desktop.MainWindow.WindowState == global::Avalonia.Controls.WindowState.Normal)
                {
                    settings.WindowPositionX = (int)desktop.MainWindow.Position.X;
                    settings.WindowPositionY = (int)desktop.MainWindow.Position.Y;
                    settings.Save();
                }
            };

            desktop.Exit += (s, e) =>
            {
                _trayIconService?.Dispose();
                settings.Save();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
