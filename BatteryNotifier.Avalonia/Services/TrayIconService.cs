using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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
                var assetLoader = AssetLoader.Open(new Uri("avares://BatteryNotifier.Avalonia/Assets/battery-notifier-logo.ico"));
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

            var exitMenuItem = new NativeMenuItem { Header = "Exit" };
            exitMenuItem.Click += OnExit;

            _trayMenu.Add(showMenuItem);
            _trayMenu.Add(new NativeMenuItemSeparator());
            _trayMenu.Add(exitMenuItem);

            _trayIcon.Menu = _trayMenu;

            // Handle double-click
            _trayIcon.Clicked += OnTrayIconClicked;

            // Subscribe to battery changes to update icon
            BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;

            // Subscribe to notifications
            NotificationService.Instance.NotificationReceived += OnNotificationReceived;

            // Initialize notification manager
            _notificationManager = new NotificationManager(new SoundManager());

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
        var isCharging = BatteryManagerStore.Instance.IsCharging;
        var status = isCharging ? "Charging" : "Discharging";

        _trayIcon.ToolTipText = $"Battery Notifier - {batteryPercent:F0}% ({status})";
    }

    private void OnNotificationReceived(object? sender, NotificationMessage notification)
    {
        if (notification.Type == NotificationType.Inline) return;

        // Show native notification
        ShowNativeNotification(notification);

        // Play sound and handle notification
        _ = _notificationManager?.EmitGlobalNotification(notification);
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
                _ => "Battery Notifier"
            };

            // Remove emoji from message for cleaner notification
            var message = notification.Message.Replace("🔋", "").Trim();

            // Avalonia's TrayIcon doesn't have built-in notification support
            // We'll need to use platform-specific notifications
            ShowPlatformNotification(title, message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show native notification");
        }
    }

    private void ShowPlatformNotification(string title, string message)
    {
        // Use native notification service for platform-specific notifications
        NotificationPlatformService.ShowNativeNotification(title, message);

        // Also update tooltip temporarily
        if (_trayIcon != null)
        {
            var originalTooltip = _trayIcon.ToolTipText;
            _trayIcon.ToolTipText = $"{title}: {message}";

            // Reset tooltip after a delay
            Task.Delay(5000).ContinueWith(_ => UpdateToolTip());
        }
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
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow != null)
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();

                // Bring to front
                if (desktop.MainWindow.WindowState == WindowState.Minimized)
                {
                    desktop.MainWindow.WindowState = WindowState.Normal;
                }
            }
        }
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
            BatteryMonitorService.Instance.BatteryStatusChanged -= OnBatteryStatusChanged;
            NotificationService.Instance.NotificationReceived -= OnNotificationReceived;

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
