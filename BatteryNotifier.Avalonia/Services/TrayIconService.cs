using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Avalonia.Views;
using BatteryNotifier.Core;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Store;
using Serilog;

namespace BatteryNotifier.Avalonia.Services;

public class TrayIconService : IDisposable
{
    private readonly ILogger _logger;
    private TrayIcon? _trayIcon;
    private NativeMenu? _trayMenu;
    private NotificationManager? _notificationManager;
    private bool _disposed;

    public TrayIconService()
    {
        _logger = BatteryNotifierAppLogger.ForContext<TrayIconService>();
    }

    public void Initialize()
    {
        try
        {
            _trayIcon = new TrayIcon();

            // Set icon using AssetLoader
            try
            {
                var assetLoader = AssetLoader.Open(new Uri("avares://BatteryNotifier/Assets/battery-notifier-logo.ico"));
                _trayIcon.Icon = new WindowIcon(assetLoader);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load tray icon from assets");
            }

            // Set tooltip
            UpdateToolTip();

            // Create menu
            _trayMenu = new NativeMenu();

            var showMenuItem = new NativeMenuItem { Header = "Show Window" };
            showMenuItem.Click += OnShowWindow;

            var settingsMenuItem = new NativeMenuItem { Header = "Settings" };
            settingsMenuItem.Click += OnOpenSettings;

            var sendLogsMenuItem = new NativeMenuItem { Header = "Send Logs..." };
            sendLogsMenuItem.Click += OnSendLogs;

            var githubMenuItem = new NativeMenuItem { Header = "GitHub" };
            githubMenuItem.Click += OnOpenGitHub;

            var exitMenuItem = new NativeMenuItem { Header = "Exit" };
            exitMenuItem.Click += OnExit;

            _trayMenu.Add(showMenuItem);
            _trayMenu.Add(settingsMenuItem);
            _trayMenu.Add(new NativeMenuItemSeparator());
            _trayMenu.Add(sendLogsMenuItem);
            _trayMenu.Add(githubMenuItem);
            _trayMenu.Add(new NativeMenuItemSeparator());
            _trayMenu.Add(exitMenuItem);

            _trayIcon.Menu = _trayMenu;

            // Handle click
            _trayIcon.Clicked += OnTrayIconClicked;

            // Subscribe to battery changes to update icon
            try
            {
                BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;
                NotificationService.Instance.NotificationReceived += OnNotificationReceived;
                _notificationManager = new NotificationManager(new SoundManager());
            }
            catch (Exception serviceEx)
            {
                _logger.Warning(serviceEx, "Some battery services could not be initialized on this platform");
            }

            _logger.Information("TrayIcon initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize TrayIcon");
        }
    }

    private void OnBatteryStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        UpdateToolTip();
    }

    private void UpdateToolTip()
    {
        if (_trayIcon == null) return;

        var batteryPercent = BatteryManagerStore.Instance.BatteryLifePercent;
        var store = BatteryManagerStore.Instance;
        var status = store.IsCharging ? "Charging" : store.IsPluggedIn ? "Plugged In" : "Discharging";

        _trayIcon.ToolTipText = $"BatteryNotifier - {batteryPercent:F0}% ({status})";
    }

    private void OnNotificationReceived(object? sender, NotificationMessage notification)
    {
        if (notification.Type == NotificationType.Inline) return;

        var suppression = SystemStateDetector.GetSuppressionState();
        var isCritical = notification.Priority >= NotificationPriority.Critical;

        // Always update tray tooltip, even when suppressed
        UpdateToolTipWithNotification(notification);

        if (suppression.ShouldSuppressToast && !isCritical)
        {
            _logger.Information(
                "Notification suppressed — DND: {DND}, Fullscreen: {FS}, Tag: {Tag}",
                suppression.IsDoNotDisturb, suppression.IsFullscreen, notification.Tag);
            return;
        }

        // Show native notification
        ShowNativeNotification(notification);

        // Play sound unless DND is active (critical overrides)
        if (!suppression.ShouldSuppressSound || isCritical)
        {
            _ = _notificationManager?.EmitGlobalNotification(notification);
        }
    }

    private void UpdateToolTipWithNotification(NotificationMessage notification)
    {
        if (_trayIcon == null) return;

        string title = notification.Tag switch
        {
            Core.Constants.LowBatteryTag => "Low Battery",
            Core.Constants.FullBatteryTag => "Full Battery",
            _ => "BatteryNotifier"
        };

        var message = notification.Message.Replace("🔋", "").Trim();
        _trayIcon.ToolTipText = $"{title}: {message}";
        Task.Delay(5000).ContinueWith(_ => UpdateToolTip());
    }

    private void ShowNativeNotification(NotificationMessage notification)
    {
        try
        {
            if (_trayIcon == null) return;

            string title = notification.Tag switch
            {
                Core.Constants.LowBatteryTag => "Low Battery",
                Core.Constants.FullBatteryTag => "Full Battery",
                _ => "BatteryNotifier"
            };

            // Remove emoji from message for cleaner notification
            var message = notification.Message.Replace("🔋", "").Trim();

            ShowPlatformNotification(title, message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show native notification");
        }
    }

    private static void ShowPlatformNotification(string title, string message)
    {
        NotificationPlatformService.ShowNativeNotification(title, message);
    }

    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    private void OnShowWindow(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is not MainWindow mainWindow)
            return;

        // Position near notification area before showing
        mainWindow.PositionNearNotificationArea();
        mainWindow.Show();
        mainWindow.Activate();

        if (mainWindow.WindowState == WindowState.Minimized)
        {
            mainWindow.WindowState = WindowState.Normal;
        }
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        ShowMainWindow();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.DataContext is MainWindowViewModel vm
            && vm.CurrentView == null)
        {
            vm.NavigateToSettingsCommand.Execute().Subscribe();
        }
    }

    private void OnSendLogs(object? sender, EventArgs e)
    {
        try
        {
            if (!CrashReporter.CanSendReport())
            {
                var remaining = CrashReporter.GetCooldownRemaining();
                _logger.Warning("Send logs rate-limited. {Minutes:F0} minutes remaining", remaining.TotalMinutes);
                // Still save to file (not rate-limited)
                var report = CrashReporter.BuildManualReport();
                CrashReporter.SaveReportToFile(report);
                return;
            }

            var manualReport = CrashReporter.BuildManualReport();

            // Save to file first (always available)
            var filePath = CrashReporter.SaveReportToFile(manualReport);

            // Open GitHub issue form for user review
            CrashReporter.OpenGitHubIssue(
                $"[Log Report] v{Core.Constants.ApplicationVersion}",
                manualReport);

            _logger.Information("User-initiated log report sent. Saved to {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send logs");
        }
    }

    private void OnOpenGitHub(object? sender, EventArgs e)
    {
        try
        {
            var url = Constants.SourceRepositoryUrl;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else
                Process.Start("xdg-open", url);
        }
        catch { }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            try
            {
                BatteryMonitorService.Instance.BatteryStatusChanged -= OnBatteryStatusChanged;
                NotificationService.Instance.NotificationReceived -= OnNotificationReceived;
            }
            catch { }

            _notificationManager?.Dispose();
            _notificationManager = null;

            if (_trayIcon != null)
            {
                _trayIcon.Clicked -= OnTrayIconClicked;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            _trayMenu = null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing TrayIconService");
        }

        _disposed = true;
    }
}
