using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
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

            // Set shutdown mode to explicit — app stays alive when window is hidden
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            // Load a platform-appropriate icon size for the window title bar.
            try
            {
                using var iconStream = AssetLoader.Open(
                    new Uri("avares://BatteryNotifier/Assets/battery-notifier-logo-48.png"));
                mainWindow.Icon = new WindowIcon(iconStream);
            }
            catch
            {
                // Falls back to the .ico set in XAML
            }

            // On macOS, Window.Icon only sets the title bar icon.
            // The Dock icon requires NSApplication.shared.applicationIconImage.
            if (OperatingSystem.IsMacOS())
            {
                try
                {
                    using var dockIconStream = AssetLoader.Open(
                        new Uri("avares://BatteryNotifier/Assets/battery-notifier-logo-128.png"));
                    using var ms = new System.IO.MemoryStream();
                    dockIconStream.CopyTo(ms);
                    MacOSDockIconHelper.SetDockIcon(ms.ToArray());
                }
                catch
                {
                    // Non-critical — generic Dock icon stays
                }
            }

            desktop.MainWindow = mainWindow;

            // Startup registration is handled in SettingsViewModel when the user toggles it.
            // Do NOT call SetStartup(true) here — it re-registers on every launch,
            // and on macOS the launchctl load would spawn a duplicate instance.

            // Extract notification icon to disk (must happen on UI thread while AssetLoader is available)
            NotificationPlatformService.Initialize();

            // Initialize tray icon
            _trayIconService = new TrayIconService();
            _trayIconService.Initialize();

            // Start minimized: hide directly to tray without showing the window.
            // Setting WindowState.Minimized first causes a brief Dock bounce on macOS.
            if (settings.StartMinimized)
            {
                desktop.MainWindow.ShowInTaskbar = false;
                desktop.MainWindow.Hide();
            }

            // Hide to tray on window close (not actually close)
            desktop.MainWindow.Closing += (s, e) =>
            {
                e.Cancel = true;
                desktop.MainWindow.Hide();
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
