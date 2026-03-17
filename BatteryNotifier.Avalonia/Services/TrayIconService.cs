using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
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
    private CancellationTokenSource? _tooltipRevertCts;
    private IDisposable? _visibilitySubscription;
    private bool _disposed;

    // Store menu items for clean unsubscription in Dispose
    private NativeMenuItem? _showHideMenuItem;
    private NativeMenuItem? _aboutMenuItem;
    private NativeMenuItem? _updateMenuItem;
    private NativeMenuItem? _exitMenuItem;

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

            _showHideMenuItem = new NativeMenuItem { Header = "Show Window" };
            _showHideMenuItem.Click += OnShowHideWindow;

            _aboutMenuItem = new NativeMenuItem { Header = "About" };
            _aboutMenuItem.Click += OnOpenAbout;

            _updateMenuItem = new NativeMenuItem { Header = "Check for Updates..." };
            _updateMenuItem.Click += OnCheckForUpdates;

            _exitMenuItem = new NativeMenuItem { Header = "Exit" };
            _exitMenuItem.Click += OnExit;

            _trayMenu.Add(_showHideMenuItem);
            _trayMenu.Add(new NativeMenuItemSeparator());
            _trayMenu.Add(_updateMenuItem);
            _trayMenu.Add(_aboutMenuItem);
            _trayMenu.Add(new NativeMenuItemSeparator());
            _trayMenu.Add(_exitMenuItem);

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

            // Subscribe to window visibility changes to keep tray menu label in sync.
            // This catches hides from the in-window menu, the X button, tray toggle, etc.
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime dt
                && dt.MainWindow is { } mainWin)
            {
                _visibilitySubscription = mainWin.GetObservable(Window.IsVisibleProperty)
                    .Subscribe(_ => UpdateShowHideMenuLabel());
            }

            // Start background update checks (if enabled)
            try
            {
                UpdateService.Instance.UpdateAvailable += OnUpdateAvailable;
                if (AppSettings.Instance.AutoCheckForUpdates)
                    UpdateService.Instance.StartBackgroundChecks();
            }
            catch (Exception updateEx)
            {
                _logger.Warning(updateEx, "Update service could not be initialized");
            }

        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize TrayIcon");
        }
    }

    private void OnBatteryStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        // Event fires on BatteryMonitorService's timer thread — marshal to UI
        global::Avalonia.Threading.Dispatcher.UIThread.Post(UpdateToolTip);
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

        // Event can fire from NotificationService's flush Timer (threadpool thread).
        // Marshal tray icon access to UI thread; sound/subprocess calls are thread-safe.
        if (!global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => OnNotificationReceived(sender, notification));
            return;
        }

        var suppression = SystemStateDetector.GetSuppressionState();
        var isCritical = notification.Priority >= NotificationPriority.Critical;

        _logger.Information("Notification received: tag={Tag} DND={DND} fullscreen={Fullscreen} critical={Critical}",
            notification.Tag, suppression.IsDoNotDisturb, suppression.IsFullscreen, isCritical);

        // Always update tray tooltip, even when suppressed
        UpdateToolTipWithNotification(notification);

        if (suppression.ShouldSuppressToast && !isCritical)
        {
            _logger.Information("Notification toast suppressed (DND={DND}, fullscreen={Fullscreen})",
                suppression.IsDoNotDisturb, suppression.IsFullscreen);
            return;
        }

        // Show native notification
        _logger.Information("Delivering native notification: {Tag} — {Message}", notification.Tag, notification.Message);
        ShowNativeNotification(notification);

        // Play sound unless DND is active (critical overrides)
        if (!suppression.ShouldSuppressSound || isCritical)
        {
            _ = _notificationManager?.EmitGlobalNotification(notification);
        }
        else
        {
            _logger.Information("Notification sound suppressed by DND");
        }
    }

    private void UpdateToolTipWithNotification(NotificationMessage notification)
    {
        if (_trayIcon == null) return;

        string title = notification.Tag switch
        {
            Constants.LowBatteryTag => "Low Battery",
            Constants.FullBatteryTag => "Full Battery",
            _ => "BatteryNotifier"
        };

        var message = notification.Message.Replace("🔋", "").Trim();
        _trayIcon.ToolTipText = $"{title}: {message}";

        // Cancel any previous revert timer, then schedule a new one
        _tooltipRevertCts?.Cancel();
        _tooltipRevertCts?.Dispose();
        _tooltipRevertCts = new CancellationTokenSource();
        _ = RevertToolTipAfterDelayAsync(_tooltipRevertCts.Token);
    }

    private async Task RevertToolTipAfterDelayAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(5000, ct).ConfigureAwait(false);
            UpdateToolTip();
        }
        catch (OperationCanceledException)
        {
            // Cancelled by a newer notification or disposal — expected
        }
    }

    private void ShowNativeNotification(NotificationMessage notification)
    {
        try
        {
            if (_trayIcon == null) return;

            string title = notification.Tag switch
            {
                Constants.LowBatteryTag => "Low Battery",
                Constants.FullBatteryTag => "Full Battery",
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
        ToggleMainWindow();
    }

    private void OnShowHideWindow(object? sender, EventArgs e)
    {
        ToggleMainWindow();
    }

    private void ToggleMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is { IsVisible: true })
            HideMainWindow();
        else
            ShowMainWindow();
    }

    private void UpdateShowHideMenuLabel()
    {
        if (_showHideMenuItem == null) return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { IsVisible: true })
        {
            _showHideMenuItem.Header = "Hide Window";
        }
        else
        {
            _showHideMenuItem.Header = "Show Window";
        }
    }

    private static void HideMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        desktop.MainWindow?.Hide();
        MacOSDockIconHelper.HideDockIcon();
    }

    private static void ShowMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is not MainWindow mainWindow)
            return;

        // Show dock icon so CMD+Tab works while window is visible
        MacOSDockIconHelper.ShowDockIcon();

        // Position near notification area before showing
        mainWindow.PositionNearNotificationArea();
        mainWindow.Show();
        mainWindow.Activate();

        if (mainWindow.WindowState == WindowState.Minimized)
        {
            mainWindow.WindowState = WindowState.Normal;
        }
    }

    private void OnOpenAbout(object? sender, EventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        // Ensure main window is visible so the About dialog has an owner
        if (desktop.MainWindow is not MainWindow mainWindow)
            return;

        if (!mainWindow.IsVisible)
            ShowMainWindow();

        if (mainWindow.DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.OpenAboutCommand.Execute().Subscribe();
        }
    }

    private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        // Event fires from a threadpool thread — marshal to UI thread for tray access
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var safeTag = SanitizeExternalText(e.Release.TagName);
            ShowPlatformNotification("Update Available",
                $"BatteryNotifier {safeTag} is available. Use tray menu to download.");
        });
    }

    /// <summary>
    /// Strip control characters and truncate text from external sources (GitHub API).
    /// The downstream NotificationPlatformService does platform-specific sanitization,
    /// but defense-in-depth ensures no unexpected content reaches it.
    /// </summary>
    private static string SanitizeExternalText(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var chars = new char[Math.Min(input.Length, 100)];
        int j = 0;
        for (int i = 0; i < input.Length && j < chars.Length; i++)
        {
            if (!char.IsControl(input[i]))
                chars[j++] = input[i];
        }
        return new string(chars, 0, j);
    }

    private async void OnCheckForUpdates(object? sender, EventArgs e)
    {
        try
        {
            // Show "Checking..." tooltip while the request is in flight
            if (_trayIcon != null)
                _trayIcon.ToolTipText = "BatteryNotifier — Checking for updates...";

            var result = await UpdateService.Instance.CheckForUpdateManualAsync().ConfigureAwait(false);

            switch (result.Status)
            {
                case CheckStatus.UpdateAvailable when result.Release != null:
                    OpenUrl(result.Release.HtmlUrl);
                    break;

                case CheckStatus.UpToDate:
                    ShowPlatformNotification("No Updates",
                        $"You're running the latest version ({Constants.ApplicationVersion}).");
                    break;

                case CheckStatus.AlreadyChecking:
                    // User clicked again while a check is running — do nothing
                    break;

                case CheckStatus.Failed:
                    ShowPlatformNotification("Update Check Failed",
                        "Could not reach GitHub. Check your internet connection.");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Manual update check failed");
            ShowPlatformNotification("Update Check Failed",
                "An unexpected error occurred.");
        }
        finally
        {
            // Restore normal tooltip
            UpdateToolTip();
        }
    }

    private static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var psi = new ProcessStartInfo("open") { UseShellExecute = false };
            psi.ArgumentList.Add(url);
            using var p = Process.Start(psi);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var p = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else
        {
            var psi = new ProcessStartInfo("xdg-open") { UseShellExecute = false };
            psi.ArgumentList.Add(url);
            using var p = Process.Start(psi);
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
            try
            {
                BatteryMonitorService.Instance.BatteryStatusChanged -= OnBatteryStatusChanged;
                NotificationService.Instance.NotificationReceived -= OnNotificationReceived;
                UpdateService.Instance.UpdateAvailable -= OnUpdateAvailable;
                UpdateService.Instance.Dispose();
            }
            catch { }

            _visibilitySubscription?.Dispose();
            _visibilitySubscription = null;

            _tooltipRevertCts?.Cancel();
            _tooltipRevertCts?.Dispose();
            _tooltipRevertCts = null;

            _notificationManager?.Dispose();
            _notificationManager = null;

            // Unsubscribe menu item Click handlers to prevent event leaks
            if (_showHideMenuItem != null) { _showHideMenuItem.Click -= OnShowHideWindow; _showHideMenuItem = null; }
            if (_aboutMenuItem != null) { _aboutMenuItem.Click -= OnOpenAbout; _aboutMenuItem = null; }
            if (_updateMenuItem != null) { _updateMenuItem.Click -= OnCheckForUpdates; _updateMenuItem = null; }
            if (_exitMenuItem != null) { _exitMenuItem.Click -= OnExit; _exitMenuItem = null; }
            _trayMenu = null;

            if (_trayIcon != null)
            {
                _trayIcon.Clicked -= OnTrayIconClicked;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing TrayIconService");
        }

        _disposed = true;
    }
}
