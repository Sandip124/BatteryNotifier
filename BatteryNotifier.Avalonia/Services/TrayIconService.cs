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
using Velopack;
using Velopack.Sources;

namespace BatteryNotifier.Avalonia.Services;

internal sealed class TrayIconService : IDisposable
{
    private readonly ILogger _logger;
    private TrayIcon? _trayIcon;
    private NotificationManager? _notificationManager;
    private NotificationDisplayService? _displayService;
    private CancellationTokenSource? _tooltipRevertCts;
    private IDisposable? _visibilitySubscription;
    private bool _disposed;

    // Store menu items for clean unsubscription in Dispose
    private NativeMenuItem? _pauseNotificationsMenuItem;
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
                var assetLoader = AssetLoader.Open(AssetUris.LogoIco);
                _trayIcon.Icon = new WindowIcon(assetLoader);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load tray icon from assets");
            }

            // Set tooltip
            UpdateToolTip();

            // Create menu
             var trayMenu = new NativeMenu();

            _showHideMenuItem = new NativeMenuItem { Header = "Show Window" };
            _showHideMenuItem.Click += OnShowHideWindow;

            _pauseNotificationsMenuItem = new NativeMenuItem { Header = "Pause Notifications (2h)" };
            _pauseNotificationsMenuItem.Click += OnTogglePauseNotifications;

            _aboutMenuItem = new NativeMenuItem { Header = "About" };
            _aboutMenuItem.Click += OnOpenAbout;

            _updateMenuItem = new NativeMenuItem { Header = "Check for Updates..." };
            _updateMenuItem.Click += OnCheckForUpdates;

            _exitMenuItem = new NativeMenuItem { Header = "Exit" };
            _exitMenuItem.Click += OnExit;

            trayMenu.Add(_showHideMenuItem);
            trayMenu.Add(_pauseNotificationsMenuItem);
            trayMenu.Add(new NativeMenuItemSeparator());
            trayMenu.Add(_updateMenuItem);
            trayMenu.Add(_aboutMenuItem);
            trayMenu.Add(new NativeMenuItemSeparator());
            trayMenu.Add(_exitMenuItem);

            _trayIcon.Menu = trayMenu;

            // Sync pause menu label from any source (tray toggle, main window Resume, auto-resume)
            NotificationService.Instance.PausedChanged += OnPausedStateChanged;

            // Handle click
            _trayIcon.Clicked += OnTrayIconClicked;

            // Subscribe to battery changes to update icon
            try
            {
                BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;
                NotificationService.Instance.NotificationReceived += OnNotificationReceived;
                _notificationManager = new NotificationManager(new SoundManager());
                _displayService = new NotificationDisplayService();
                _displayService.SetNotificationManager(_notificationManager);
            }
            catch (Exception serviceEx)
            {
                _logger.Warning(serviceEx, "Some battery services could not be initialized on this platform");
            }

            // Subscribe to window visibility changes to keep tray menu label in sync.
            // This catches hides from the in-window menu, the X button, tray toggle, etc.
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWin })
            {
                _visibilitySubscription = mainWin.GetObservable(Visual.IsVisibleProperty)
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
        if (!global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(UpdateToolTip);
            return;
        }

        if (_trayIcon == null) return;

        _trayIcon.ToolTipText = $"BatteryNotifier - {FormatBatteryStatus()}";
    }

    private static string FormatBatteryStatus()
    {
        var store = BatteryManagerStore.Instance;
        var pct = $"{store.BatteryLifePercent:F0}%";

        string status;
        if (store.IsCharging) status = "Charging";
        else if (store.IsPluggedIn) status = "Plugged In";
        else status = "On Battery";

        var time = FormatTimeRemaining(store);
        return time != null ? $"{pct} · {status} · {time}" : $"{pct} · {status}";
    }

    private static string? FormatTimeRemaining(BatteryManagerStore store)
    {
        if (store.BatteryLifeRemaining <= 0) return null;
        var ts = store.BatteryLifeRemainingInSeconds;
        var h = (int)ts.TotalHours;
        return h > 0 ? $"~{h}h {ts.Minutes}m" : $"~{ts.Minutes}m";
    }

    private static void OnTogglePauseNotifications(object? sender, EventArgs e)
    {
        if (NotificationService.Instance.IsPaused)
            NotificationService.Instance.ResumeNotifications();
        else
            NotificationService.Instance.PauseNotifications();
    }

    private void OnPausedStateChanged(bool paused)
    {
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_pauseNotificationsMenuItem != null)
                _pauseNotificationsMenuItem.Header = paused
                    ? "Resume Notifications"
                    : "Pause Notifications (2h)";
        });
    }

    private void OnNotificationReceived(object? sender, NotificationMessageEventArgs notification)
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

        // Temporarily exit efficiency mode for notification delivery
        EfficiencyModeService.Instance.AcquireNormalMode();

        // Show Avalonia-native notification (screen flash + card)
        _logger.Information("Delivering notification: {Tag} — {Message}", notification.Tag, notification.Message);
        var alert = !string.IsNullOrEmpty(notification.Tag)
            ? AppSettings.Instance.Alerts.Find(a => a.Id == notification.Tag)
            : null;
        _displayService?.ShowNotification(notification, alert);

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

    private void UpdateToolTipWithNotification(NotificationMessageEventArgs notification)
    {
        if (_trayIcon == null) return;

        string title = notification.Tag switch
        {
            Constants.LowBatteryTag => "Low Battery",
            Constants.FullBatteryTag => "Full Battery",
            _ => Constants.AppName
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
        await Task.Delay(5000, ct).ConfigureAwait(false);
        UpdateToolTip();
    }

    private static void OnTrayIconClicked(object? sender, EventArgs e)
    {
        ToggleMainWindow();
    }

    private static void OnShowHideWindow(object? sender, EventArgs e)
    {
        ToggleMainWindow();
    }

    private static void ToggleMainWindow()
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
        EfficiencyModeService.Instance.EnableEfficiency();
    }

    private static void ShowMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is not MainWindow mainWindow)
            return;

        EfficiencyModeService.Instance.DisableEfficiency();

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

    private static void OnOpenAbout(object? sender, EventArgs e)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowStandalone();
    }

    private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        // Event fires from a threadpool thread — marshal to UI thread for tray access
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var safeTag = SanitizeExternalText(e.Release.TagName);
            _displayService?.ShowSimpleNotification("Update Available",
                $"BatteryNotifier {safeTag} is available. Click 'Check for Updates' to install.");
        });
    }

    /// <summary>
    /// Strip control characters and truncate text from external sources (GitHub API).
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
            if (_trayIcon != null)
                _trayIcon.ToolTipText = "BatteryNotifier — Checking for updates...";

            var mgr = new UpdateManager(new GithubSource(Constants.SourceRepositoryUrl, null, false));

            if (!mgr.IsInstalled)
            {
                // Portable/dev mode — fall back to opening GitHub
                var result = await UpdateService.Instance.CheckForUpdateManualAsync();
                if (result.Status == CheckStatus.UpdateAvailable && result.Release != null)
                    OpenUrl(result.Release.HtmlUrl);
                else if (result.Status == CheckStatus.UpToDate)
                    _displayService?.ShowSimpleNotification("No Updates",
                        $"You're running the latest version ({Constants.ApplicationVersion}).");
                else if (result.Status == CheckStatus.Failed)
                    _displayService?.ShowSimpleNotification("Update Check Failed",
                        "Could not reach GitHub. Check your internet connection.");
                return;
            }

            var updateInfo = await mgr.CheckForUpdatesAsync();
            if (updateInfo == null)
            {
                _displayService?.ShowSimpleNotification("No Updates",
                    $"You're running the latest version ({Constants.ApplicationVersion}).");
                return;
            }

            _displayService?.ShowSimpleNotification("Downloading Update",
                $"Downloading BatteryNotifier {updateInfo.TargetFullRelease.Version}...");

            await mgr.DownloadUpdatesAsync(updateInfo);

            _displayService?.ShowSimpleNotification("Update Ready",
                $"BatteryNotifier {updateInfo.TargetFullRelease.Version} downloaded. Restarting...");

            // Brief delay so user can see the notification
            await Task.Delay(2000);

            mgr.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Update check/download failed");
            _displayService?.ShowSimpleNotification("Update Check Failed",
                "An unexpected error occurred. Check your internet connection.");
        }
        finally
        {
            UpdateToolTip();
        }
    }

    private static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var psi = new ProcessStartInfo(Constants.ResolveCommand("open")) { UseShellExecute = false };
            psi.ArgumentList.Add(url);
            using var p = Process.Start(psi);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var p = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else
        {
            var psi = new ProcessStartInfo(Constants.ResolveCommand("xdg-open")) { UseShellExecute = false };
            psi.ArgumentList.Add(url);
            using var p = Process.Start(psi);
        }
    }

    private static void OnExit(object? sender, EventArgs e)
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
            UpdateService.Instance.UpdateAvailable -= OnUpdateAvailable;
            UpdateService.Instance.Dispose();


            _visibilitySubscription?.Dispose();
            _visibilitySubscription = null;

            _tooltipRevertCts?.Cancel();
            _tooltipRevertCts?.Dispose();
            _tooltipRevertCts = null;

            _notificationManager?.Dispose();
            _notificationManager = null;
            _displayService?.DismissAll();
            _displayService = null;

            NotificationService.Instance.PausedChanged -= OnPausedStateChanged;

            // Unsubscribe menu item Click handlers to prevent event leaks
            if (_pauseNotificationsMenuItem != null)
            {
                _pauseNotificationsMenuItem.Click -= OnTogglePauseNotifications;
                _pauseNotificationsMenuItem = null;
            }

            if (_showHideMenuItem != null)
            {
                _showHideMenuItem.Click -= OnShowHideWindow;
                _showHideMenuItem = null;
            }

            if (_aboutMenuItem != null)
            {
                _aboutMenuItem.Click -= OnOpenAbout;
                _aboutMenuItem = null;
            }

            if (_updateMenuItem != null)
            {
                _updateMenuItem.Click -= OnCheckForUpdates;
                _updateMenuItem = null;
            }

            if (_exitMenuItem != null)
            {
                _exitMenuItem.Click -= OnExit;
                _exitMenuItem = null;
            }
            
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