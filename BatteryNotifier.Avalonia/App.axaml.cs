using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using BatteryNotifier.Avalonia.Services;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Avalonia.Views;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Avalonia;

public class App : Application
{
    private TrayIconService? _trayIconService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Register bundled sounds resolver so Core can resolve "bundled:" prefixed paths
        Core.Managers.BuiltInSounds.ExternalResolver = settingsValue =>
            BundledSounds.IsBundled(settingsValue) ? BundledSounds.Resolve(settingsValue) : null;

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
                    AssetUris.Logo48);
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
                    using var dockIconStream = AssetLoader.Open(
                        AssetUris.Logo128);
                    using var ms = new System.IO.MemoryStream();
                    dockIconStream.CopyTo(ms);
                    MacOSDockIconHelper.SetDockIcon(ms.ToArray());
            }

            desktop.MainWindow = mainWindow;

            // Startup registration is handled in SettingsViewModel when the user toggles it.
            // Do NOT call SetStartup(true) here — it re-registers on every launch,
            // and on macOS the launchctl load would spawn a duplicate instance.

            // Check for crash from previous session
            _ = CheckForPreviousCrash(mainWindow);

            // Extract notification icon to disk (must happen on UI thread while AssetLoader is available)
            NotificationPlatformService.Initialize();

            // Initialize tray icon
            _trayIconService = new TrayIconService();
            _trayIconService.Initialize();

            // App always starts hidden in tray — user opens via tray icon "Show Window".
            desktop.MainWindow.ShowInTaskbar = false;
            desktop.MainWindow.Hide();
            MacOSDockIconHelper.HideDockIcon();

            // Hide to tray on window close (not actually close)
            desktop.MainWindow.Closing += (_, args) =>
            {
                args.Cancel = true;
                desktop.MainWindow.Hide();
                MacOSDockIconHelper.HideDockIcon();
            };

            desktop.Exit += (_, _) =>
            {
                _trayIconService?.Dispose();
                settings.Save();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task CheckForPreviousCrash(Window mainWindow)
    {
        var crashDetails = CrashReporter.DetectPreviousCrash();
        if (crashDetails == null) return;

        try
        {
            // Show dialog asking user if they want to send the crash report
            var dialog = new Window
            {
                Title = $"{Core.Constants.AppName} — Crash Detected",
                Width = 460,
                Height = 200,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = BuildCrashDialogContent(crashDetails)
            };

            await dialog.ShowDialog(mainWindow).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            BatteryNotifierAppLogger.Error(ex, "Failed to show crash dialog");
        }
    }

    private static object BuildCrashDialogContent(string crashDetails)
    {
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };

        panel.Children.Add(new TextBlock
        {
            Text = $"{Core.Constants.AppName} crashed during the last session.",
            FontSize = 14,
            FontWeight = global::Avalonia.Media.FontWeight.SemiBold,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Would you like to send a crash report to help fix the issue? " +
                   "A GitHub issue will open in your browser for review before submitting. " +
                   "Personal information (usernames, paths) is removed from logs.",
            FontSize = 12,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            Opacity = 0.8
        });

        var buttonPanel = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // Close helper — TopLevel.GetTopLevel works where VisualRoot is protected
        static void CloseParentWindow(Button? btn)
        {
            if (btn != null && TopLevel.GetTopLevel(btn) is Window w) w.Close();
        }

        var dismissButton = new Button { Content = "Dismiss", MinWidth = 80 };
        dismissButton.Click += (s, _) => CloseParentWindow(s as Button);

        var saveButton = new Button { Content = "Save to File", MinWidth = 100 };
        saveButton.Click += (s, _) =>
        {
            var report = CrashReporter.BuildCrashReport(crashDetails);
            var path = CrashReporter.SaveReportToFile(report);
            if (!string.IsNullOrEmpty(path))
            {
                    var dir = System.IO.Path.GetDirectoryName(path)!;
                    if (OperatingSystem.IsMacOS())
                    {
                        using var p = System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo(Core.Constants.ResolveCommand("open")) { ArgumentList = { "-R", path } });
                    }
                    else if (OperatingSystem.IsWindows())
                    {
                        using var p = System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo(Core.Constants.ResolveCommand("explorer")) { ArgumentList = { "/select,", path } });
                    }
                    else
                    {
                        using var p = System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo(Core.Constants.ResolveCommand("xdg-open")) { ArgumentList = { dir } });
                    }
            }
            CloseParentWindow(s as Button);
        };

        buttonPanel.Children.Add(dismissButton);
        buttonPanel.Children.Add(saveButton);
        panel.Children.Add(buttonPanel);

        return panel;
    }
}
